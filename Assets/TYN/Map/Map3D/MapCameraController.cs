using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace EldritchMile.Map3D
{
    /// <summary>
    /// 玩家操作地圖視角。掛在 MapView3D 旁邊。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【操作配置】左鍵 = 平移，右鍵／中鍵 = 旋轉。
    ///
    /// 左鍵同時是選節點的鍵，所以左鍵上的手勢**必須是安全的** ——
    /// 誤觸平移只要放手就會自己飄回去，誤觸旋轉則會留在歪掉的角度。
    /// 一開始做成「左鍵旋轉」，實測之後換過來。
    ///
    /// 【為什麼旋轉要夾住】地圖有推進方向（往深處走）。
    /// 能轉 360 度的話玩家會轉到上下顛倒，再也分不出哪邊是前面。
    /// 夾在正負 40 度：夠看清楚被棋子擋住的東西，又不會迷路。
    ///
    /// 【為什麼需要「歸位」】任何可以自由轉動的視角都必須有回家的路。
    /// 玩家轉歪了找不到北的時候，那顆鈕就是唯一的救命索。
    ///
    /// ⚠️ **拖曳與點擊要分得開。** 節點是左鍵點的，而左鍵也用來轉視角 ——
    /// 沒有位移門檻的話，玩家想轉視角卻會誤觸節點，那是不可逆的（直接進關卡）。
    /// 所以超過 dragThreshold 才算拖曳，否則算點擊，見 MapView3D。
    /// </summary>
    [RequireComponent(typeof(MapCameraRig))]
    public class MapCameraController : MonoBehaviour
    {
        [Header("接哪裡")]
        [Tooltip("顯示地圖的 RawImage。滑鼠要在它範圍內才吃操作")]
        public RawImage surface;

        [Header("旋轉（左鍵拖曳）")]
        [Tooltip("水平拖一個畫面寬度轉幾度")]
        public float yawPerScreen = 90f;

        [Tooltip("垂直拖一個畫面高度轉幾度")]
        public float pitchPerScreen = 60f;

        [Tooltip("勾選 = 拖曳方向與地圖轉動方向相反（像抓著地圖轉）")]
        public bool invertDrag = true;

        [Tooltip("**鎖住俯角**：拖曳只轉水平方向，像把地圖放在桌上轉。\n" +
                 "俯角由 MapCameraRig 的 Pitch 決定（美術調的值），玩家改不動。\n" +
                 "（2026-09-17 美術：俯角要固定）")]
        public bool lockPitch = true;

        [Header("縮放（滾輪）")]
        [Tooltip("滾一格改變多少距離")]
        public float zoomPerNotch = 2.5f;

        [Header("平移（右鍵或中鍵拖曳）")]
        [Tooltip("拖一個畫面寬度平移多少世界單位")]
        public float panPerScreen = 20f;

        [Tooltip("平移離地圖中心最遠多少世界單位。防止把地圖推出畫面外找不回來")]
        public float panLimit = 6f;

        [Header("歸位")]
        [Tooltip("回到預設視角要多久（秒）")]
        [Min(0f)] public float resetSeconds = 0.35f;

        [Header("追隨目前位置")]
        [Tooltip("鏡頭跟著玩家目前所在的節點。關掉就固定看地圖中心")]
        public bool followCurrentNode = true;

        [Tooltip("追過去要多久（秒）。太快會像被拉扯，太慢會跟不上")]
        [Min(0f)] public float followSmooth = 0.35f;

        [Tooltip("追隨的目標。由 MapView3D 在換節點時指過來，不用手動填")]
        public Transform followTarget;

        [Header("平移自動歸位")]
        [Tooltip("放手之後多久開始飄回去（秒）。0 = 放手就回")]
        [Min(0f)] public float panReturnDelay = 1.2f;

        [Tooltip("飄回去要多久（秒）")]
        [Min(0.01f)] public float panReturnSmooth = 0.6f;

        [Header("縮放滑桿")]
        [Tooltip("左下角那根。0 = 拉遠，1 = 拉近。留空則只能用滾輪")]
        public Slider zoomSlider;

        [Header("門檻")]
        [Tooltip("滑鼠移動超過這麼多像素才算拖曳，否則算點擊。太小會誤判成拖曳而點不到節點")]
        public float dragThreshold = 6f;

        [Header("開場縱覽（每一場 run 第一次打開地圖）")]
        [Tooltip("地圖滑下來之後，停在「看得到整張圖」的距離多久（秒）。\n" +
                 "玩家點一下或滾輪就提早開始移動")]
        [Min(0f)] public float introHoldSeconds = 1.5f;

        [Tooltip("從縱覽拉近到起點要多久（秒）")]
        [Min(0.01f)] public float introMoveSeconds = 1.4f;

        [Tooltip("縱覽時四周留多少空間。1 = 剛好貼齊邊緣，1.15 = 多留 15%")]
        [Min(1f)] public float introFitMargin = 1.15f;

        /// <summary>開場縱覽還在跑（含等待地圖滑下來那一段）</summary>
        public bool IsPlayingIntro { get { return introPhase != IntroPhase.None; } }

        private enum IntroPhase { None, Waiting, Hold, Move }
        private IntroPhase introPhase = IntroPhase.None;
        private float introT;
        private float introFromDistance;
        private Vector3 introFromOffset;
        private Transform introTarget;

        /// <summary>這一次按下之後有沒有被判定成拖曳。MapView3D 靠它決定要不要吃點擊。</summary>
        public bool IsDragging { get; private set; }

        private MapCameraRig rig;
        private Vector2 pressPos;
        private Vector2 lastPos;
        private bool pressedInside;
        private int pressButton = -1;

        /// 手動平移的位移。會自己飄回 0
        private Vector3 panOffset;

        /// 追隨目前節點造成的位移。與 panOffset 相加才是最終的 pivotOffset
        private Vector3 followOffset;
        private Vector3 followVel;

        /// 放手之後過了多久
        private float sincePan;

        /// 避免「滑桿改距離 → 距離改滑桿」無限互相觸發
        private bool syncingSlider;

        private float homeYaw, homePitch, homeDistance;
        private Vector3 homeOffset;
        private float resetT = -1f;
        private float fromYaw, fromPitch, fromDistance;
        private Vector3 fromOffset;

        private void Awake()
        {
            rig = GetComponent<MapCameraRig>();
            homeYaw = rig.yaw; homePitch = rig.pitch;
            homeDistance = rig.distance; homeOffset = rig.pivotOffset;

            if (zoomSlider == null) return;

            // 滑桿右邊 = 拉近。距離越小越近，所以是反過來對應的
            zoomSlider.minValue = 0f;
            zoomSlider.maxValue = 1f;
            zoomSlider.SetValueWithoutNotify(DistanceToSlider(rig.distance));
            zoomSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        private void OnDestroy()
        {
            if (zoomSlider != null) zoomSlider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        private float DistanceToSlider(float d)
        {
            float lo = rig.distanceLimit.x, hi = rig.distanceLimit.y;
            if (hi - lo < 0.001f) return 0f;
            return 1f - Mathf.Clamp01((d - lo) / (hi - lo));
        }

        private float SliderToDistance(float v)
        {
            return Mathf.Lerp(rig.distanceLimit.y, rig.distanceLimit.x, Mathf.Clamp01(v));
        }

        /// <summary>
        /// 滑桿被拖動。
        ///
        /// ⚠️ `syncingSlider` 是為了擋掉「滑桿改距離 → 距離回寫滑桿 → 又觸發一次」
        /// 的互相觸發。沒有它的話用滾輪縮放會跟滑桿打架，數值會抖。
        /// </summary>
        private void OnSliderChanged(float v)
        {
            if (syncingSlider) return;

            rig.distance = SliderToDistance(v);
            rig.Apply();
        }

        private void PushSlider()
        {
            if (zoomSlider == null) return;

            syncingSlider = true;
            zoomSlider.SetValueWithoutNotify(DistanceToSlider(rig.distance));
            syncingSlider = false;
        }

        /// <summary>回到預設視角。給 UI 的鈕綁。</summary>
        public void ResetView()
        {
            fromYaw = rig.yaw; fromPitch = rig.pitch;
            fromDistance = rig.distance; fromOffset = rig.pivotOffset;

            // 手動平移直接歸零 —— 歸位動畫跑完之後才不會又被它拉走
            panOffset = Vector3.zero;
            sincePan = panReturnDelay;

            resetT = 0f;
        }

        // ==========================================
        // 開場縱覽
        // ==========================================

        /// <summary>
        /// 準備開場縱覽：把鏡頭擺到看得到整張圖的位置，**先不開始計時**。
        ///
        /// 【為什麼分成準備與開始兩步】地圖是滑下來的覆蓋層。在滑下來**之前**就擺好縱覽，
        /// 玩家看到的第一眼就是全圖；等滑完（`StartIntro`）才開始算停留時間 ——
        /// 不然停留時間有一大半花在滑動上，玩家根本沒看清楚。
        /// </summary>
        /// <param name="mapBounds">所有節點的世界範圍</param>
        /// <param name="target">縱覽結束後要停在誰身上（起點的棋子）</param>
        public void PrepareIntro(Bounds mapBounds, Transform target)
        {
            if (rig == null) rig = GetComponent<MapCameraRig>();

            resetT = -1f;
            panOffset = Vector3.zero;
            sincePan = panReturnDelay;

            Vector3 centre = rig.pivot != null ? rig.pivot.position : Vector3.zero;
            Vector3 off = mapBounds.center - centre;
            off.y = 0f;

            followOffset = off;
            followVel = Vector3.zero;

            float fit = FitDistance(mapBounds);
            rig.distanceMaxOverride = fit;
            rig.yaw = homeYaw;
            rig.pitch = homePitch;
            rig.distance = fit;
            rig.pivotOffset = followOffset;
            rig.Apply();
            PushSlider();

            introTarget = target;
            introPhase = IntroPhase.Waiting;
            introT = 0f;
        }

        /// <summary>地圖滑完了，開始算停留時間。沒有準備過的話什麼都不做。</summary>
        public void StartIntro()
        {
            if (introPhase != IntroPhase.Waiting) return;
            introPhase = IntroPhase.Hold;
            introT = 0f;
        }

        /// <summary>
        /// 要多遠才裝得下整張圖。
        ///
        /// 地面是斜著看的：左右看的是整個寬，前後因為俯角被壓扁成 sin(pitch) 倍。
        /// 兩個方向各算一次需要的距離，取大的那個。
        /// </summary>
        private float FitDistance(Bounds b)
        {
            float radius = new Vector2(b.extents.x, b.extents.z).magnitude;
            float vHalf = rig.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float hHalf = Mathf.Atan(Mathf.Tan(vHalf) * Mathf.Max(0.1f, rig.Aspect));

            float byHeight = radius * Mathf.Sin(rig.pitch * Mathf.Deg2Rad) / Mathf.Tan(vHalf);
            float byWidth = radius / Mathf.Tan(hHalf);

            return Mathf.Max(byHeight, byWidth, homeDistance) * introFitMargin;
        }

        /// <summary>
        /// 縱覽的每一幀。停留 → 平滑拉近到起點 → 交還給一般的追隨。
        ///
        /// ⚠️ 期間**不吃拖曳與縮放**（會跟動畫搶鏡頭），但點一下／滾輪會跳過停留。
        /// 點擊本身照常交給 MapView3D —— 看完全圖直接點起點是合理的。
        /// </summary>
        private void TickIntro(Mouse mouse)
        {
            if (introPhase == IntroPhase.Waiting) return;

            bool input = mouse != null
                && (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame
                    || Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f);

            introT += Time.unscaledDeltaTime;

            if (introPhase == IntroPhase.Hold)
            {
                if (introT < introHoldSeconds && !input) return;

                introPhase = IntroPhase.Move;
                introT = 0f;
                introFromDistance = rig.distance;
                introFromOffset = followOffset;
                return;
            }

            float t = Mathf.Clamp01(introT / introMoveSeconds);
            float e = Mathf.SmoothStep(0f, 1f, t);

            Vector3 goal = introFromOffset;
            if (introTarget != null)
            {
                Vector3 centre = rig.pivot != null ? rig.pivot.position : Vector3.zero;
                goal = introTarget.position - centre;
                goal.y = 0f;
            }

            rig.distance = Mathf.Lerp(introFromDistance, homeDistance, e);
            followOffset = Vector3.Lerp(introFromOffset, goal, e);
            rig.pivotOffset = followOffset;
            rig.Apply();
            PushSlider();

            if (t < 1f) return;

            // 交還：之後由 ComposeOffset 照常追隨起點的棋子
            introPhase = IntroPhase.None;
            rig.distanceMaxOverride = 0f;
            followVel = Vector3.zero;
            if (introTarget != null) followTarget = introTarget;
        }

        private void Update()
        {
            if (rig == null) return;

            if (introPhase != IntroPhase.None) { TickIntro(Mouse.current); return; }

            if (resetT >= 0f) { TickReset(); return; }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();

            // ⚠️ 滑鼠壓在 UI 上（縮放滑桿、離開鍵）時不要動鏡頭。
            //    滑桿蓋在 MapSurface 上面，沒有這道判斷的話拖滑桿會**同時**把地圖拖走
            bool overUI = EventSystem.current != null
                          && EventSystem.current.IsPointerOverGameObject();

            bool inside = !overUI && IsInsideSurface(pos);

            // ── 按下 ──
            if (mouse.leftButton.wasPressedThisFrame) BeginPress(pos, inside, 0);
            else if (mouse.rightButton.wasPressedThisFrame) BeginPress(pos, inside, 1);
            else if (mouse.middleButton.wasPressedThisFrame) BeginPress(pos, inside, 2);

            // ── 放開 ──
            if (mouse.leftButton.wasReleasedThisFrame
                || mouse.rightButton.wasReleasedThisFrame
                || mouse.middleButton.wasReleasedThisFrame)
            {
                pressedInside = false;
                pressButton = -1;
                // ⚠️ IsDragging 不在這裡清 —— MapView3D 要在同一幀讀到它，
                //    才知道這一下放開該不該算成點擊節點。清在 LateUpdate
            }

            // ── 拖曳中 ──
            if (pressedInside && pressButton >= 0)
            {
                if (!IsDragging && (pos - pressPos).magnitude > dragThreshold) IsDragging = true;

                if (IsDragging)
                {
                    Vector2 delta = pos - lastPos;

                    // 左鍵 = 平移，右鍵／中鍵 = 旋轉。
                    // 左鍵同時是選節點的鍵，而平移比旋轉「安全」——
                    // 誤觸平移只要放手就會飄回去，誤觸旋轉則會留在歪掉的角度
                    if (pressButton == 0) Pan(delta);
                    else Orbit(delta);
                }
            }

            lastPos = pos;

            // ── 滾輪縮放 ──
            if (!inside) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            rig.distance -= Mathf.Sign(scroll) * zoomPerNotch;
            rig.Apply();
            PushSlider();
        }

        private void LateUpdate()
        {
            ComposeOffset();

            // 拖曳旗標活過整整一幀，讓 MapView3D 有機會讀到
            if (pressButton < 0) IsDragging = false;
        }

        /// <summary>
        /// 每幀把「追隨目前節點」與「手動平移」合成最終的 pivotOffset。
        ///
        /// 【為什麼要拆成兩個變數】兩者的行為完全不同：
        ///   · 追隨是**持續**的，玩家走到哪就跟到哪
        ///   · 平移是**暫時**的，放手之後要飄回去
        /// 混在同一個變數裡的話，平移歸位會把追隨的位移一起歸掉，
        /// 鏡頭就會彈回地圖中心而不是回到玩家身上。
        ///
        /// 【為什麼平移要自動歸位】玩家推開地圖看遠處，看完之後
        /// 十之八九想回到自己身上。要他手動推回來是多餘的操作，
        /// 而且推不準 —— 沒有人能把地圖推回正中央。
        /// </summary>
        private void ComposeOffset()
        {
            if (rig == null) return;
            if (resetT >= 0f) return;    // 歸位動畫進行中，別跟它搶
            if (introPhase != IntroPhase.None) return;   // 開場縱覽自己在寫鏡頭

            // ── 追隨 ──
            if (followCurrentNode && followTarget != null)
            {
                Vector3 centre = rig.pivot != null ? rig.pivot.position : Vector3.zero;
                Vector3 want = followTarget.position - centre;

                // 地圖是平的，不要跟著節點的高度跑
                want.y = 0f;

                followOffset = Vector3.SmoothDamp(
                    followOffset, want, ref followVel, Mathf.Max(0.01f, followSmooth));
            }
            else
            {
                followOffset = Vector3.SmoothDamp(
                    followOffset, Vector3.zero, ref followVel, Mathf.Max(0.01f, followSmooth));
            }

            // ── 平移飄回 0 ──
            bool panning = pressedInside && pressButton == 0 && IsDragging;
            if (!panning)
            {
                sincePan += Time.unscaledDeltaTime;
                if (sincePan >= panReturnDelay)
                {
                    float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / panReturnSmooth);
                    panOffset = Vector3.Lerp(panOffset, Vector3.zero, k);
                    if (panOffset.sqrMagnitude < 0.0004f) panOffset = Vector3.zero;
                }
            }

            rig.pivotOffset = followOffset + panOffset;
            rig.Apply();
        }

        private void BeginPress(Vector2 pos, bool inside, int button)
        {
            pressPos = pos;
            lastPos = pos;
            pressedInside = inside;
            pressButton = inside ? button : -1;
            IsDragging = false;
        }

        private void Orbit(Vector2 delta)
        {
            float sign = invertDrag ? -1f : 1f;

            rig.yaw += sign * delta.x / Mathf.Max(1f, Screen.width) * yawPerScreen;
            if (!lockPitch) rig.pitch -= sign * delta.y / Mathf.Max(1f, Screen.height) * pitchPerScreen;
            rig.Apply();   // 夾持在 Apply 裡做
        }

        private void Pan(Vector2 delta)
        {
            // 平移要沿著**相機自己的**左右與前後，不是世界軸 ——
            // 不然轉過視角之後拖曳方向會跟手感對不起來
            Vector3 right = transform.right;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

            float kx = -delta.x / Mathf.Max(1f, Screen.width) * panPerScreen;
            float kz = -delta.y / Mathf.Max(1f, Screen.height) * panPerScreen;

            Vector3 next = panOffset + right * kx + fwd * kz;

            // 夾在範圍內。沒有這個的話玩家會把地圖推出畫面再也找不回來
            if (next.magnitude > panLimit) next = next.normalized * panLimit;

            panOffset = next;
            sincePan = 0f;      // 手還在動，重新計時
        }

        private void TickReset()
        {
            resetT += Time.unscaledDeltaTime;
            float t = resetSeconds <= 0f ? 1f : Mathf.Clamp01(resetT / resetSeconds);
            float e = Mathf.SmoothStep(0f, 1f, t);

            // LerpAngle：可以繞一整圈之後，轉到 170 度再歸位不該倒著轉回去
            rig.yaw = Mathf.LerpAngle(fromYaw, homeYaw, e);
            rig.pitch = Mathf.Lerp(fromPitch, homePitch, e);
            rig.distance = Mathf.Lerp(fromDistance, homeDistance, e);
            // ⚠️ 目標是 followOffset 不是 homeOffset ——
            //    追隨開著的時候「原位」就是玩家目前所在的節點
            Vector3 goal = (followCurrentNode && followTarget != null) ? followOffset : homeOffset;
            rig.pivotOffset = Vector3.Lerp(fromOffset, goal, e);
            rig.Apply();

            PushSlider();
            if (t >= 1f) resetT = -1f;
        }

        private bool IsInsideSurface(Vector2 screen)
        {
            if (surface == null) return true;   // 沒接就當整個畫面都算

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    surface.rectTransform, screen, null, out local)) return false;

            return surface.rectTransform.rect.Contains(local);
        }
    }
}

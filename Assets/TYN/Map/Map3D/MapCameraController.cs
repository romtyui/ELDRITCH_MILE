using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace EldritchMile.Map3D
{
    /// <summary>
    /// 玩家操作地圖視角。掛在 MapView3D 旁邊。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【設計取捨：為什麼拖曳是「轉」不是「平移」】
    /// 這張地圖是**桌上的一張紙**，不是 Google Maps。玩家看到傾斜的紙
    /// 第一個念頭是「把它轉過來看」，不是「把它推走」。
    /// 平移留給右鍵／中鍵，需要的人才會用到。
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

        [Header("門檻")]
        [Tooltip("滑鼠移動超過這麼多像素才算拖曳，否則算點擊。太小會誤判成拖曳而點不到節點")]
        public float dragThreshold = 6f;

        /// <summary>這一次按下之後有沒有被判定成拖曳。MapView3D 靠它決定要不要吃點擊。</summary>
        public bool IsDragging { get; private set; }

        private MapCameraRig rig;
        private Vector2 pressPos;
        private Vector2 lastPos;
        private bool pressedInside;
        private int pressButton = -1;

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
        }

        /// <summary>回到預設視角。給 UI 的鈕綁。</summary>
        public void ResetView()
        {
            fromYaw = rig.yaw; fromPitch = rig.pitch;
            fromDistance = rig.distance; fromOffset = rig.pivotOffset;
            resetT = 0f;
        }

        private void Update()
        {
            if (rig == null) return;

            if (resetT >= 0f) { TickReset(); return; }

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 pos = mouse.position.ReadValue();
            bool inside = IsInsideSurface(pos);

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
                    if (pressButton == 0) Orbit(delta);
                    else Pan(delta);
                }
            }

            lastPos = pos;

            // ── 滾輪縮放 ──
            if (!inside) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            rig.distance -= Mathf.Sign(scroll) * zoomPerNotch;
            rig.Apply();
        }

        private void LateUpdate()
        {
            // 拖曳旗標活過整整一幀，讓 MapView3D 有機會讀到
            if (pressButton < 0) IsDragging = false;
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
            rig.pitch -= sign * delta.y / Mathf.Max(1f, Screen.height) * pitchPerScreen;
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

            Vector3 next = rig.pivotOffset + right * kx + fwd * kz;

            // 夾在範圍內。沒有這個的話玩家會把地圖推出畫面再也找不回來
            if (next.magnitude > panLimit) next = next.normalized * panLimit;

            rig.pivotOffset = next;
            rig.Apply();
        }

        private void TickReset()
        {
            resetT += Time.unscaledDeltaTime;
            float t = resetSeconds <= 0f ? 1f : Mathf.Clamp01(resetT / resetSeconds);
            float e = Mathf.SmoothStep(0f, 1f, t);

            rig.yaw = Mathf.Lerp(fromYaw, homeYaw, e);
            rig.pitch = Mathf.Lerp(fromPitch, homePitch, e);
            rig.distance = Mathf.Lerp(fromDistance, homeDistance, e);
            rig.pivotOffset = Vector3.Lerp(fromOffset, homeOffset, e);
            rig.Apply();

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

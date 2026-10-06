using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EldritchMile.Core;



namespace EldritchMile.Map3D
{
    /// <summary>
    /// 3D 版地圖（階段 1：只傾斜，還不能旋轉／縮放）。
    ///
    /// 【為什麼走 RenderTexture 而不是第二台相機直接畫到螢幕】
    /// 地圖是**滑下來的覆蓋層**（見 MapOverlayController）—— 它動的是一個
    /// RectTransform。相機直接畫到螢幕的話滑不動，整個「下拉」就沒了。
    /// 所以 3D 世界畫進 RenderTexture，再由一個會跟著滑的 RawImage 顯示。
    /// 附帶好處：3D 世界與遊戲主相機完全隔離，不必動現有的 2D Renderer 設定。
    ///
    /// 【資料層零改動】
    /// 沿用 RunNodeData 的 xPercent / yPercent（**0~100，不是 0~1**），
    /// 直接鋪到地面的 XZ 平面。MapData 與 MapGenerator 一行都不用改。
    ///
    /// 【與舊的 MapView 並存】
    /// 舊那支留著沒動，要切回去只要換 prefab。萬一測試前發現不對，
    /// 退回去的成本必須接近零。
    /// </summary>
    public class MapView3D : MapOverlayController
    {
        [Header("3D 世界")]
        [Tooltip("世界空間的地圖根物件。不參與覆蓋層的滑動")]
        public Transform world;

        [Tooltip("只照地圖圖層的相機，輸出到 RenderTexture")]
        public Camera mapCamera;

        [Tooltip("顯示 RenderTexture 的 UI。這個會跟著地圖一起滑下來")]
        public RawImage surface;

        [Tooltip("視角操作。留空則不做拖曳／點擊的區分，任何點擊都會選節點")]
        public MapCameraController cameraController;

        [Tooltip("地面的世界尺寸（寬, 深）")]
        public Vector2 groundSize = new Vector2(20f, 20f);

        [Header("節點")]
        [Tooltip("單一節點 prefab。不同種類只換 pin 的圖，不做六個 prefab")]
        public MapNode3D nodePrefab;

        [Tooltip("節點浮在地面之上多高，避免 z-fighting")]
        public float nodeLift = 0.02f;

        [Tooltip("棋子的世界高度。**不用倍率是刻意的** —— 各種節點圖的解析度不同，寫倍率會大小不一")]
        public float pinWorldHeight = 0.9f;

        [Tooltip("底座圓環的世界直徑")]
        public float ringWorldSize = 0.5f;

        [Header("節點圖（依種類）")]
        public Sprite spriteEvent;
        public Sprite spriteCombat;
        public Sprite spriteElite;
        public Sprite spriteBoss;
        public Sprite spriteShop;
        public Sprite spriteSpecialEvent;

        [Tooltip("對話節點。留空會退回事件的圖")]
        public Sprite spriteDialogue;

        [Header("連線")]
        [Tooltip("連線材質。留空會用 Sprites/Default")]
        public Material lineMaterial;

        [Tooltip("線寬（世界單位）")]
        public float lineWidth = 0.04f;

        [Tooltip("線從節點中心往內縮多少，免得插進棋子裡")]
        public float lineEndGap = 0.25f;

        [Tooltip("連線的彎曲程度，佔線長的比例。0 = 直線，0.15 左右接近示意圖的弧線")]
        [Range(0f, 0.5f)] public float lineBend = 0.15f;

        [Tooltip("弧線用幾段折線畫。直線時忽略。太少會看出稜角")]
        [Range(2, 32)] public int lineSegments = 14;

        [Header("說明框")]
        [Tooltip("沿用平面版那一套。留空則不顯示")]
        public EldritchMile.Map.MapTooltipUI nodeTooltip;

        [Tooltip("一個空的 RectTransform，我會把它移到節點的螢幕位置給說明框貼")]
        public RectTransform tooltipAnchor;

        [Tooltip("各種節點的標題與說明。格式與平面版 MapView 相同。\n" +
                 "沒有對應條目的類型會退回顯示 enum 名稱")]
        public List<EldritchMile.Map.MapView.NodeTooltipInfo> nodeTooltipTexts =
            new List<EldritchMile.Map.MapView.NodeTooltipInfo>();

        [Tooltip("菁英戰的附註。一般雜魚刻意不標 —— 標示的價值來自稀有")]
        public string tooltipTierElite = "<color=#FF9B6A>◆ 菁英 —— 這一站不好惹。</color>";
        public string tooltipTierBoss = "<color=#FF6A6A>◆◆ 首領。</color>";

        [Tooltip("接在說明後面的一行，講「你現在能不能去」。留空則不附加")]
        public string tooltipStateCurrent = "<color=#FFD98A>你在這裡。</color>";
        public string tooltipStateSelectable = "<color=#9BE39B>可以前往。</color>";
        public string tooltipStateVisited = "<color=#8A8A8A>已經去過了。</color>";
        public string tooltipStateUnreachable = "<color=#8A8A8A>從這裡過不去。</color>";

        [Header("開場縱覽")]
        [Tooltip("每一場 run 第一次打開地圖時，先讓玩家看整張圖，再平滑移到起點。\n" +
                 "時間長短在 MapCameraController 的「開場縱覽」那一組調")]
        public bool playIntroOnNewMap = true;

        [Header("玩家棋子")]
        [Tooltip("玩家在地圖上的棋子。留空則不顯示")]
        public Sprite playerSprite;

        [Tooltip("棋子的世界高度。**下面那格大於 0 時不用這格**")]
        public float playerWorldHeight = 0.7f;

        [Tooltip("棋子高度 ＝ 一般節點（Pin World Height）的幾倍。大於 0 時優先於上面那格。\n" +
                 "用倍率是為了節點改大小時棋子跟著變（2026-09-14 定 75%）")]
        [Range(0f, 2f)] public float playerHeightOfNode = 0.75f;

        [Tooltip("棋子站在節點的哪一側（世界單位）。站正中間會被節點的圖擋住")]
        public Vector3 playerOffset = new Vector3(0.4f, 0f, -0.2f);

        [Tooltip("從一站移到下一站花幾秒")]
        public float playerMoveDuration = 0.9f;

        [Tooltip("位移的緩動。預設與平面版相同：推出去 → 滑行 → 摩擦煞停，不回彈")]
        public AnimationCurve playerMoveCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.28f, 0.55f, 2.1f, 2.1f),
            new Keyframe(1f, 1f, 0f, 0f));

        // ==========================================
        private MapData boundMap;
        private readonly Dictionary<string, MapNode3D> spawned = new Dictionary<string, MapNode3D>();
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private MapNode3D hovered;
        private bool moving;

        /// 說明框正在講的節點。鏡頭會動，所以每幀要重新對位
        private MapNode3D tooltipNode;

        private Transform playerRoot;

        // ==========================================
        public override void Refresh(RunContext run)
        {
            if (run == null || run.mapData == null) return;

            bool rebuilt = boundMap != run.mapData;
            if (rebuilt) Build(run.mapData);
            SyncState();

            // ⚠️ 在地圖滑下來**之前**就把縱覽擺好 —— 玩家看到的第一眼才是全圖。
            //    停留時間等 OnOpened（滑完）才開始算，見 MapCameraController.PrepareIntro
            if (rebuilt && playIntroOnNewMap && cameraController != null && spawned.Count > 0)
                cameraController.PrepareIntro(NodeBounds(), playerRoot);
        }

        /// <summary>所有節點（含棋子）的世界範圍。縱覽要把它整個裝進畫面。</summary>
        private Bounds NodeBounds()
        {
            bool first = true;
            Bounds b = new Bounds();

            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
            {
                if (kv.Value == null) continue;
                Vector3 p = kv.Value.transform.position;
                if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                else b.Encapsulate(p);
            }

            if (playerRoot != null) b.Encapsulate(playerRoot.position);
            return b;
        }

        protected override void OnClosing()
        {
            // 覆蓋層是滑出畫面的，子物件的 OnDisable 永遠不會觸發（見基底類別）。
            // hover 的殘留一定要在這裡清，不然說明框會浮在下一個畫面上
            if (hovered != null) { hovered.OnHoverExit(); hovered = null; }
            tooltipNode = null;

            // ⚠️ 用總開關，不是只 HideImmediate —— 理由見平面版 MapView.OnClosing
            if (nodeTooltip != null) nodeTooltip.SetSuppressed(true);
        }

        /// <summary>
        /// 地圖展開 → 說明框的總開關打開。
        ///
        /// ⚠️ **少了這一支說明框永遠不會出現。** MapTooltipUI 的 suppressed 一旦被設成 true
        /// （地圖第一次收起來時），之後所有 Show() 都會被吃掉。平面版在 OnOpened 解開，
        /// 3D 版一開始漏抄了 —— 實測 suppressed = True、Show 呼叫了但框沒開（2026-09-14）。
        /// </summary>
        public override IEnumerator OnOpened()
        {
            if (nodeTooltip != null) nodeTooltip.SetSuppressed(false);

            // 地圖滑完了才開始算縱覽的停留時間（沒準備過縱覽的話什麼都不會發生）
            if (cameraController != null) cameraController.StartIntro();

            yield return base.OnOpened();
        }

        // ==========================================
        // 建圖
        // ==========================================

        private void Build(MapData map)
        {
            Clear();
            boundMap = map;

            if (world == null || nodePrefab == null)
            {
                Debug.LogError("[地圖3D] world 或 nodePrefab 沒有指定，畫不出來", this);
                return;
            }

            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData d = map.allNodes[i];
                if (d == null) continue;

                MapNode3D n = Instantiate(nodePrefab, world);
                n.transform.localPosition = WorldPosOf(d);
                n.transform.localRotation = Quaternion.identity;
                n.gameObject.name = "Node_" + d.nodeId;

                if (n.pin != null)
                {
                    n.pin.sprite = SpriteFor(d);
                    FitPin(n.pin, pinWorldHeight);
                }
                if (n.ring != null) FitRing(n.ring);

                // 棋子要面向地圖相機，不是主相機
                YBillboard bb = n.pin != null ? n.pin.GetComponent<YBillboard>() : null;
                if (bb != null) bb.target = mapCamera;

                n.Init(d, this);
                spawned[d.nodeId] = n;
            }

            BuildLines(map);
            BuildPlayer();
        }

        /// <summary>
        /// 玩家的棋子。跟節點一樣是直立的、只繞 Y 軸面向地圖相機。
        ///
        /// 材質借節點 pin 的那一份 —— 用 SpriteRenderer 的預設材質的話，
        /// 在 2D Renderer 底下光照行為會跟節點不一樣，棋子會比地圖亮或暗一截。
        /// </summary>
        private void BuildPlayer()
        {
            if (playerSprite == null || world == null) return;

            GameObject root = new GameObject("Player");
            root.transform.SetParent(world, false);
            root.layer = world.gameObject.layer;

            GameObject pinGo = new GameObject("Pin");
            pinGo.transform.SetParent(root.transform, false);
            pinGo.layer = root.layer;

            SpriteRenderer sr = pinGo.AddComponent<SpriteRenderer>();
            sr.sprite = playerSprite;
            if (nodePrefab != null && nodePrefab.pin != null) sr.sharedMaterial = nodePrefab.pin.sharedMaterial;

            YBillboard bb = pinGo.AddComponent<YBillboard>();
            bb.target = mapCamera;

            FitPin(sr, playerHeightOfNode > 0f ? pinWorldHeight * playerHeightOfNode : playerWorldHeight);

            playerRoot = root.transform;
            playerRoot.localPosition = PlayerSpot(boundMap != null ? boundMap.currentNodeId : null) + playerOffset;
        }

        /// <summary>
        /// 棋子該站的位置（不含 playerOffset）。
        /// 還沒出發時站在第 0 層那幾站的前方。
        /// </summary>
        private Vector3 PlayerSpot(string nodeId)
        {
            MapNode3D n;
            if (!string.IsNullOrEmpty(nodeId) && spawned.TryGetValue(nodeId, out n) && n != null)
                return n.transform.localPosition;

            Vector3 sum = Vector3.zero;
            float minZ = float.MaxValue;
            int count = 0;

            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
            {
                if (kv.Value == null || kv.Value.Data.layer != 0) continue;
                Vector3 p = kv.Value.transform.localPosition;
                sum += p;
                minZ = Mathf.Min(minZ, p.z);
                count++;
            }

            if (count == 0) return Vector3.zero;

            Vector3 spot = sum / count;
            spot.z = minZ - 1f;
            return spot;
        }

        /// <summary>
        /// 棋子沿著**跟連線同一條弧線**滑過去。
        ///
        /// 【為什麼要沿弧線】連線是彎的，棋子走直線的話會切過地面、偏離畫出來的路，
        /// 看起來像抄捷徑。控制點用同一支 CurveControl 算，兩者必定重合。
        ///
        /// 移動期間鏡頭改追棋子 —— 不然棋子會滑出畫面。
        /// </summary>
        private IEnumerator MovePlayer(Vector3 from, Vector3 to)
        {
            if (playerRoot == null) yield break;

            if (cameraController != null) cameraController.followTarget = playerRoot;

            Vector3 ctrl = CurveControl(from, to);
            float dur = Mathf.Max(0.01f, playerMoveDuration);
            float t = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = playerMoveCurve.Evaluate(Mathf.Clamp01(t / dur));
                playerRoot.localPosition = Bezier(from, ctrl, to, p) + playerOffset;
                yield return null;
            }

            playerRoot.localPosition = to + playerOffset;
        }

        /// <summary>
        /// 把棋子縮到指定的世界高度，並讓它**站在地面上**（底邊貼地，不是中心貼地）。
        ///
        /// 【為什麼不用固定倍率】節點圖的解析度各不相同（512、442…），
        /// 同一個 localScale 會讓它們大小差好幾倍。指定「要多高」才是穩的。
        /// </summary>
        private static void FitPin(SpriteRenderer sr, float worldHeight)
        {
            if (sr == null || sr.sprite == null) return;

            float h = sr.sprite.bounds.size.y;
            if (h <= 0.0001f) return;

            float k = worldHeight / h;
            sr.transform.localScale = new Vector3(k, k, 1f);

            // sprite 的樞紐在中心，所以往上抬半個身高才會底邊貼地
            Vector3 p = sr.transform.localPosition;
            p.y = worldHeight * 0.5f;
            sr.transform.localPosition = p;
        }

        private void FitRing(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return;

            float w = sr.sprite.bounds.size.x;
            if (w <= 0.0001f) return;

            float k = ringWorldSize / w;
            sr.transform.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>
        /// xPercent / yPercent（**0~100**）換算成地面上的座標。
        /// y 走的是 Z 軸 —— 地面是平躺的，畫面上的「上」在 3D 裡是「遠」。
        /// </summary>
        private Vector3 WorldPosOf(RunNodeData d)
        {
            return new Vector3(
                (d.xPercent / 100f - 0.5f) * groundSize.x,
                nodeLift,
                (d.yPercent / 100f - 0.5f) * groundSize.y);
        }

        /// <summary>
        /// 這一種節點用哪張圖。
        ///
        /// ⚠️ **每一條都要有退路。** 沒有退路的話，沒指定圖的種類會變成
        /// 「有節點、有連線，但畫面上什麼都沒有」—— 玩家看到的是線連到空氣。
        /// 平面版的 `PrefabFor` 本來就有這條退路，3D 版一開始漏掉了，
        /// 實測 40 個節點有 7 個是隱形的（6 商店 + 1 特殊事件）。
        /// </summary>
        private Sprite SpriteFor(RunNodeData d)
        {
            Sprite s;
            switch (d.kind)
            {
                case MapNodeKind.Combat:
                    // 菁英不是一種 MapNodeKind，是 Combat 加上 enemyTier —— 與平面版同一條規則
                    s = d.enemyTier == EncounterPool.Tier.Elite && spriteElite != null
                        ? spriteElite : spriteCombat;
                    break;
                case MapNodeKind.Boss: s = spriteBoss; break;
                case MapNodeKind.Shop: s = spriteShop; break;
                case MapNodeKind.SpecialEvent: s = spriteSpecialEvent; break;
                case MapNodeKind.Dialogue: s = spriteDialogue; break;
                default: s = spriteEvent; break;
            }

            if (s == null) s = spriteEvent;

            if (s == null)
                Debug.LogWarning("[地圖3D]「" + d.kind + "」沒有圖，連 spriteEvent 也是空的 —— "
                    + "這個節點會是隱形的（線連過去但看不到東西）", this);

            return s;
        }

        /// <summary>
        /// 畫連線。
        ///
        /// ⚠️ **`alignment` 一定要是 `TransformZ`，不能用預設的 `View`。**
        /// `View` 會讓線永遠轉向面對相機 —— 俯視時線就「立起來」變成緞帶，
        /// 而且相機一轉整條線跟著扭。地面上的路必須躺在地面上。
        ///
        /// 為了讓 `TransformZ` 的法線朝上，線物件要轉 -90 度（local +Z → world +Y），
        /// 因此世界座標 (x, ?, z) 要寫成 local (x, -z, 0)。這個換算不直覺，
        /// 改的時候先看清楚再動。
        ///
        /// 【弧線】二次貝茲，控制點往垂直方向偏 `lineBend` × 線長。
        /// 弧線躺在地面上，所以透視怎麼壓底圖就怎麼壓它 —— 不會有額外的變形。
        /// </summary>
        private void BuildLines(MapData map)
        {
            Material mat = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));

            for (int i = 0; i < map.allNodes.Count; i++)
            {
                RunNodeData a = map.allNodes[i];
                if (a == null || a.nextNodeIds == null) continue;

                for (int k = 0; k < a.nextNodeIds.Count; k++)
                {
                    RunNodeData b = map.GetNode(a.nextNodeIds[k]);
                    if (b == null) continue;

                    GameObject go = new GameObject("Line_" + a.nodeId + "_" + b.nodeId);
                    go.transform.SetParent(world, false);
                    go.layer = world.gameObject.layer;

                    // local +Z 轉成 world +Y，線才會躺著
                    go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    go.transform.localPosition = new Vector3(0f, nodeLift * 0.5f, 0f);

                    LineRenderer lr = go.AddComponent<LineRenderer>();
                    lr.useWorldSpace = false;
                    lr.alignment = LineAlignment.TransformZ;
                    lr.material = mat;
                    lr.widthMultiplier = lineWidth;
                    lr.numCapVertices = 2;
                    lr.numCornerVertices = 4;
                    lr.textureMode = LineTextureMode.Tile;

                    Vector3 pa = WorldPosOf(a);
                    Vector3 pb = WorldPosOf(b);

                    // ⚠️ 控制點用**節點中心**算，不是縮短後的端點 ——
                    //    棋子的移動也用節點中心算，兩邊才會是同一條弧
                    Vector3 ctrl = CurveControl(pa, pb);

                    Vector3 dir = (pb - pa).normalized;
                    pa += dir * lineEndGap;
                    pb -= dir * lineEndGap;

                    WriteCurve(lr, pa, pb, ctrl);

                    // 之前漏了這一行 —— Clear() 清不到線，換一場 run 舊線會留在地上
                    lines.Add(lr);
                }
            }
        }

        /// <summary>
        /// 把一條（可能彎的）路寫進 LineRenderer。
        /// 座標要從世界的 XZ 換成線物件的 local XY —— 見 BuildLines 的說明。
        /// </summary>
        private void WriteCurve(LineRenderer lr, Vector3 pa, Vector3 pb, Vector3 ctrl)
        {
            if (lineBend <= 0.001f)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(pa.x, -pa.z, 0f));
                lr.SetPosition(1, new Vector3(pb.x, -pb.z, 0f));
                return;
            }

            int n = Mathf.Max(2, lineSegments);
            lr.positionCount = n;

            for (int i = 0; i < n; i++)
            {
                Vector3 p = Bezier(pa, ctrl, pb, (float)i / (n - 1));
                lr.SetPosition(i, new Vector3(p.x, -p.z, 0f));
            }
        }

        /// <summary>
        /// 兩站之間那條弧線的控制點。**連線與棋子移動共用這一支** —— 兩邊各算各的，
        /// 棋子就會偏離畫出來的路。lineBend 為 0 時就是中點（直線）。
        /// </summary>
        private Vector3 CurveControl(Vector3 pa, Vector3 pb)
        {
            Vector3 mid = (pa + pb) * 0.5f;
            if (lineBend <= 0.001f) return mid;

            Vector3 d = pb - pa;

            // 垂直於連線、躺在地面上的方向
            Vector3 perp = new Vector3(-d.z, 0f, d.x).normalized;

            // 往哪邊彎由兩端的座標決定 —— 同一組節點每次都彎同一邊，
            // 不然重建地圖時線會左右亂跳
            float side = (pa.x + pb.z) >= 0f ? 1f : -1f;
            return mid + perp * (d.magnitude * lineBend * side);
        }

        /// <summary>二次貝茲。</summary>
        private static Vector3 Bezier(Vector3 a, Vector3 c, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

        private void Clear()
        {
            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
                if (kv.Value != null) DestroyImmediate(kv.Value.gameObject);
            spawned.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (lines[i] != null) DestroyImmediate(lines[i].gameObject);
            lines.Clear();

            if (playerRoot != null) DestroyImmediate(playerRoot.gameObject);
            playerRoot = null;

            hovered = null;
            tooltipNode = null;
        }

        // ==========================================
        private void SyncState()
        {
            if (boundMap == null) return;

            RunNodeData cur = boundMap.CurrentNode;
            List<string> reachable = cur != null ? cur.nextNodeIds : new List<string>();
            bool atStart = string.IsNullOrEmpty(boundMap.currentNodeId);

            MapNode3D current = null;

            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
            {
                MapNode3D n = kv.Value;
                if (n == null) continue;

                bool isCurrent = kv.Key == boundMap.currentNodeId;
                bool selectable = atStart ? n.Data.layer == 0 : reachable.Contains(kv.Key);
                n.UpdateVisual(isCurrent, selectable, n.Data.visited);

                if (isCurrent) current = n;
            }

            // 鏡頭追隨目前所在的節點。開場還沒選過節點時 current 是 null，
            // 那時 followTarget 留空 —— 鏡頭會停在地圖中心，正好是「縱覽全局」
            // 還沒出發時追起點的棋子（2026-09-15：開地圖要帶玩家看到自己在哪）。
            // 縱覽進行中不碰 —— 縱覽結束時它自己會把目標交過來
            if (cameraController != null && !moving && !cameraController.IsPlayingIntro)
                cameraController.followTarget = current != null
                    ? current.transform
                    : playerRoot;

            // 棋子歸位。移動中不碰 —— 那時位置由 MovePlayer 在寫
            if (playerRoot != null && !moving)
                playerRoot.localPosition = PlayerSpot(boundMap.currentNodeId) + playerOffset;
        }

        // ==========================================
        // 輸入：從 RawImage 上的座標打一條射線進 3D 世界
        // ==========================================

        /// <summary>
        /// 【為什麼不能用 uGUI 的事件】節點現在是世界空間的物件，不在 Canvas 底下，
        /// EventSystem 打不到它們。
        ///
        /// 【為什麼要先換算成 RawImage 的局部座標】相機畫的是 RenderTexture 不是螢幕。
        /// 直接拿滑鼠的螢幕座標去 ScreenPointToRay 會整個對不上 —— 必須先問
        /// 「滑鼠落在 RawImage 的哪個比例位置」，那個比例才是視埠座標。
        ///
        /// ⚠️ 一定要走 Input System 的 Mouse.current 並判 null：
        /// 舊的 UnityEngine.Input 編得過但執行時會洗版例外（見 RunDebugPanel）。
        /// </summary>
        private void Update()
        {
            if (!IsOpen || moving || surface == null || mapCamera == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    surface.rectTransform, mouse.position.ReadValue(), null, out local))
            {
                SetHovered(null);
                return;
            }

            Rect r = surface.rectTransform.rect;
            Vector2 vp = new Vector2((local.x - r.x) / r.width, (local.y - r.y) / r.height);

            if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) { SetHovered(null); return; }

            RaycastHit hit;
            MapNode3D found = null;
            if (Physics.Raycast(mapCamera.ViewportPointToRay(vp), out hit, 1000f))
                found = hit.collider.GetComponentInParent<MapNode3D>();

            SetHovered(found);

            // ⚠️ 改成「放開」才算點，而且拖曳過就不算。
            //    左鍵同時用來轉視角，沒有這道判斷的話玩家想轉視角卻會誤觸節點 ——
            //    而進關卡是不可逆的
            if (found == null || !mouse.leftButton.wasReleasedThisFrame) return;
            if (cameraController != null && cameraController.IsDragging) return;

            found.OnClicked();
        }

        private void SetHovered(MapNode3D n)
        {
            if (hovered == n) return;

            if (hovered != null) hovered.OnHoverExit();
            hovered = n;
            if (hovered != null) hovered.OnHoverEnter();
        }

        // ==========================================
        // 說明框：把節點的世界位置換算到螢幕，再借用平面版那一套
        // ==========================================

        public void ShowNodeTooltip(MapNode3D node)
        {
            if (nodeTooltip == null || tooltipAnchor == null || node == null || node.Data == null) return;
            if (!PlaceTooltipAnchor(node)) return;

            string title, body;
            BuildTooltipText(node, out title, out body);

            tooltipNode = node;
            nodeTooltip.Show(title, body, tooltipAnchor);
        }

        public void HideNodeTooltip(MapNode3D node)
        {
            if (tooltipNode == node) tooltipNode = null;
            if (nodeTooltip != null) nodeTooltip.Hide();
        }

        /// <summary>
        /// 鏡頭會追隨、拖曳、縮放 —— 節點在畫面上的位置每幀都可能變，框要跟著走。
        /// 放 LateUpdate 是為了盡量排在鏡頭移動之後。
        /// </summary>
        private void LateUpdate()
        {
            if (tooltipNode == null || nodeTooltip == null || !IsOpen) return;
            if (PlaceTooltipAnchor(tooltipNode)) nodeTooltip.Follow(tooltipAnchor);
        }

        /// <summary>
        /// 把 tooltipAnchor 移到**棋子頭頂**在畫面上的位置。
        ///
        /// 【為什麼是頭頂不是節點中心】節點中心在地面上，框貼那裡會蓋住棋子本身。
        /// 【為什麼先換成 RawImage 的比例】相機畫的是 RenderTexture，
        /// 視埠座標要對應到 RawImage 的矩形，不是整個螢幕。
        /// </summary>
        private bool PlaceTooltipAnchor(MapNode3D node)
        {
            if (mapCamera == null || surface == null || tooltipAnchor == null || node == null) return false;

            Vector3 head = node.transform.position;
            if (node.pin != null)
                head = node.pin.transform.position
                     + node.transform.up * (pinWorldHeight * 0.5f * node.transform.lossyScale.y);

            Vector3 vp = mapCamera.WorldToViewportPoint(head);
            if (vp.z <= 0f) return false;   // 在鏡頭背後

            Rect r = surface.rectTransform.rect;

            if (tooltipAnchor.parent != surface.rectTransform)
                tooltipAnchor.SetParent(surface.rectTransform, false);

            tooltipAnchor.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipAnchor.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipAnchor.sizeDelta = Vector2.zero;
            tooltipAnchor.anchoredPosition = new Vector2(
                (vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);
            return true;
        }

        /// <summary>標題、難度、說明、狀態 —— 與平面版 MapView 同一套規則。</summary>
        private void BuildTooltipText(MapNode3D node, out string title, out string body)
        {
            RunNodeData d = node.Data;

            EldritchMile.Map.MapView.NodeTooltipInfo info = null;
            for (int i = 0; i < nodeTooltipTexts.Count; i++)
                if (nodeTooltipTexts[i] != null && nodeTooltipTexts[i].kind == d.kind) { info = nodeTooltipTexts[i]; break; }

            title = info != null && !string.IsNullOrEmpty(info.title) ? info.title : d.kind.ToString();
            body = info != null ? info.body : "";

            // 難度先講 —— 「這站硬不硬」是玩家在分岔口最想知道的事
            string tier = "";
            if (d.kind == MapNodeKind.Combat || d.kind == MapNodeKind.Boss)
            {
                if (d.enemyTier == EncounterPool.Tier.Elite) tier = tooltipTierElite;
                else if (d.enemyTier == EncounterPool.Tier.Boss) tier = tooltipTierBoss;
            }
            if (!string.IsNullOrEmpty(tier))
                body = string.IsNullOrEmpty(body) ? tier : tier + "\n" + body;

            string state;
            switch (node.NodeState)
            {
                case MapNode3D.State.Current: state = tooltipStateCurrent; break;
                case MapNode3D.State.Selectable: state = tooltipStateSelectable; break;
                case MapNode3D.State.Visited: state = tooltipStateVisited; break;
                default: state = tooltipStateUnreachable; break;
            }
            if (!string.IsNullOrEmpty(state))
                body = string.IsNullOrEmpty(body) ? state : body + "\n" + state;
        }

        // ==========================================
        public void OnNodeClicked(RunNodeData node)
        {
            if (node == null || moving) return;
            if (GameFlowManager.Instance == null) return;
            if (GameFlowManager.Instance.IsTransitioning) return;

            tooltipNode = null;
            if (nodeTooltip != null) nodeTooltip.HideImmediate();
            StartCoroutine(EnterAfterFrame(node));
        }

        private IEnumerator EnterAfterFrame(RunNodeData node)
        {
            moving = true;

            if (playerRoot != null && boundMap != null)
                yield return MovePlayer(PlayerSpot(boundMap.currentNodeId), PlayerSpot(node.nodeId));
            else
                yield return null;

            moving = false;

            // 收地圖、載入 Stage 全交給總管（鐵則 1：畫面層不做流程決策）
            GameFlowManager.Instance.EnterNode(node);
        }
    }
}

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

        // ==========================================
        private MapData boundMap;
        private readonly Dictionary<string, MapNode3D> spawned = new Dictionary<string, MapNode3D>();
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private MapNode3D hovered;
        private bool moving;

        // ==========================================
        public override void Refresh(RunContext run)
        {
            if (run == null || run.mapData == null) return;

            if (boundMap != run.mapData) Build(run.mapData);
            SyncState();
        }

        protected override void OnClosing()
        {
            // 覆蓋層是滑出畫面的，子物件的 OnDisable 永遠不會觸發（見基底類別）。
            // hover 的殘留一定要在這裡清，不然說明框會浮在下一個畫面上
            if (hovered != null) { hovered.OnHoverExit(); hovered = null; }
            if (nodeTooltip != null) nodeTooltip.HideImmediate();
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
                    FitPin(n.pin);
                }
                if (n.ring != null) FitRing(n.ring);

                // 棋子要面向地圖相機，不是主相機
                YBillboard bb = n.pin != null ? n.pin.GetComponent<YBillboard>() : null;
                if (bb != null) bb.target = mapCamera;

                n.Init(d, this);
                spawned[d.nodeId] = n;
            }

            BuildLines(map);
        }

        /// <summary>
        /// 把棋子縮到指定的世界高度，並讓它**站在地面上**（底邊貼地，不是中心貼地）。
        ///
        /// 【為什麼不用固定倍率】節點圖的解析度各不相同（512、442…），
        /// 同一個 localScale 會讓它們大小差好幾倍。指定「要多高」才是穩的。
        /// </summary>
        private void FitPin(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return;

            float h = sr.sprite.bounds.size.y;
            if (h <= 0.0001f) return;

            float k = pinWorldHeight / h;
            sr.transform.localScale = new Vector3(k, k, 1f);

            // sprite 的樞紐在中心，所以往上抬半個身高才會底邊貼地
            Vector3 p = sr.transform.localPosition;
            p.y = pinWorldHeight * 0.5f;
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

        private Sprite SpriteFor(RunNodeData d)
        {
            switch (d.kind)
            {
                case MapNodeKind.Combat:
                    // 菁英不是一種 MapNodeKind，是 Combat 加上 enemyTier —— 與平面版同一條規則
                    return d.enemyTier == EncounterPool.Tier.Elite && spriteElite != null
                        ? spriteElite : spriteCombat;
                case MapNodeKind.Boss: return spriteBoss;
                case MapNodeKind.Shop: return spriteShop;
                case MapNodeKind.SpecialEvent: return spriteSpecialEvent;
                default: return spriteEvent;
            }
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

                    Vector3 dir = (pb - pa).normalized;
                    pa += dir * lineEndGap;
                    pb -= dir * lineEndGap;

                    WriteCurve(lr, pa, pb);
                }
            }
        }

        /// <summary>
        /// 把一條（可能彎的）路寫進 LineRenderer。
        /// 座標要從世界的 XZ 換成線物件的 local XY —— 見 BuildLines 的說明。
        /// </summary>
        private void WriteCurve(LineRenderer lr, Vector3 pa, Vector3 pb)
        {
            if (lineBend <= 0.001f)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(pa.x, -pa.z, 0f));
                lr.SetPosition(1, new Vector3(pb.x, -pb.z, 0f));
                return;
            }

            Vector3 mid = (pa + pb) * 0.5f;
            Vector3 d = pb - pa;

            // 垂直於連線、躺在地面上的方向
            Vector3 perp = new Vector3(-d.z, 0f, d.x).normalized;

            // 往哪邊彎由兩端的座標決定 —— 同一組節點每次都彎同一邊，
            // 不然重建地圖時線會左右亂跳
            float side = (pa.x + pb.z) >= 0f ? 1f : -1f;
            Vector3 ctrl = mid + perp * (d.magnitude * lineBend * side);

            int n = Mathf.Max(2, lineSegments);
            lr.positionCount = n;

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                float u = 1f - t;

                // 二次貝茲
                Vector3 p = u * u * pa + 2f * u * t * ctrl + t * t * pb;
                lr.SetPosition(i, new Vector3(p.x, -p.z, 0f));
            }
        }

        private void Clear()
        {
            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
                if (kv.Value != null) DestroyImmediate(kv.Value.gameObject);
            spawned.Clear();

            for (int i = 0; i < lines.Count; i++)
                if (lines[i] != null) DestroyImmediate(lines[i].gameObject);
            lines.Clear();

            hovered = null;
        }

        // ==========================================
        private void SyncState()
        {
            if (boundMap == null) return;

            RunNodeData cur = boundMap.CurrentNode;
            List<string> reachable = cur != null ? cur.nextNodeIds : new List<string>();
            bool atStart = string.IsNullOrEmpty(boundMap.currentNodeId);

            foreach (KeyValuePair<string, MapNode3D> kv in spawned)
            {
                MapNode3D n = kv.Value;
                if (n == null) continue;

                bool isCurrent = kv.Key == boundMap.currentNodeId;
                bool selectable = atStart ? n.Data.layer == 0 : reachable.Contains(kv.Key);
                n.UpdateVisual(isCurrent, selectable, n.Data.visited);
            }
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
            if (nodeTooltip == null || tooltipAnchor == null || node == null) return;
            if (mapCamera == null || surface == null) return;

            Vector3 vp = mapCamera.WorldToViewportPoint(node.transform.position);
            Rect r = surface.rectTransform.rect;

            tooltipAnchor.SetParent(surface.rectTransform, false);
            tooltipAnchor.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipAnchor.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipAnchor.anchoredPosition = new Vector2(
                (vp.x - 0.5f) * r.width, (vp.y - 0.5f) * r.height);

            nodeTooltip.Show(node.Data.kind.ToString(), "", tooltipAnchor);
        }

        public void HideNodeTooltip(MapNode3D node)
        {
            if (nodeTooltip != null) nodeTooltip.Hide();
        }

        // ==========================================
        public void OnNodeClicked(RunNodeData node)
        {
            if (node == null || moving) return;
            if (GameFlowManager.Instance == null) return;
            if (GameFlowManager.Instance.IsTransitioning) return;

            if (nodeTooltip != null) nodeTooltip.HideImmediate();
            StartCoroutine(EnterAfterFrame(node));
        }

        private IEnumerator EnterAfterFrame(RunNodeData node)
        {
            moving = true;
            yield return null;   // 階段 1 還沒有棋子移動動畫，先留這一格
            moving = false;

            // 收地圖、載入 Stage 全交給總管（鐵則 1：畫面層不做流程決策）
            GameFlowManager.Instance.EnterNode(node);
        }
    }
}

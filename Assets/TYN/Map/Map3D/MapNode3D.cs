using UnityEngine;
using EldritchMile.Core;

// ⚠️ Assets/TYN/_Archive/Scripts/PerspectiveMapGenerator.cs 在**全域命名空間**
//    也定義了 RunNodeData 與 MapData，會蓋掉 EldritchMile.Core 那組
//    （症狀是「MapData 沒有 GetNode」這種看起來莫名其妙的錯誤）。
//    別名不能跟全域型別同名（CS0576），所以前面加 Core。
using CoreMapData = EldritchMile.Core.MapData;
using CoreNode = EldritchMile.Core.RunNodeData;


namespace EldritchMile.Map3D
{
    /// <summary>
    /// 3D 地圖上的一個節點 —— 「插在地圖上的棋子」。
    ///
    /// 由兩件東西組成，**兩件的行為刻意相反**：
    ///   · <see cref="ring"/>  躺在地面上，跟著透視變形。3D 感就是它給的
    ///   · <see cref="pin"/>   直立，只繞 Y 軸轉身面對相機（見 YBillboard）
    ///
    /// 【為什麼判定區掛在根物件而不是 pin 上】
    /// pin 會一直轉，collider 跟著轉的話點擊範圍會忽大忽小。
    /// 根物件不轉，判定穩定 —— 玩家點的是「這個位置」，不是「這張圖」。
    /// </summary>
    public class MapNode3D : MonoBehaviour
    {
        public CoreNode Data { get; private set; }

        [Header("組件")]
        [Tooltip("直立的棋子本體。應該掛 YBillboard")]
        public SpriteRenderer pin;

        [Tooltip("躺在地面的底座圓環。不要掛 YBillboard")]
        public SpriteRenderer ring;

        [Tooltip("點擊判定。留空會在自己身上找")]
        public Collider hitbox;

        [Header("狀態顏色")]
        public Color colorCurrent = new Color(1f, 0.85f, 0.55f, 1f);
        public Color colorSelectable = Color.white;
        public Color colorVisited = new Color(0.55f, 0.55f, 0.55f, 1f);
        public Color colorUnreachable = new Color(0.35f, 0.35f, 0.35f, 0.65f);

        [Header("大小")]
        [Tooltip("目前所在的節點放大倍率")]
        public float scaleCurrent = 1.25f;

        [Tooltip("可前往的節點倍率")]
        public float scaleSelectable = 1f;

        [Tooltip("去不了的節點倍率")]
        public float scaleInactive = 0.8f;

        [Tooltip("滑鼠移上去時，棋子往上抬幾個世界單位。0 = 不抬")]
        public float hoverLift = 0.12f;

        public enum State { Current, Selectable, Visited, Unreachable }
        public State NodeState { get; private set; } = State.Unreachable;

        /// <summary>可以點嗎。只有 Selectable 能點</summary>
        public bool IsSelectable { get { return NodeState == State.Selectable; } }

        private MapView3D view;
        private Vector3 pinHome;
        private bool pinHomeCaptured;

        // ==========================================
        public void Init(CoreNode data, MapView3D owner)
        {
            Data = data;
            view = owner;

            if (hitbox == null) hitbox = GetComponent<Collider>();

            // ⚠️ 這裡不抓 pinHome —— 生成當下 pin 可能還沒被擺到定位。
            //    第一次 hover 才抓，跟快捷欄格子踩過的坑是同一個
            pinHomeCaptured = false;
        }

        public void UpdateVisual(bool isCurrent, bool selectable, bool isVisited)
        {
            if (isCurrent) NodeState = State.Current;
            else if (selectable) NodeState = State.Selectable;
            else if (isVisited) NodeState = State.Visited;
            else NodeState = State.Unreachable;

            Color c;
            float s;

            switch (NodeState)
            {
                case State.Current: c = colorCurrent; s = scaleCurrent; break;
                case State.Selectable: c = colorSelectable; s = scaleSelectable; break;
                case State.Visited: c = colorVisited; s = scaleInactive; break;
                default: c = colorUnreachable; s = scaleInactive; break;
            }

            if (pin != null) pin.color = c;

            // 圓環比棋子暗一階 —— 它是投影不是主體，一樣亮會搶掉棋子
            if (ring != null) ring.color = new Color(c.r, c.g, c.b, c.a * 0.75f);

            transform.localScale = new Vector3(s, s, s);
        }

        // ==========================================
        // 由 MapView3D 的射線偵測呼叫（不是 uGUI 事件 —— 這裡是世界空間）
        // ==========================================

        public void OnHoverEnter()
        {
            if (!IsSelectable || pin == null) return;

            EnsurePinHome();
            pin.transform.localPosition = pinHome + new Vector3(0f, hoverLift, 0f);
            if (view != null) view.ShowNodeTooltip(this);
        }

        public void OnHoverExit()
        {
            if (pin != null && pinHomeCaptured) pin.transform.localPosition = pinHome;
            if (view != null) view.HideNodeTooltip(this);
        }

        public void OnClicked()
        {
            if (!IsSelectable) return;
            if (view != null) view.OnNodeClicked(Data);
        }

        private void EnsurePinHome()
        {
            if (pinHomeCaptured || pin == null) return;
            pinHome = pin.transform.localPosition;
            pinHomeCaptured = true;
        }
    }
}

using UnityEngine;

namespace EldritchMile.Explore
{
    /// <summary>
    /// 房間內的「預定位子」。C6：
    /// 「寶箱會以各種角度[隨機]出現在預定的位子(室內室外皆有)」
    ///
    /// 【與舊版的差別】封存的 RoomController 是直接 GetComponentsInChildren 掃
    /// 已經擺好的物件 —— 完全靜態，每次進同一個房間看到的東西一模一樣。
    /// 改成 slot 之後，位置是設計好的，內容與角度是隨機的。
    /// </summary>
    public class SpawnSlot : MonoBehaviour
    {
        public enum Placement
        {
            Any,
            Indoor,
            Outdoor,
        }

        [Header("位置屬性")]
        public Placement placement = Placement.Any;

        [Tooltip("這個位子與內容的**對號入座標記**。留空 = 不限。\n\n" +
                 "⚠️ **是雙向的**：\n" +
                 "　· 位子有 tag → 只收同 tag 的內容\n" +
                 "　· 內容有 tag → 只進同 tag 的位子\n\n" +
                 "只做單向（位子挑內容）的話沒有用 —— 沒有 tag 的位子照樣會把\n" +
                 "那個內容抽走，專屬的位子就撲空了。「畫在背景上的櫃子」正是這種情況：\n" +
                 "櫃子被隨機擺到別處，畫上去的那一格反而空著。")]
        public string contentTag = "";

        [Header("隨機角度（C6：以各種角度出現）")]
        [Tooltip("生成物繞 Z 軸的隨機旋轉範圍（度）")]
        public Vector2 rotationRange = new Vector2(-12f, 12f);

        [Tooltip("隨機縮放範圍。1,1 表示不縮放")]
        public Vector2 scaleRange = new Vector2(1f, 1f);

        [Header("釘在美術上的位子")]
        [Tooltip("**這一格的家具美術已經畫在背景上了**，生成物只當「碰得到的東西」。\n\n" +
                 "生成之後會把它的圖**透明度調成 0** —— 畫面上看到的是美術原稿，\n" +
                 "但碰撞框、hover、點擊全部照常。\n\n" +
                 "⚠️ **是調 alpha，不是 `enabled = false`。**\n" +
                 "停用 Renderer／Image 的話整個東西就點不到了 ——\n" +
                 "這個專案在快捷欄踩過三次同一個坑（見交接文件「坑 1」）。\n\n" +
                 "⚠️ 打開這一格時，`Rotation Range` 與 `Scale Range` 要歸零 ——\n" +
                 "畫上去的櫃子不會歪，碰撞框歪了就對不上。\n\n" +
                 "⚠️ 代價：`InteractableBase` 的「互動後換圖」看不見了（圖是透明的）。\n" +
                 "玩家的回饋只剩戰利品播報。畫上去的家具本來就不會變，這是這個做法的極限。")]
        public bool artIsPainted = false;

        [Tooltip("這一格**佔住多大一塊地**（世界單位，以位子為中心）。\n" +
                 "0,0 ＝ 不佔地。\n\n" +
                 "【要解決什麼】美術把櫃子畫在牆上，但別的位子就在它前面 ——\n" +
                 "隨機生成的寶箱會直接蓋在那個櫃子上，畫面變成「櫃子前面浮著一個寶箱」。\n" +
                 "填了尺寸之後，**footprint 跟這塊地重疊的位子會整格跳過**。\n\n" +
                 "中屋那個矮櫃量出來是 4.59 × 3.40。\n\n" +
                 "⚠️ 代價是那間房少一兩個位子。要把它們要回來就**把那些位子挪開**，\n" +
                 "挪到不重疊的地方（見 HANDOFF 的安全區數字），這一格就不會擋到它們。")]
        public Vector2 reserveSize = Vector2.zero;

        /// <summary>這一格佔住的那塊地（世界座標）。沒填尺寸就回一個空的。</summary>
        public Rect ReservedArea
        {
            get
            {
                if (reserveSize.x <= 0f || reserveSize.y <= 0f) return Rect.zero;

                Vector3 c = transform.position;
                return new Rect(c.x - reserveSize.x * 0.5f, c.y - reserveSize.y * 0.5f,
                                reserveSize.x, reserveSize.y);
            }
        }

        [Header("機率")]
        [Tooltip("這個位子有東西的機率。1 = 一定有")]
        [Range(0f, 1f)] public float fillChance = 1f;

        /// 執行時由 RoomController 設定
        public bool IsOccupied { get; private set; }

        public void MarkOccupied() => IsOccupied = true;

        public bool Accepts(Placement contentPlacement, string tag)
        {
            if (placement != Placement.Any &&
                contentPlacement != Placement.Any &&
                placement != contentPlacement)
            {
                return false;
            }

            // 位子有 tag → 只收同 tag 的內容
            if (!string.IsNullOrEmpty(contentTag) && contentTag != tag) return false;

            // 內容有 tag → 只進同 tag 的位子（**這一半以前沒有**，見 contentTag 的說明）
            if (!string.IsNullOrEmpty(tag) && contentTag != tag) return false;

            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.green : new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.4f);
        }
    }
}

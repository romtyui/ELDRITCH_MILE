using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    /// <summary>
    /// 場景裡「可有可無」的美術組件。掛在 `Art_*` 的根上。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【要解決什麼】美術給的一組背景配十幾張家具／物件。整組原封不動地擺出來，
    /// 每一間房都長得一模一樣 —— 玩家走十間屋子會覺得只走了一間。
    ///
    /// 做法是**每一件各自決定出不出現**，而且**在地圖生成時就決定**、存在節點上。
    ///
    /// 【為什麼是地圖生成時，不是進房間才擲】
    ///
    ///   1. **同一個節點重進要長一樣。**現場擲的話，玩家離開再進來整間房就變了，
    ///      那會讓人懷疑自己記錯，比「每間都一樣」更糟。
    ///   2. **探索與戰鬥要共用同一套擺設。**企劃要的是「從探索到戰鬥，背後的場景
    ///      都是同一個地方」。存在節點上，兩個 Stage 讀同一份資料就自然一致。
    ///
    /// 跟 <see cref="EldritchMile.Core.EncounterPlanner"/>（哪一站打誰）
    /// 是同一個模式：**看得到全局的時候決定，之後只是讀出來套用。**
    /// </summary>
    public class SceneDressing : MonoBehaviour
    {
        [System.Serializable]
        public class Piece
        {
            [Tooltip("這件東西的物件。留空則這一條會被忽略")]
            public GameObject target;

            [Tooltip("出現的機率 0~1。\n" +
                     "1 = 一定在（例如結構性的牆面、地板）\n" +
                     "0.5 = 一半機率\n" +
                     "⚠️ Unity 用 + 新增元素會零填充 —— 不改的話這件東西永遠不會出現")]
            [Range(0f, 1f)] public float chance = 0.5f;

            [Tooltip("勾起來就**永遠顯示**，忽略機率。\n" +
                     "美術之後補了「這張是背景的一部分，不能關」的東西時用這個")]
            public bool always = false;
        }

        [Tooltip("可有可無的組件。**背景本身不要列進來** —— 背景關掉就穿幫了")]
        public List<Piece> pieces = new List<Piece>();

        [Tooltip("把每次的擺設結果印到 Console")]
        public bool verbose = false;

        /// <summary>
        /// 依 seed 決定哪些東西出現。**同一個 seed 必定得到同一套擺設。**
        /// </summary>
        public void Apply(int seed)
        {
            var rng = new System.Random(seed);
            int on = 0, total = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                Piece p = pieces[i];
                if (p == null || p.target == null) continue;

                total++;
                bool show = p.always || rng.NextDouble() < p.chance;
                p.target.SetActive(show);
                if (show) on++;
            }

            if (verbose)
                Debug.Log($"[場景擺設] {name} seed {seed}：{on} / {total} 件出現", this);
        }

        /// <summary>
        /// 把目前的子物件自動收集成清單。**編輯器用** ——
        /// 美術更新後在 Inspector 右鍵跑一次，不用手動一件件拖。
        /// 背景（名字含 BG / Background）會自動跳過。
        /// </summary>
        [ContextMenu("自動收集子物件")]
        public void CollectChildren()
        {
            pieces.Clear();

            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.transform == transform) continue;

                // 背景不能關 —— 關掉整間房就穿幫了
                string n = sr.name;
                if (n.Contains("BG") || n.Contains("Background")) continue;

                // 停用中的（例如刻意留著的參考草圖）也不收
                if (!sr.gameObject.activeSelf) continue;

                pieces.Add(new Piece { target = sr.gameObject, chance = 0.6f });
            }

            Debug.Log($"[場景擺設] {name} 收集到 {pieces.Count} 件可切換組件", this);
        }
    }
}

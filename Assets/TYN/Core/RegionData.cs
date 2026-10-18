using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一個地區（漁村、深海…）用到的東西收在同一份。（2026-10-18，設計見 Docs/Design_RegionTravel.md）
    ///
    /// ────────────────────────────────────────────────────────
    /// 【地區之間的位置關係用「物理」想】每一區在世界上有一個格子座標 <see cref="worldCell"/>，
    /// x 往東、y 往北。通往哪一區的通道就**出現在面向那一區的地圖邊緣**：
    ///   B 在西邊 → 通道在這張地圖的西緣；穿過去 → 出現在 B 地圖的**東緣**、差不多同一個南北位置。
    /// 這樣玩家腦中的地圖不會打架 —— 往西走出去，不會從新地區的西邊冒出來。
    /// 方向是從兩區的座標算出來的，**不用另外填**，搬動一區的座標，通道就跟著換邊。
    ///
    /// 【這一份沒填的欄位】一律退回 GameFlowManager 上原本那一格（地圖設定、敵人池…），
    /// 所以先建一份只有 id 的地區也能跑。
    /// </summary>
    [CreateAssetMenu(fileName = "Region_", menuName = "Eldritch/Region")]
    public class RegionData : ScriptableObject
    {
        [Header("識別")]
        [Tooltip("存檔與程式比對用。**建好就不要改** —— 存檔裡記的是這個字串")]
        public string regionId = "";

        [Tooltip("進入時打在黑幕上的地區名（支援 TMP rich text）。留空 = 不打")]
        public string displayName = "";

        [Header("世界上的位置")]
        [Tooltip("這一區在世界上的格子座標（x 往東、y 往北）。\n" +
                 "通道出現在地圖哪一邊，是用兩區的座標差算的：目標在西 → 通道在西緣。")]
        public Vector2Int worldCell = Vector2Int.zero;

        [Header("地圖")]
        [Tooltip("這一區的地圖生成設定（地形圖、層數、節點機率）。留空 = 用 GameFlowManager 那一份")]
        public MapGenerationSettings mapSettings;

        [Tooltip("地圖底圖。留空 = 不換（場景裡擺的那張）")]
        public Texture groundTexture;

        [Tooltip("我方的敵人安排（每個戰鬥節點打誰）。留空 = 用 GameFlowManager 那一份")]
        public EncounterPool encounterPool;

        [Header("商店")]
        [Tooltip("這一區商店的貨表。留空 = 照商店 prefab 自己的設定")]
        public LootTable shopStock;

        [Header("戰鬥（交給 Romtyui 的 BattleRegionHook）")]
        [Tooltip("戰鬥背景。留空 = 不換（戰鬥 prefab 裡那張）")]
        public Sprite battleBackground;

        [Tooltip("戰鬥組的怪物池（沒有指定對手時他自己抽的那一份）。留空 = 不換。\n\n" +
                 "⚠️ 要跟上面的 encounterPool **同一區** —— 只換一邊的話，\n" +
                 "漁村的節點可能排到別區的怪（或反過來）。")]
        public EnemyEncounterPoolData battleEncounterPool;

        [Header("通道（岔路通往的地區）")]
        public List<RegionExit> exits = new List<RegionExit>();

        [Tooltip("**這一區做好了沒**。沒做好的話通道照樣出現在地圖上，\n" +
                 "但走進去只會播一段系統提示「通道還沒完成」，然後回地圖繼續走。\n" +
                 "（2026-10-18：目前只有漁村，其他地區先用這一格擋著）")]
        public bool travelReady = false;

        /// <summary>從 <paramref name="from"/> 往這一區走，是哪個方向（-1/0/1, -1/0/1）。</summary>
        public Vector2Int DirectionFrom(RegionData from)
        {
            Vector2Int d = from != null ? worldCell - from.worldCell : Vector2Int.zero;
            return new Vector2Int(Math.Sign(d.x), Math.Sign(d.y));
        }
    }

    [Serializable]
    public class RegionExit
    {
        [Tooltip("通往哪一區")]
        public RegionData target;

        [Tooltip("這一場 run 會不會出現這條通道（0~1）。用 run 的種子擲，重進同一場結果一樣")]
        [Range(0f, 1f)] public float chance = 1f;
    }
}

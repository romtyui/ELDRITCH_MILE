using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 戰鬥外也能打開的**卡冊**（翻頁的那本書）。商店的 BOOK 按鈕開的就是它。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【它本來只在戰鬥裡】（2026-10-13）卡冊是 Romtyui 的 <see cref="DeckViewerUI"/>，
    /// 掛在 `Stage_Battle_v3` 的 BattleUICanvas(No Rendering)/DeckViewer 底下。
    /// 這裡的 prefab（`DeckBook.prefab`）是那一整組的**複製品**，放在場景層，
    /// 跟齒輪功能表（<see cref="SystemMenuUI"/>）是同一個做法 —— **不改他的檔案**。
    ///
    /// 【缺的那一塊：牌從哪來】他的卡冊讀的是戰鬥中的 <see cref="BattleDeck"/>
    /// （抽牌堆／棄牌堆／消耗區／手牌）。戰鬥外沒有那個東西，
    /// 牌組本體是 <see cref="RunStateManager.savedDeck"/>。
    /// 所以這個物件自己帶一個**沒人用的 BattleDeck**，每次打開前把 savedDeck 灌進去。
    ///   · 用 `RestoreDrawPileOrderOnly` 而不是 `InitializeDeck` —— 後者會洗牌，
    ///     卡冊每開一次順序都不一樣會像壞掉
    ///   · 已查過：BattleDeck 沒有 Awake/Start，全專案也沒有人用 FindObjectOfType 找它，
    ///     所以多一個閒置的 BattleDeck 不會被戰鬥誤認
    ///
    /// 【戰鬥才有意義的分頁要藏起來】棄牌堆、消耗區、手牌在戰鬥外永遠是空的。
    /// 留著的話玩家點了只會看到空白，以為牌不見了。
    ///
    /// 【複製品會跟他的版本脫節】他改了戰鬥裡那本書，這裡不會跟著變。
    /// 跟 Stage_Battle 一樣是重打包時要一起更新的東西（見 Docs 與 memory 的打包清單）。
    /// </summary>
    public class DeckBookUI : MonoBehaviour
    {
        public static DeckBookUI Instance { get; private set; }

        [Header("元件（複製自 Stage_Battle_v3）")]
        [Tooltip("他的卡冊。留空會自己在子物件裡找")]
        public DeckViewerUI viewer;

        [Tooltip("背景模糊。留空會自己在子物件裡找")]
        public DeckViewerBlur blur;

        [Tooltip("灌牌用的 BattleDeck。留空會在自己身上加一個")]
        public BattleDeck bookDeck;

        [Header("戰鬥外")]
        [Tooltip("藏起棄牌堆／消耗區／手牌三個分頁 —— 戰鬥外它們永遠是空的")]
        public bool hideBattleOnlyTabs = true;

        /// <summary>卡冊現在開著嗎。</summary>
        public bool IsOpen => viewer != null && viewer.panelRoot != null && viewer.panelRoot.activeInHierarchy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[卡冊] 場景裡有兩個 DeckBookUI，關掉後來的「{name}」", this);
                gameObject.SetActive(false);
                return;
            }
            Instance = this;

            if (viewer == null) viewer = GetComponentInChildren<DeckViewerUI>(true);
            if (blur == null) blur = GetComponentInChildren<DeckViewerBlur>(true);
            if (bookDeck == null) bookDeck = GetComponent<BattleDeck>();
            if (bookDeck == null) bookDeck = gameObject.AddComponent<BattleDeck>();

            if (viewer == null) Debug.LogWarning($"[卡冊]「{name}」底下沒有 DeckViewerUI，BOOK 按了不會有事", this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 戰鬥中打開：直接看**戰鬥的牌堆**（抽牌堆／棄牌堆／消耗區／手牌四個分頁都在）。
        /// （2026-10-14）戰鬥裡那顆書原本開的是 Romtyui 自己的卡冊 —— 那本由他的相機畫，
        /// 我們的遺物／食物 HUD 是 Overlay 畫布，一定疊在它上面、不會被模糊蓋住。
        /// 改開這本（Overlay 7000）就跟商店一樣：HUD 被截進模糊背景、再被卡冊蓋住。
        ///
        /// ⚠️ **不要**對 liveDeck 呼叫 RestoreDrawPileOrderOnly —— 那會把戰鬥中的牌堆清空。
        /// </summary>
        public void Open(BattleDeck liveDeck)
        {
            if (liveDeck == null) { Open(); return; }
            if (viewer == null) return;

            viewer.battleDeck = liveDeck;
            SetBattleOnlyTabsVisible(true);

            viewer.OpenDrawPile();
            if (blur != null) blur.Open();
        }

        /// <summary>打開卡冊，顯示目前這一趟的牌組（戰鬥外）。</summary>
        public void Open()
        {
            if (viewer == null) return;

            List<CardData> deck = RunStateManager.Instance != null ? RunStateManager.Instance.savedDeck : null;
            if (deck == null)
            {
                Debug.LogWarning("[卡冊] 場上沒有 RunStateManager，沒有牌組可以看", this);
                deck = new List<CardData>();
            }

            bookDeck.RestoreDrawPileOrderOnly(deck);
            viewer.battleDeck = bookDeck;

            // 上次可能是戰鬥中打開的，分頁要照這次的設定重新決定
            SetBattleOnlyTabsVisible(!hideBattleOnlyTabs);

            // 順序照他戰鬥裡那顆按鈕：先開分頁，再交給模糊層（它會先藏面板、截圖、再打開）
            viewer.OpenDrawPile();
            if (blur != null) blur.Open();
        }

        public void Close()
        {
            if (blur != null) blur.Close();
            else if (viewer != null) viewer.Close();
        }

        private void SetBattleOnlyTabsVisible(bool visible)
        {
            SetActive(viewer.discardPileButton, visible);
            SetActive(viewer.exhaustPileButton, visible);
            SetActive(viewer.handButton, visible);
        }

        private static void SetActive(Button b, bool on)
        {
            if (b != null) b.gameObject.SetActive(on);
        }
    }
}

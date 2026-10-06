using System;
using System.Collections.Generic;
using EldritchMile.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BattleRewardHeaderReferences
{
    [Tooltip("區域文字，本次只預留，不修改文字。")]
    public TMP_Text regionText;

    [Tooltip("目前 HP 數字。")]
    public TMP_Text hpNumberText;

    [Tooltip("目前 SAN 數字。")]
    public TMP_Text sanNumberText;
}

public class BattleRewardUI : MonoBehaviour
{
    private enum RewardKind
    {
        Money,
        Weapon,
        Relic
    }

    private class RewardRow
    {
        public RewardKind kind;
        public int money;
        public ItemData relic;
        public List<CardData> choices;
        public BattleRewardEntryUI view;
        public bool completed;
    }

    [Header("獨立戰鬥測試")]
    [Tooltip("直接播放獨立戰鬥場景時開啟。沒有正式 RunContext 才建立測試資料；從探索進入時仍優先使用正式資料。")]
    public bool enableStandaloneTestMode = false;

    private RunContext standaloneTestRun;

    [Header("畫面")]
    [Tooltip("獎勵畫面UI 根物件。")]
    public GameObject rewardRoot;

    [Tooltip("獎勵選項。")]
    public GameObject overviewRoot;

    [Tooltip("window。")]
    public GameObject weaponRoot;

    [Tooltip("共用確認UI。")]
    public GameObject confirmationRoot;

    [Header("兩個畫面的 HP / SAN")]
    [Tooltip("分別指定列表與武器畫面的文字。")]
    public BattleRewardHeaderReferences[] headers = new BattleRewardHeaderReferences[2];

    [Header("列表")]
    [Tooltip("Scroll View / Viewport / Content。")]
    public Transform entryParent;

    [Tooltip("reward_template Prefab。")]
    public BattleRewardEntryUI entryTemplate;

    [Tooltip("固定金幣圖示。")]
    public Sprite moneyIcon;

    [Tooltip("固定武器圖示。")]
    public Sprite weaponIcon;

    [Header("武器候選")]
    [Tooltip("武器獎勵使用的卡牌資料庫，會依 Card ID 篩選符合條件的卡牌。")]
    public AllCardDatabase cardPoolRange;

    [Tooltip("Card ID 包含任一字串即可加入候選池，不區分大小寫。清單為空或全部空白時不限制 ID。")]
    public List<string> requiredCardIdTexts = new List<string> { "human", "ocean" };

    [Tooltip("生成候選牌的 CardContent。")]
    public Transform cardParent;

    [Tooltip("包含卡牌與專屬選擇框的 Prefab。")]
    public BattleRewardCardUI cardTemplate;

    [Min(0f)]
    [Tooltip("選中後至少等待多久，才能再點同一張並領取。")]
    public float cardConfirmMinInterval = 0.1f;

    [Header("遺物資料")]
    [Tooltip("留空時使用 GameFlowManager 的 Item Database。")]
    public ItemDatabase itemDatabase;

    [Header("退出")]
    [Tooltip("共用 Close 的 Button。")]
    public Button closeButton;

    [Header("共用確認")]
    [Tooltip("Yes 按鈕。")]
    public Button yesButton;

    [Tooltip("No 按鈕。")]
    public Button noButton;

    [Tooltip("確認欄內的提示文字。")]
    public TMP_Text confirmationText;

    [TextArea]
    [Tooltip("放棄目前武器獎勵的提示。")]
    public string abandonWeaponMessage = "還未選擇獎勵，確定放棄這筆武器獎勵？";

    [TextArea]
    [Tooltip("離開整個獎勵畫面的提示。")]
    public string exitRewardsMessage = "確定放棄剩餘獎勵並離開戰鬥？";

    [Header("不再確認設定")]
    [Tooltip("確認框整組，包含 Toggle 與說明文字。")]
    public GameObject preferenceRoot;

    [Tooltip("本趟旅程不再確認的勾選框。")]
    public Toggle doNotAskAgainToggle;

    [Tooltip("勾選框旁的文字。")]
    public TMP_Text doNotAskAgainText;

    [Tooltip("預設隱藏勾選設定，不影響 Yes/No 確認。")]
    public bool showConfirmationPreferences = false;

    [Tooltip("放棄武器時的勾選說明。")]
    public string weaponPreferenceMessage = "本趟旅程不再確認放棄武器獎勵";

    [Tooltip("離開列表時的勾選說明。")]
    public string exitPreferenceMessage = "本趟旅程不再確認放棄剩餘獎勵";

    private readonly List<RewardRow> rows = new List<RewardRow>();
    private readonly List<BattleRewardCardUI> cards = new List<BattleRewardCardUI>();

    private BattleManager manager;
    private RunContext run;
    private ItemDatabase database;
    private Action completedCallback;
    private BattleRewardCardUI selectedCard;
    private int currentRow = -1;
    private float selectedAt;
    private bool initialized;
    private bool isOpen;
    private bool confirming;
    private bool confirmingWeaponAbandon;
    private bool granting;

    public bool Show(BattleManager battleManager, EnemyFormationData formation, Action onCompleted)
    {
        if (isOpen || battleManager == null || battleManager.playerDeck == null || formation == null || RunStateManager.Instance == null) return false;

        BattleStageController stage = battleManager.GetComponentInParent<BattleStageController>(true);
        RunContext context = stage != null ? stage.RewardRunContext : null;

        if (context == null && enableStandaloneTestMode)
        {
            if (standaloneTestRun == null) standaloneTestRun = RunContext.CreateNew(null);

            context = standaloneTestRun;
            Debug.Log("[BattleRewardUI] 使用獨立戰鬥測試資料。");
        }

        if (context == null)
        {
            Debug.LogError("[BattleRewardUI] 找不到 RunContext。獨立場景測試請開啟 Enable Standalone Test Mode；正式流程請確認 BattleStageController 已取得旅程資料。");
            return false;
        }

        if (rewardRoot == null || overviewRoot == null || weaponRoot == null || confirmationRoot == null || entryParent == null || entryTemplate == null || entryTemplate.actionButton == null || cardParent == null || cardTemplate == null || cardTemplate.cardView == null || closeButton == null || yesButton == null || noButton == null || confirmationText == null)
        {
            Debug.LogError("[BattleRewardUI] 必要 UI 引用尚未指定。");
            return false;
        }

        InitializeUI();
        ClearViews();

        manager = battleManager;
        run = context;
        database = itemDatabase != null ? itemDatabase : (GameFlowManager.Instance != null ? GameFlowManager.Instance.itemDatabase : null);
        completedCallback = onCompleted;
        currentRow = -1;
        isOpen = true;
        confirming = false;
        granting = false;

        confirmationRoot.SetActive(false);
        weaponRoot.SetActive(false);
        overviewRoot.SetActive(true);
        rewardRoot.SetActive(true);

        SetPreferenceVisibility();
        RefreshHeaders();
        GenerateRewards(formation);
        CreateEntries();

        return true;
    }


    private void InitializeUI()
    {
        if (initialized) return;

        closeButton.onClick.AddListener(RequestClose);
        yesButton.onClick.AddListener(ConfirmYes);
        noButton.onClick.AddListener(ConfirmNo);

        initialized = true;
    }

    private void SetPreferenceVisibility()
    {
        if (preferenceRoot != null) preferenceRoot.SetActive(showConfirmationPreferences);
        if (doNotAskAgainToggle != null) doNotAskAgainToggle.gameObject.SetActive(showConfirmationPreferences);
        if (doNotAskAgainText != null) doNotAskAgainText.gameObject.SetActive(showConfirmationPreferences);
    }

    private void RefreshHeaders()
    {
        if (headers == null || manager == null) return;

        for (int i = 0; i < headers.Length; i++)
        {
            BattleRewardHeaderReferences header = headers[i];
            if (header == null) continue;

            if (header.hpNumberText != null && manager.playerUnit != null) header.hpNumberText.text = manager.playerUnit.currentHp.ToString();
            if (header.sanNumberText != null && manager.energySystem != null) header.sanNumberText.text = manager.energySystem.currentEnergy.ToString();
        }
    }

    private int RandomRows(int maximum)
    {
        if (maximum <= 0) return 0;
        return UnityEngine.Random.Range(1, maximum + 1);
    }

    private void GenerateRewards(EnemyFormationData formation)
    {
        int moneyRows = RandomRows(formation.moneyRewardMaxRows);
        int minimum = Mathf.Max(0, Mathf.Min(formation.moneyRewardRange.x, formation.moneyRewardRange.y));
        int maximum = Mathf.Max(minimum, Mathf.Max(formation.moneyRewardRange.x, formation.moneyRewardRange.y));

        for (int i = 0; i < moneyRows; i++)
        {
            int amount = UnityEngine.Random.Range(minimum, maximum + 1);
            rows.Add(new RewardRow { kind = RewardKind.Money, money = amount });
        }

        int weaponRows = RandomRows(formation.weaponRewardMaxRows);

        for (int i = 0; i < weaponRows; i++)
        {
            List<CardData> choices = CreateCardChoices(formation.weaponChoiceCount);
            if (choices.Count == 0) continue;
            rows.Add(new RewardRow { kind = RewardKind.Weapon, choices = choices });
        }

        int relicRows = RandomRows(formation.relicRewardMaxRows);
        HashSet<string> reservedIds = new HashSet<string>();

        for (int i = 0; i < relicRows; i++)
        {
            ItemData relic = PickRelic(formation.relicRewardMaxLevel, reservedIds);
            if (relic == null) continue;

            reservedIds.Add(relic.id);
            rows.Add(new RewardRow { kind = RewardKind.Relic, relic = relic });
        }

        if (weaponRows > 0 && !rows.Exists(row => row.kind == RewardKind.Weapon)) Debug.LogWarning("[BattleRewardUI] 沒有符合條件的武器卡，請檢查卡池與 ID。");
        if (relicRows > 0 && database == null) Debug.LogWarning("[BattleRewardUI] 沒有 ItemDatabase，未生成遺物獎勵。");
        if (relicRows > 0 && database != null && !rows.Exists(row => row.kind == RewardKind.Relic)) Debug.LogWarning($"[BattleRewardUI] tire = {formation.relicRewardMaxLevel} 沒有可領取的遺物。");
    }

    private List<CardData> CreateCardChoices(int count)
    {
        List<CardData> result = new List<CardData>();

        if (cardPoolRange == null)
        {
            Debug.LogWarning("[BattleRewardUI] Card Pool Range 尚未指定 AllCardDatabase。", this);
            return result;
        }

        IReadOnlyList<CardData> sourceCards = cardPoolRange.Cards;

        if (sourceCards == null)
        {
            Debug.LogWarning("[BattleRewardUI] AllCardDatabase 的卡牌清單是 null。", this);
            return result;
        }

        List<CardData> pool = new List<CardData>();
        HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < sourceCards.Count; i++)
        {
            CardData data = sourceCards[i];
            if (data == null || string.IsNullOrWhiteSpace(data.cardId) || data.isToken || data.exhaust) continue;
            if (!MatchesRewardCardId(data.cardId)) continue;
            if (!ids.Add(data.cardId)) continue;

            pool.Add(data);
        }

        int actualCount = Mathf.Min(Mathf.Max(1, count), pool.Count);

        for (int i = 0; i < actualCount; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private ItemData PickRelic(int requiredTier, HashSet<string> reservedIds)
    {
        if (database == null || database.items == null) return null;

        List<ItemData> candidates = new List<ItemData>();
        HashSet<string> ids = new HashSet<string>();

        for (int i = 0; i < database.items.Count; i++)
        {
            ItemData item = database.items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
            if (item.relicEffect == null && !item.HasTag("Curio")) continue;
            if (item.tire != requiredTier) continue;
            if (run.HasItem(item.id) || reservedIds.Contains(item.id) || !ids.Add(item.id)) continue;
            if (database.GetById(item.id) != item) continue;

            candidates.Add(item);
        }

        if (candidates.Count == 0) return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private void CreateEntries()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            RewardRow row = rows[i];
            string name = row.kind == RewardKind.Money ? "金幣" : (row.kind == RewardKind.Weapon ? "武器" : row.relic.Label);
            string amount = row.kind == RewardKind.Money ? $"+{row.money}" : "+1";
            Sprite icon = row.kind == RewardKind.Money ? moneyIcon : (row.kind == RewardKind.Weapon ? weaponIcon : row.relic.icon);

            row.view = Instantiate(entryTemplate, entryParent);
            row.view.gameObject.SetActive(false);
            row.view.Bind(this, i, name, amount, icon, row.kind == RewardKind.Weapon);
            row.view.gameObject.SetActive(true);
        }
    }

    public void ClickReward(int index)
    {
        if (!isOpen || confirming || granting || currentRow >= 0 || index < 0 || index >= rows.Count) return;

        RewardRow row = rows[index];
        if (row.completed) return;

        if (row.kind == RewardKind.Weapon)
        {
            OpenWeapon(index);
            return;
        }

        granting = true;

        if (row.kind == RewardKind.Money)
        {
            run.AddMoney(row.money);
            CompleteRow(index);
        }
        else if (row.relic != null)
        {
            if (!run.HasItem(row.relic.id)) run.AddItem(row.relic.id, 1);
            else Debug.LogWarning("[BattleRewardUI] 此遺物已持有，不重複加入。");

            CompleteRow(index);
        }

        granting = false;
    }

    private void OpenWeapon(int index)
    {
        ClearCards();
        currentRow = index;
        overviewRoot.SetActive(false);
        weaponRoot.SetActive(true);
        RefreshHeaders();

        List<CardData> choices = rows[index].choices;

        for (int i = 0; i < choices.Count; i++)
        {
            BattleRewardCardUI card = Instantiate(cardTemplate, cardParent);
            card.gameObject.SetActive(false);
            card.Bind(this, choices[i]);
            card.gameObject.SetActive(true);
            cards.Add(card);
        }
    }

    public void ClickCard(BattleRewardCardUI card)
    {
        if (!isOpen || confirming || granting || currentRow < 0 || card == null || card.Data == null || !cards.Contains(card)) return;

        if (selectedCard != card)
        {
            selectedCard = card;
            selectedAt = Time.unscaledTime;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null) cards[i].SetSelected(cards[i] == selectedCard);
            }

            return;
        }

        if (Time.unscaledTime - selectedAt < Mathf.Max(0f, cardConfirmMinInterval)) return;

        granting = true;

        if (!manager.playerDeck.AddRewardCardToDeck(card.Data))
        {
            granting = false;
            return;
        }

        CompleteRow(currentRow);
        RunStateManager.Instance.SaveFromBattle(manager.playerUnit, manager.energySystem, manager.playerDeck);
        ReturnToOverview();
        granting = false;
    }

    public void RequestClose()
    {
        if (!isOpen || confirming || granting) return;

        bool weapon = currentRow >= 0;
        bool skip = weapon ? run.skipWeaponRewardAbandonConfirmation : run.skipBattleRewardExitConfirmation;

        if (skip)
        {
            if (weapon) AbandonCurrentWeapon();
            else FinishRewards();

            return;
        }

        confirming = true;
        confirmingWeaponAbandon = weapon;
        confirmationText.text = weapon ? abandonWeaponMessage : exitRewardsMessage;

        SetPreferenceVisibility();
        if (doNotAskAgainToggle != null) doNotAskAgainToggle.SetIsOnWithoutNotify(false);
        if (doNotAskAgainText != null) doNotAskAgainText.text = weapon ? weaponPreferenceMessage : exitPreferenceMessage;

        confirmationRoot.SetActive(true);
        confirmationRoot.transform.SetAsLastSibling();
    }

    public void ConfirmYes()
    {
        if (!isOpen || !confirming || granting) return;

        bool remember = showConfirmationPreferences && doNotAskAgainToggle != null && doNotAskAgainToggle.isOn;

        if (confirmingWeaponAbandon)
        {
            if (remember) run.skipWeaponRewardAbandonConfirmation = true;
            AbandonCurrentWeapon();
        }
        else
        {
            if (remember) run.skipBattleRewardExitConfirmation = true;
            FinishRewards();
        }
    }

    public void ConfirmNo()
    {
        if (!isOpen || !confirming) return;

        confirming = false;
        confirmationRoot.SetActive(false);
    }

    private void AbandonCurrentWeapon()
    {
        if (currentRow < 0) return;

        CompleteRow(currentRow);
        ReturnToOverview();
    }

    private void ReturnToOverview()
    {
        confirming = false;
        confirmationRoot.SetActive(false);
        ClearCards();
        currentRow = -1;
        weaponRoot.SetActive(false);
        overviewRoot.SetActive(true);
        RefreshHeaders();
    }

    private void CompleteRow(int index)
    {
        if (index < 0 || index >= rows.Count || rows[index].completed) return;

        rows[index].completed = true;
        if (rows[index].view != null) rows[index].view.SetCompleted();
    }

    private void FinishRewards()
    {
        if (!isOpen) return;

        isOpen = false;
        confirming = false;

        for (int i = 0; i < rows.Count; i++)
        {
            if (!rows[i].completed) CompleteRow(i);
        }

        confirmationRoot.SetActive(false);
        weaponRoot.SetActive(false);
        overviewRoot.SetActive(false);
        ClearViews();
        rewardRoot.SetActive(false);

        Action callback = completedCallback;
        completedCallback = null;
        callback?.Invoke();
    }

    private void ClearCards()
    {
        selectedCard = null;

        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            cards[i].gameObject.SetActive(false);
            Destroy(cards[i].gameObject);
        }

        cards.Clear();
    }

    private void ClearViews()
    {
        ClearCards();

        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].view == null) continue;
            rows[i].view.gameObject.SetActive(false);
            Destroy(rows[i].view.gameObject);
        }

        rows.Clear();
        currentRow = -1;
    }

    public void CancelWithoutCompletion()
    {
        completedCallback = null;
        isOpen = false;
        confirming = false;
        granting = false;
        ClearViews();

        if (confirmationRoot != null) confirmationRoot.SetActive(false);
        if (weaponRoot != null) weaponRoot.SetActive(false);
        if (overviewRoot != null) overviewRoot.SetActive(false);
        if (rewardRoot != null) rewardRoot.SetActive(false);
    }

    private bool MatchesRewardCardId(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId)) return false;
        if (requiredCardIdTexts == null || requiredCardIdTexts.Count == 0) return true;

        bool hasFilter = false;

        for (int i = 0; i < requiredCardIdTexts.Count; i++)
        {
            string filter = requiredCardIdTexts[i];
            if (string.IsNullOrWhiteSpace(filter)) continue;

            hasFilter = true;
            if (cardId.IndexOf(filter.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }

        return !hasFilter;
    }
}
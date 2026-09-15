using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TokenUsageRecord
{
    public string tokenId;
    public int count;
}
public class BattleDeck : MonoBehaviour
{
    [Header("Starting Deck")]
    public List<CardData> startingDeck = new();

    private List<CardInstance> drawPile = new();
    private List<CardInstance> hand = new();
    private List<CardInstance> discardPile = new();
    private List<CardInstance> exhaustPile = new();
    private List<CardInstance> playedCardsThisTurn = new();

    public IReadOnlyList<CardInstance> PlayedCardsThisTurn => playedCardsThisTurn;

    [Header("Token Usage Records")]
    [SerializeField] private List<TokenUsageRecord> tokenUsageRecords = new();

    public IReadOnlyList<TokenUsageRecord> TokenUsageRecords => tokenUsageRecords;

    //public IReadOnlyList<CardInstance> Hand => hand;

    public IReadOnlyList<CardInstance> DrawPile => drawPile;
    public IReadOnlyList<CardInstance> Hand => hand;
    public IReadOnlyList<CardInstance> DiscardPile => discardPile;
    public IReadOnlyList<CardInstance> ExhaustPile => exhaustPile;

    [Header("Debug View")]
    [SerializeField] private List<string> debugDrawPile = new();
    [SerializeField] private List<string> debugHand = new();
    [SerializeField] private List<string> debugDiscardPile = new();
    [SerializeField] private List<string> debugExhaustPile = new();
    [SerializeField] private List<string> debugPlayedCardsThisTurn = new();

    private Dictionary<string, int> playedTokenCounts = new Dictionary<string, int>();

    public enum DeckViewMode
    {
        DrawPile,
        DiscardPile,
        ExhaustPile,
        Hand
    }
    public void InitializeDeck()
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        playedCardsThisTurn.Clear();
        ResetTokenUsage();
        foreach (CardData card in startingDeck)
        {
            drawPile.Add(new CardInstance(card));
        }

        Shuffle(drawPile);
        RefreshDebugView();
    }

    public void ResetForNewGame()
    {
        InitializeDeck();

        Debug.Log("[BattleDeck] 重新開始新遊戲：牌組已從 startingDeck 重建");
    }

    public void PrepareForNextBattleKeepDeck()
    {
        if (drawPile == null)
            drawPile = new List<CardInstance>();

        if (hand == null)
            hand = new List<CardInstance>();

        if (discardPile == null)
            discardPile = new List<CardInstance>();

        if (exhaustPile == null)
            exhaustPile = new List<CardInstance>();

        List<CardInstance> nextBattleDeck = new List<CardInstance>();

        // DrawPile
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardInstance card = drawPile[i];

            if (card == null || card.data == null)
                continue;

            // Exhaust 卡無論目前在哪裡，下一場都不保留。
            if (card.data.exhaust)
            {
                Debug.Log(
                    $"[BattleDeck] 戰鬥結束移除 Exhaust 卡牌：{card.data.cardName}（DrawPile）"
                );

                continue;
            }

            nextBattleDeck.Add(card);
        }

        // Hand
        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];

            if (card == null || card.data == null)
                continue;

            if (card.data.exhaust)
            {
                Debug.Log(
                    $"[BattleDeck] 戰鬥結束移除 Exhaust 卡牌：{card.data.cardName}（Hand）"
                );

                continue;
            }

            nextBattleDeck.Add(card);
        }

        // DiscardPile
        for (int i = 0; i < discardPile.Count; i++)
        {
            CardInstance card = discardPile[i];

            if (card == null || card.data == null)
                continue;

            if (card.data.exhaust)
            {
                Debug.Log(
                    $"[BattleDeck] 戰鬥結束移除 Exhaust 卡牌：{card.data.cardName}（DiscardPile）"
                );

                continue;
            }

            nextBattleDeck.Add(card);
        }

        // ExhaustPile
        for (int i = 0; i < exhaustPile.Count; i++)
        {
            CardInstance card = exhaustPile[i];

            if (card == null || card.data == null)
                continue;

            // 真正的 Exhaust 卡：永久移除。
            if (card.data.exhaust)
            {
                Debug.Log(
                    $"[BattleDeck] 戰鬥結束永久移除 Exhaust 卡牌：{card.data.cardName}"
                );

                continue;
            }

            // 非 Exhaust，但因 Ethereal 等原因進入 ExhaustPile 的牌：
            // 下一場重新回到牌組。
            nextBattleDeck.Add(card);

            Debug.Log(
                $"[BattleDeck] Ethereal / 非 Exhaust 卡牌返回下一場牌組：{card.data.cardName}"
            );
        }

        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        playedCardsThisTurn.Clear();

        drawPile.AddRange(nextBattleDeck);

        Shuffle(drawPile);

        RefreshDebugView();

        Debug.Log(
            $"[BattleDeck] 下一場戰鬥準備完成，剩餘牌組數量 = {drawPile.Count}"
        );
    }

    private bool ShouldRemoveAfterBattle(CardInstance card)
    {
        if (card == null || card.data == null)
            return true;

        return card.data.exhaust;
    }

    private void LogRemovedBattleOnlyCard(CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        Debug.Log(
            $"[BattleDeck] 戰鬥結束移除 Exhaust 卡牌：{card.data.cardName}"
        );
    }

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++)
            DrawOneCard();

        RefreshDebugView();
    }

    public CardInstance DrawOneCard()
    {
        if (drawPile == null)
            drawPile = new List<CardInstance>();

        if (discardPile == null)
            discardPile = new List<CardInstance>();

        // 牌組空了，就把棄牌堆洗回牌組
        if (drawPile.Count == 0)
        {
            ReshuffleDiscardIntoDraw();
        }

        // 洗回後還是沒牌，代表抽牌堆和棄牌堆都空
        if (drawPile.Count == 0)
        {
            Debug.Log("[BattleDeck] 抽牌堆和棄牌堆都沒有牌，無法抽牌");
            RefreshDebugView();
            return null;
        }

        CardInstance top = drawPile[0];
        drawPile.RemoveAt(0);
        hand.Add(top);

        RefreshDebugView();

        return top;
    }

    public void OnCardPlayed(CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        RegisterTokenPlayed(card);

        if (hand.Remove(card))
        {
            if (card.data != null && card.data.exhaust)
                exhaustPile.Add(card);
            else
                discardPile.Add(card);
        }

        RefreshDebugView();
    }

    public void DiscardHandAtEndTurn()
    {
        for (int i = hand.Count - 1; i >= 0; i--)
        {
            CardInstance card = hand[i];

            if (card == null || card.data == null)
            {
                hand.RemoveAt(i);
                continue;
            }

            // Ethereal 優先於 Retain：
            // 回合結束仍在手上時直接消耗。
            if (card.data.ethereal)
            {
                hand.RemoveAt(i);
                exhaustPile.Add(card);

                Debug.Log(
                    $"[BattleDeck] Ethereal 卡牌回合結束被消耗：{card.data.cardName}"
                );

                continue;
            }

            if (card.data.retain)
                continue;

            hand.RemoveAt(i);
            discardPile.Add(card);
        }

        RefreshDebugView();
    }

    public CardTransformResult TransformRandomCardInDrawPileByPool(CardTransformPoolData transformPool)
    {
        if (transformPool == null)
        {
            Debug.LogWarning("[BattleDeck] transformPool 是 null，無法變換牌");
            return new CardTransformResult(false);
        }

        if (drawPile == null)
            drawPile = new List<CardInstance>();

        List<int> validIndexes = GetValidTransformIndexes(transformPool);

        if (validIndexes.Count == 0)
        {
            Debug.Log("[BattleDeck] 抽牌堆沒有可變化的牌，嘗試將棄牌堆洗回抽牌堆");

            ReshuffleDiscardIntoDraw();

            validIndexes = GetValidTransformIndexes(transformPool);
        }

        if (validIndexes.Count == 0)
        {
            Debug.Log($"[BattleDeck] 抽牌堆與棄牌堆都沒有可以被變化卡池 {transformPool.transformId} 變換的牌");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        int selectedListIndex = Random.Range(0, validIndexes.Count);
        int selectedDrawPileIndex = validIndexes[selectedListIndex];

        CardInstance oldCardInstance = drawPile[selectedDrawPileIndex];

        if (oldCardInstance == null || oldCardInstance.data == null)
        {
            Debug.LogWarning("[BattleDeck] 選到的牌資料是 null");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        CardData oldCardData = oldCardInstance.data;

        bool hasResult = transformPool.TryGetTransformResult(oldCardData, out CardData newCardData);

        if (!hasResult || newCardData == null)
        {
            Debug.LogWarning("[BattleDeck] 找到候選牌，但沒有取得變換結果");
            RefreshDebugView();
            return new CardTransformResult(false);
        }

        drawPile[selectedDrawPileIndex] = new CardInstance(newCardData);

        Debug.Log($"[BattleDeck] 變化卡池 {transformPool.transformId}：{oldCardData.cardName} → {newCardData.cardName}");

        RefreshDebugView();

        return new CardTransformResult(oldCardData, newCardData, transformPool.transformId);
    }

    private List<int> GetValidTransformIndexes(CardTransformPoolData transformPool)
    {
        List<int> validIndexes = new List<int>();

        if (drawPile == null || drawPile.Count == 0)
            return validIndexes;

        for (int i = 0; i < drawPile.Count; i++)
        {
            CardInstance cardInstance = drawPile[i];

            if (cardInstance == null || cardInstance.data == null)
                continue;

            if (transformPool.TryGetTransformResult(cardInstance.data, out CardData resultCard))
            {
                if (resultCard != null)
                    validIndexes.Add(i);
            }
        }

        return validIndexes;
    }

    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        Shuffle(drawPile);

        RefreshDebugView();
    }

    private void Shuffle(List<CardInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    [ContextMenu("Refresh Debug View")]
    public void RefreshDebugView()
    {
        FillDebugList(debugDrawPile, drawPile);
        FillDebugList(debugHand, hand);
        FillDebugList(debugDiscardPile, discardPile);
        FillDebugList(debugExhaustPile, exhaustPile);
        FillDebugList(debugPlayedCardsThisTurn, playedCardsThisTurn);
    }

    private void FillDebugList(List<string> debugList, List<CardInstance> source)
    {
        debugList.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            CardInstance card = source[i];

            if (card == null || card.data == null)
            {
                debugList.Add($"{i}: null");
                continue;
            }

            debugList.Add($"{i}: {card.data.cardName} / Cost {card.currentCost}");
        }
    }

    public void AddCardToHand(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogWarning("[BattleDeck] AddCardToHand 失敗，cardData 是 null");
            return;
        }

        hand.Add(new CardInstance(cardData));

        Debug.Log($"[BattleDeck] 生成卡牌到手牌：{cardData.cardName}");
    }

    private void RegisterTokenPlayed(CardInstance card)
    {
        if (card == null || card.data == null)
            return;

        if (!card.data.isToken)
            return;

        string id = NormalizeTokenId(card.data.tokenId);

        if (string.IsNullOrWhiteSpace(id))
            return;

        for (int i = 0; i < tokenUsageRecords.Count; i++)
        {
            TokenUsageRecord record = tokenUsageRecords[i];

            if (record == null)
                continue;

            if (NormalizeTokenId(record.tokenId) != id)
                continue;

            record.count++;

            Debug.Log(
                $"[BattleDeck] 使用 Token：{id}，目前使用次數：{record.count}"
            );

            return;
        }

        Debug.LogWarning(
            $"[BattleDeck] 打出了 Token：{id}，但 Token Usage Records 裡沒有設定這個 ID，所以沒有累加"
        );
    }

    public int GetUsedTokenCount(string tokenId)
    {
        string id = NormalizeTokenId(tokenId);

        for (int i = 0; i < tokenUsageRecords.Count; i++)
        {
            TokenUsageRecord record = tokenUsageRecords[i];

            if (record == null)
                continue;

            if (NormalizeTokenId(record.tokenId) == id)
                return Mathf.Max(0, record.count);
        }

        return 0;
    }

    public int CountTokenInHand(string tokenId)
    {
        string id = NormalizeTokenId(tokenId);

        int count = 0;

        for (int i = 0; i < hand.Count; i++)
        {
            CardInstance card = hand[i];

            if (card == null || card.data == null)
                continue;

            if (!card.data.isToken)
                continue;

            if (NormalizeTokenId(card.data.tokenId) == id)
                count++;
        }

        return count;
    }

    public void ResetTokenUsage()
    {
        for (int i = 0; i < tokenUsageRecords.Count; i++)
        {
            TokenUsageRecord record = tokenUsageRecords[i];

            if (record == null)
                continue;

            record.count = 0;
        }

        Debug.Log("[BattleDeck] 所有 Token 使用次數已重置為 0");
    }

    private string NormalizeTokenId(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            return "DefaultToken";

        return tokenId.Trim();
    }

    public void RestoreDeckSnapshot(
    List<CardData> savedDrawPile,
    List<CardData> savedHand,
    List<CardData> savedDiscardPile,
    List<CardData> savedExhaustPile
)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        RestorePile(savedDrawPile, drawPile);
        RestorePile(savedHand, hand);
        RestorePile(savedDiscardPile, discardPile);
        RestorePile(savedExhaustPile, exhaustPile);

        RefreshDebugView();

        Debug.Log(
            $"[BattleDeck] 已還原牌堆快照：" +
            $"Draw {drawPile.Count}, " +
            $"Hand {hand.Count}, " +
            $"Discard {discardPile.Count}, " +
            $"Exhaust {exhaustPile.Count}"
        );
    }
    private void RestorePile(List<CardData> savedCards, List<CardInstance> targetPile)
    {
        if (savedCards == null || targetPile == null)
            return;

        for (int i = 0; i < savedCards.Count; i++)
        {
            CardData cardData = savedCards[i];

            if (cardData == null)
                continue;

            targetPile.Add(new CardInstance(cardData));
        }
    }
    public void RestoreDrawPileOrderOnly(List<CardData> savedDrawPileOrder)
    {
        drawPile.Clear();
        hand.Clear();
        discardPile.Clear();
        exhaustPile.Clear();

        if (savedDrawPileOrder != null)
        {
            for (int i = 0; i < savedDrawPileOrder.Count; i++)
            {
                CardData cardData = savedDrawPileOrder[i];

                if (cardData == null)
                    continue;

                drawPile.Add(new CardInstance(cardData));
            }
        }

        RefreshDebugView();

        Debug.Log($"[BattleDeck] 已還原戰鬥開始抽牌堆順序，張數 = {drawPile.Count}");
    }
    public int GetTokenPlayedCount(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
            tokenId = "DefaultToken";

        if (playedTokenCounts == null)
            return 0;

        if (playedTokenCounts.TryGetValue(tokenId, out int count))
            return count;

        return 0;
    }
}
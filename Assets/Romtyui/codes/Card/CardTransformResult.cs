public class CardTransformResult
{
    public bool success;
    public CardData originalCardData;
    public CardData resultCardData;
    public string transformPoolId;
    // 保存這次選中的實際卡片，避免事件觸發時重新抽選。
    public CardInstance originalCardInstance;

    // 防止同一次變換重複執行。
    public bool isApplied;

    public CardTransformResult(bool success)
    {
        this.success = success;
    }

    public CardTransformResult(CardData originalCardData, CardData resultCardData, string transformPoolId)
    {
        success = true;
        this.originalCardData = originalCardData;
        this.resultCardData = resultCardData;
        this.transformPoolId = transformPoolId;
    }
}
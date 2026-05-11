public class CardTransformResult
{
    public bool success;
    public CardData originalCardData;
    public CardData resultCardData;
    public string transformPoolId;

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
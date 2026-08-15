public interface CardDescriptionValueProvider
{
    bool TryGetDescriptionValue(
        string key,
        CardResolveContext context,
        out int value
    );
}
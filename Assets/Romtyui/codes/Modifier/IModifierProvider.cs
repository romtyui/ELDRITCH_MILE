using System.Collections.Generic;

public interface IModifierProvider
{
    void CollectModifiers(ModifierQuery query, List<ModifierValue> results);
}
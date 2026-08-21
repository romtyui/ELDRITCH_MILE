using System.Collections.Generic;

public interface IRelicsEffectSource
{
    IReadOnlyList<RelicsEffectData>
        RelicsEffects
    {
        get;
    }
}
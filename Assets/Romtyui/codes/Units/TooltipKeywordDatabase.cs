using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TooltipKeywordEntry
{
    public string keyword;
    public string title;

    [TextArea(2, 5)]
    public string description;
}

//[CreateAssetMenu(menuName = "CardGame/UI/Tooltip Keyword Database")]
//public class TooltipKeywordDatabase : ScriptableObject
//{
//    public List<TooltipKeywordEntry> entries = new();

//    public bool TryGet(string keyword, out TooltipKeywordEntry entry)
//    {
//        entry = null;

//        if (string.IsNullOrWhiteSpace(keyword))
//            return false;

//        for (int i = 0; i < entries.Count; i++)
//        {
//            if (entries[i] == null)
//                continue;

//            if (entries[i].keyword == keyword)
//            {
//                entry = entries[i];
//                return true;
//            }
//        }

//        return false;
//    }
//}
[CreateAssetMenu(menuName = "CardGame/UI/Tooltip Keyword Database")]
public class TooltipKeywordDatabase : ScriptableObject
{
    public List<TooltipKeywordEntry> entries = new();

    public bool TryGet(string keyword, out TooltipKeywordEntry entry)
    {
        entry = null;

        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            TooltipKeywordEntry current = entries[i];

            if (current == null)
                continue;

            if (current.keyword == keyword)
            {
                entry = current;
                return true;
            }
        }

        return false;
    }

    public List<TooltipKeywordEntry> FindKeywordsInText(string text)
    {
        List<TooltipKeywordEntry> results = new();

        if (string.IsNullOrWhiteSpace(text))
            return results;

        for (int i = 0; i < entries.Count; i++)
        {
            TooltipKeywordEntry entry = entries[i];

            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.keyword))
                continue;

            if (text.Contains(entry.keyword))
            {
                if (!results.Contains(entry))
                    results.Add(entry);
            }
        }

        return results;
    }
}
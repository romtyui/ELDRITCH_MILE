using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "CardGame/Card Database/All Card Database")]
public class AllCardDatabase : ScriptableObject
{
    [Header("All Cards")]
    public List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;

    public void AddCard(CardData card)
    {
        if (card == null)
            return;

        if (cards.Contains(card))
            return;

        cards.Add(card);
    }

    public void RemoveNullsAndDuplicates()
    {
        List<CardData> cleanList = new();

        for (int i = 0; i < cards.Count; i++)
        {
            CardData card = cards[i];

            if (card == null)
                continue;

            if (cleanList.Contains(card))
                continue;

            cleanList.Add(card);
        }

        cards = cleanList;
    }

#if UNITY_EDITOR
    [ContextMenu("Editor/Collect All CardData In Project")]
    public void CollectAllCardDataInProject()
    {
        cards.Clear();

        string[] guids = AssetDatabase.FindAssets("t:CardData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

            AddCard(card);
        }

        SortByName();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AllCardDatabase] 已收集所有 CardData，數量 = {cards.Count}");
    }

    [ContextMenu("Editor/Sort By Name")]
    public void SortByName()
    {
        cards.Sort((a, b) =>
        {
            string aName = a != null
                ? (string.IsNullOrWhiteSpace(a.cardName) ? a.name : a.cardName)
                : "";

            string bName = b != null
                ? (string.IsNullOrWhiteSpace(b.cardName) ? b.name : b.cardName)
                : "";

            return string.Compare(aName, bName, System.StringComparison.Ordinal);
        });

        EditorUtility.SetDirty(this);
    }
#endif
}
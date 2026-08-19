using System;
using System.Collections.Generic;
using UnityEngine;

public class RelicsInventory : MonoBehaviour
{
    // =========================================================
    // Current Relics
    // =========================================================

    [Header("Current Relics")]
    [Tooltip(
        "目前使用 ScriptableObject 儲存玩家持有的遺物資料。" +
        "取得正式遺物資料類別後，可以再替換成對方的 RelicData 類型。"
    )]
    [SerializeField]
    private List<ScriptableObject> currentRelics =
        new();


    public IReadOnlyList<ScriptableObject>
        CurrentRelics =>
            currentRelics;


    public int Count =>
        currentRelics.Count;


    public event Action
        OnRelicsChanged;


    // =========================================================
    // Add Relic
    // =========================================================

    public bool AddRelic(
        ScriptableObject relic
    )
    {
        if (relic == null)
        {
            Debug.LogWarning(
                "[RelicsInventory] " +
                "AddRelic 失敗，relic 是 null"
            );

            return false;
        }


        currentRelics.Add(
            relic
        );


        Debug.Log(
            $"[RelicsInventory] " +
            $"獲得遺物：{relic.name}"
        );


        OnRelicsChanged?.Invoke();


        return true;
    }


    // =========================================================
    // Remove Relic
    // =========================================================

    public bool RemoveRelic(
        ScriptableObject relic
    )
    {
        if (relic == null)
            return false;


        bool removed =
            currentRelics.Remove(
                relic
            );


        if (!removed)
            return false;


        Debug.Log(
            $"[RelicsInventory] " +
            $"移除遺物：{relic.name}"
        );


        OnRelicsChanged?.Invoke();


        return true;
    }


    // =========================================================
    // Remove Relic At
    // =========================================================

    public bool RemoveRelicAt(
        int index
    )
    {
        if (index < 0 ||
            index >= currentRelics.Count)
        {
            Debug.LogWarning(
                $"[RelicsInventory] " +
                $"RemoveRelicAt index 超出範圍：" +
                $"{index}"
            );

            return false;
        }


        ScriptableObject relic =
            currentRelics[index];


        currentRelics.RemoveAt(
            index
        );


        Debug.Log(
            $"[RelicsInventory] " +
            $"移除遺物：" +
            $"{(relic != null ? relic.name : "null")}"
        );


        OnRelicsChanged?.Invoke();


        return true;
    }


    // =========================================================
    // Get Relic
    // =========================================================

    public ScriptableObject GetRelic(
        int index
    )
    {
        if (index < 0 ||
            index >= currentRelics.Count)
        {
            return null;
        }


        return currentRelics[index];
    }


    // =========================================================
    // Contains
    // =========================================================

    public bool ContainsRelic(
        ScriptableObject relic
    )
    {
        if (relic == null)
            return false;


        return currentRelics.Contains(
            relic
        );
    }


    // =========================================================
    // Clear
    // =========================================================

    public void Clear()
    {
        currentRelics.Clear();


        OnRelicsChanged?.Invoke();


        Debug.Log(
            "[RelicsInventory] " +
            "已清空所有遺物"
        );
    }
}
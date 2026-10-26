using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "CardGame/Enemy/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    [Header("All Enemies")]
    public List<EnemyData> enemies = new();

    public EnemyData GetById(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId)) return null;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];

            if (enemy == null) continue;
            if (enemy.enemyId == enemyId) return enemy;
        }

        return null;
    }

    public void AddEnemy(EnemyData enemy)
    {
        if (enemy == null) return;
        if (enemies.Contains(enemy)) return;

        enemies.Add(enemy);
    }

    public void RemoveNullsAndDuplicates()
    {
        List<EnemyData> cleanList = new();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyData enemy = enemies[i];

            if (enemy == null) continue;
            if (cleanList.Contains(enemy)) continue;

            cleanList.Add(enemy);
        }

        enemies = cleanList;
    }

#if UNITY_EDITOR
    [ContextMenu("Editor/Collect All EnemyData In Project")]
    public void CollectAllEnemyDataInProject()
    {
        Undo.RecordObject(this, "Collect All EnemyData");

        enemies.Clear();

        string[] guids = AssetDatabase.FindAssets("t:EnemyData");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);

            AddEnemy(enemy);
        }

        SortEnemyListById();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[EnemyDatabase] 已收集所有 EnemyData，數量 = {enemies.Count}", this);
    }

    [ContextMenu("Editor/Sort By Enemy ID")]
    public void SortByEnemyId()
    {
        Undo.RecordObject(this, "Sort Enemies By ID");

        SortEnemyListById();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    private void SortEnemyListById()
    {
        enemies.Sort((a, b) =>
        {
            string aId = a != null ? a.enemyId : "";
            string bId = b != null ? b.enemyId : "";

            return string.Compare(aId, bId, System.StringComparison.Ordinal);
        });
    }

    [ContextMenu("Editor/Remove Nulls And Duplicates")]
    public void CleanEnemyListInEditor()
    {
        Undo.RecordObject(this, "Clean Enemy List");

        RemoveNullsAndDuplicates();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log($"[EnemyDatabase] 已清除空項目與重複引用，剩餘數量 = {enemies.Count}", this);
    }

    [ContextMenu("Editor/Reset Enemy List")]
    public void ResetEnemyList()
    {
        Undo.RecordObject(this, "Reset Enemy List");

        enemies.Clear();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log("[EnemyDatabase] 已清空怪物清單，EnemyData 資產保留。", this);
    }
#endif
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Explore
{
    using EldritchMile.Core;

    /// <summary>
    /// 節點 → 房間 prefab 的對照表。
    ///
    /// 【取代什麼】舊的 MapNodeExplore（ScriptableObject）把 roomPrefab、敘事文字、
    /// targetSceneName 全綁在一起，而且是「一個節點一個資產」，地圖生成時要先準備好
    /// 一堆 SO。現在地圖節點只有 kind + contentId 兩個純資料欄位，
    /// 要生成哪個房間在這裡查表決定。
    /// </summary>
    [CreateAssetMenu(fileName = "RoomLibrary", menuName = "Eldritch/Room Library")]
    public class RoomLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("對應 RunNodeData.contentId。留空 = 此類型的通用房間")]
            public string contentId = "";

            public MapNodeKind nodeKind = MapNodeKind.Event;

            public GameObject roomPrefab;

            [Min(0f)] public float weight = 1f;
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>
        /// 依節點挑房間。優先比對 contentId，找不到就從同 kind 的通用房間依權重抽。
        /// </summary>
        public GameObject Pick(RunNodeData node, System.Random rng)
        {
            if (node == null) return null;

            // 1. 指名的 contentId
            if (!string.IsNullOrEmpty(node.contentId))
            {
                Entry exact = entries.Find(e => e != null && e.contentId == node.contentId);
                if (exact != null && exact.roomPrefab != null) return exact.roomPrefab;
            }

            // 2. 同類型的通用房間，依權重抽
            var candidates = new List<Entry>();
            float total = 0f;

            int sameKind = 0;
            int zeroWeight = 0;
            int missingPrefab = 0;

            foreach (Entry e in entries)
            {
                if (e == null) continue;
                if (e.nodeKind != node.kind) continue;

                sameKind++;

                if (e.roomPrefab == null) { missingPrefab++; continue; }
                if (e.weight <= 0f) { zeroWeight++; continue; }

                candidates.Add(e);
                total += e.weight;
            }

            if (candidates.Count == 0)
            {
                // 分辨三種完全不同的失敗原因，否則只說「找不到」很難查
                if (sameKind == 0)
                {
                    Debug.LogWarning($"[房間庫] 沒有任何 Node Kind = {node.kind} 的條目");
                }
                else if (zeroWeight > 0)
                {
                    Debug.LogWarning(
                        $"[房間庫] 有 {sameKind} 筆 {node.kind} 條目，但其中 {zeroWeight} 筆的 Weight 是 0 而被跳過。\n" +
                        "⚠️ Unity 用 Inspector 的 + 新增 List 元素時會零填充，不會套用程式裡的預設值 —— " +
                        "請手動把 Weight 改成 1。"
                    );
                }
                else if (missingPrefab > 0)
                {
                    Debug.LogWarning($"[房間庫] 有 {sameKind} 筆 {node.kind} 條目，但 Room Prefab 沒指定");
                }
                return null;
            }

            double roll = rng.NextDouble() * total;
            foreach (Entry e in candidates)
            {
                roll -= e.weight;
                if (roll <= 0) return e.roomPrefab;
            }

            return candidates[candidates.Count - 1].roomPrefab;
        }
    }
}

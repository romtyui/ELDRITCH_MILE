using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 依 StageType 生成 / 銷毀 Stage prefab。
    ///
    /// 【為什麼用 prefab 而不是把所有 UI 都放在場景裡】
    /// EventScene 的 hierarchy 保持乾淨，而且各 Stage 分屬不同 prefab 檔案，
    /// 與隊友同時作業時不會在同一個 .unity 上衝突。
    /// </summary>
    public class StageHost : MonoBehaviour
    {
        [Serializable]
        public class StageEntry
        {
            public StageType type;
            public GameObject prefab;

            [Tooltip("留空則掛在 defaultParent 底下")]
            public Transform customParent;
        }

        [Header("掛載位置")]
        [Tooltip("2.5D 房間、戰鬥 prefab 等世界物件")]
        public Transform worldRoot;

        [Tooltip("Stage 的 UI（Canvas_Stage）")]
        public Transform uiRoot;

        [Header("Stage 清單")]
        public List<StageEntry> stages = new List<StageEntry>();

        public StageController Current { get; private set; }
        public StageType CurrentType => Current != null ? Current.Stage : StageType.None;

        private GameObject currentInstance;

        /// <summary>
        /// 生成指定 Stage。呼叫前應先確保舊的已經 Unload。
        /// </summary>
        public StageController Load(StageType type)
        {
            if (currentInstance != null)
            {
                Debug.LogWarning($"[StageHost] 舊的 {CurrentType} 尚未卸載就要載入 {type}，強制卸載");
                Unload();
            }

            StageEntry entry = stages.Find(s => s.type == type);

            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"[StageHost] 找不到 {type} 的 prefab —— 尚未實作或未在 Inspector 指定");
                return null;
            }

            Transform parent = entry.customParent != null
                ? entry.customParent
                : (uiRoot != null ? uiRoot : transform);

            currentInstance = Instantiate(entry.prefab, parent, false);
            currentInstance.name = $"Stage_{type}";

            Current = currentInstance.GetComponent<StageController>();

            if (Current == null)
            {
                Debug.LogError($"[StageHost] {type} 的 prefab 根物件缺少 StageController");
            }

            ValidateInstance(type, currentInstance);

            return Current;
        }

        /// <summary>
        /// 診斷「從舊場景抽 prefab」最常見的兩個坑。
        ///
        /// 根 Canvas 的 RectTransform 是被 Unity 即時驅動的（尺寸=螢幕、scale 由 CanvasScaler 算），
        /// 所以序列化存下來的是無意義的 0。一旦它變成別人的子物件，那些值就不再被驅動，
        /// localScale 會真的是 0 —— prefab 有生成、但被縮到看不見。
        /// </summary>
        private void ValidateInstance(StageType type, GameObject instance)
        {
            if (instance == null) return;

            if (instance.GetComponent<Canvas>() != null)
            {
                Debug.LogWarning(
                    $"[StageHost] {type} 的 prefab 根物件帶著 Canvas。\n" +
                    "掛載目標（Canvas_Stage）已經提供 Canvas / CanvasScaler / GraphicRaycaster，" +
                    "巢狀的那一層不但多餘，還會讓 RectTransform 停止被驅動。\n" +
                    "請在 prefab 內依序移除 GraphicRaycaster → CanvasScaler → Canvas，" +
                    "再把根 RectTransform 設為 stretch 全滿、Scale 改回 1。"
                );
            }

            var rt = instance.transform as RectTransform;
            if (rt != null && rt.localScale.sqrMagnitude < 0.0001f)
            {
                Debug.LogError(
                    $"[StageHost] {type} 的 prefab 根物件 Scale 是 0，畫面上不會有任何東西。\n" +
                    "這通常是從舊場景的根 Canvas 直接拉出 prefab 造成的，移除 Canvas 後 Scale 不會自動修正，" +
                    "需要手動改回 (1, 1, 1)。"
                );
            }
        }

        public void Unload()
        {
            if (currentInstance != null)
            {
                Destroy(currentInstance);
                currentInstance = null;
            }

            Current = null;
        }

        public bool Has(StageType type)
        {
            StageEntry entry = stages.Find(s => s.type == type);
            return entry != null && entry.prefab != null;
        }
    }
}

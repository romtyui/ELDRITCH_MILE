using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MonsterAnimationEventRelay : MonoBehaviour
{
    [Tooltip("光暗兩份共用動畫時，只勾選其中一份，避免重複生成特效")]
    [SerializeField] private bool handleEffectEvents = true;
    [Serializable]

    public class EffectEntry
    {
        [Tooltip("填入 Animation Event 的 String 參數，例如 SoundWave")]
        public string eventId;

        [Tooltip("要生成的完整特效 Prefab")]
        public GameObject prefab;

        [Tooltip("生成位置與旋轉；未指定時使用此腳本所在物件")]
        public Transform spawnPoint;

        [Tooltip("相對掛點方向的位置偏移，單位為世界單位，不乘上掛點縮放")]
        public Vector3 positionOffset;

        [Tooltip("相對掛點旋轉的角度偏移")]
        public Vector3 rotationOffset;

        [Tooltip("乘上 Prefab 原本大小；1 為原大小，0.5 為一半")]
        public Vector3 scaleMultiplier = Vector3.one;

        [Tooltip("生成後是否跟隨掛點的位置、旋轉與縮放變化")]
        public bool followSpawnPoint;

        [Min(0f)]
        [Tooltip("生成後自動銷毀的秒數；0 表示由其他方式清除")]
        public float lifetime = 3f;

        [Tooltip("勾選後，以 Spawn Point 為父物件，使用本地位置、旋轉與大小生成")]
        public bool useLocalTransform;

        [Tooltip("使用本地座標時，直接指定生成物件的 Local Scale，不乘上 Prefab 原大小")]
        public Vector3 localScale = Vector3.one;
    }

    [SerializeField] private List<EffectEntry> effects = new List<EffectEntry>();

    private readonly List<GameObject> spawnedEffects = new List<GameObject>();

    public void PlayEffect(string eventId)
    {
        if (!handleEffectEvents) return;
        if (string.IsNullOrWhiteSpace(eventId)) return;

        spawnedEffects.RemoveAll(instance => instance == null);

        foreach (EffectEntry entry in effects)
        {
            if (entry == null || entry.eventId != eventId) continue;

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[{nameof(MonsterAnimationEventRelay)}] 特效 {eventId} 尚未指定 Prefab。", this);
                return;
            }

            Transform anchor = entry.spawnPoint != null ? entry.spawnPoint : transform;
            GameObject instance;

            if (entry.useLocalTransform)
            {
                instance = Instantiate(entry.prefab, anchor, false);
                instance.transform.localPosition = entry.positionOffset;
                instance.transform.localRotation = Quaternion.Euler(entry.rotationOffset);
                instance.transform.localScale = entry.localScale;

                if (!entry.followSpawnPoint) instance.transform.SetParent(null, true);
            }
            else
            {
                Vector3 spawnPosition = anchor.position + anchor.rotation * entry.positionOffset;
                Quaternion spawnRotation = anchor.rotation * Quaternion.Euler(entry.rotationOffset);
                instance = Instantiate(entry.prefab, spawnPosition, spawnRotation);
                instance.transform.localScale = Vector3.Scale(entry.prefab.transform.localScale, entry.scaleMultiplier);

                if (entry.followSpawnPoint) instance.transform.SetParent(anchor, true);
            }

            spawnedEffects.Add(instance);

            if (entry.lifetime > 0f) Destroy(instance, entry.lifetime);

            return;
        }

        Debug.LogWarning($"[{nameof(MonsterAnimationEventRelay)}] 找不到特效事件：{eventId}。", this);
    }

    public void ClearEffects()
    {
        foreach (GameObject instance in spawnedEffects)
        {
            if (instance == null) continue;
            Destroy(instance);
        }

        spawnedEffects.Clear();
    }

    private void OnDisable()
    {
        ClearEffects();
    }
}
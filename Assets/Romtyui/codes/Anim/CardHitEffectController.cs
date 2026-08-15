using System.Collections;
using UnityEngine;

public class CardHitEffectController : MonoBehaviour
{
    [Header("Effect Root")]

    [Tooltip(
        "所有卡牌命中特效生成的位置。" +
        "建議放在 Battle Canvas 底下。"
    )]
    public RectTransform effectRoot;


    [Header("Center Position")]

    [Tooltip(
        "TargetType.None 沒有角色目標時，" +
        "特效顯示在畫面中的位置。"
    )]
    public Vector2 screenCenterPosition =
        Vector2.zero;


    [Header("Audio")]

    [Tooltip("命中特效音效使用的 AudioSource")]
    public AudioSource audioSource;


    // =========================================================
    // 對角色目標生成特效
    // =========================================================

    public GameObject SpawnEffectOnTarget(CardHitEffectData data, BattleUnit target, bool playSound = true)
    {
        if (data == null)
            return null;

        if (data.effectPrefab == null)
            return null;

        if (target == null)
            return null;

        if (effectRoot == null)
        {
            Debug.LogWarning(
                "[CardHitEffectController] effectRoot 沒有指定"
            );

            return null;
        }


        GameObject effectObject =
            Instantiate(
                data.effectPrefab,
                effectRoot
            );


        RectTransform effectRect =
            effectObject.transform as RectTransform;


        /*
         * 取得目標的 RectTransform。
         *
         * 目前你的 BattleUnit / EnemyUnit
         * 都是場景中的 MonoBehaviour，
         * Enemy 也掛在 UI Slot 系統中。
         */
        RectTransform targetRect =
            target.transform as RectTransform;


        if (effectRect != null &&
            targetRect != null)
        {
            /*
             * 先取得目標的世界座標。
             */
            Vector3 targetWorldPosition =
                targetRect.position;


            /*
             * 再轉換成 EffectRoot 的本地座標。
             */
            Vector3 localPosition =
                effectRoot.InverseTransformPoint(
                    targetWorldPosition
                );


            effectRect.anchoredPosition =
                new Vector2(
                    localPosition.x,
                    localPosition.y
                )
                +
                data.positionOffset;


            effectRect.localScale =
                new Vector3(
                    data.scale.x,
                    data.scale.y,
                    1f
                );


            effectRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    data.rotationZ
                );
        }


        if (playSound)
        {
            PlayHitSound(data);
        }


        /*
         * 自動刪除。
         */
        if (data.lifeTime > 0f)
        {
            Destroy(
                effectObject,
                data.lifeTime
            );
        }


        return effectObject;
    }


    // =========================================================
    // 沒有角色目標的特效
    // =========================================================

    public GameObject SpawnEffectAtCenter(
        CardHitEffectData data
    )
    {
        if (data == null)
            return null;

        if (data.effectPrefab == null)
            return null;

        if (effectRoot == null)
        {
            Debug.LogWarning(
                "[CardHitEffectController] effectRoot 沒有指定"
            );

            return null;
        }


        GameObject effectObject =
            Instantiate(
                data.effectPrefab,
                effectRoot
            );


        RectTransform effectRect =
            effectObject.transform as RectTransform;


        if (effectRect != null)
        {
            effectRect.anchoredPosition =
                screenCenterPosition
                +
                data.positionOffset;


            effectRect.localScale =
                new Vector3(
                    data.scale.x,
                    data.scale.y,
                    1f
                );


            effectRect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    data.rotationZ
                );
        }


        PlayHitSound(data);


        if (data.lifeTime > 0f)
        {
            Destroy(
                effectObject,
                data.lifeTime
            );
        }


        return effectObject;
    }


    // =========================================================
    // 等待真正命中時間
    // =========================================================

    public IEnumerator WaitForImpact(
        CardHitEffectData data
    )
    {
        if (data == null)
            yield break;


        if (data.impactDelay <= 0f)
            yield break;


        /*
         * 使用 Realtime，
         * 避免教學暫停或 TimeScale 影響動畫命中時間。
         */
        yield return new WaitForSecondsRealtime(
            data.impactDelay
        );
    }


    // =========================================================
    // 單一角色完整播放
    // =========================================================

    public IEnumerator PlayOnTarget(
        CardHitEffectData data,
        BattleUnit target
    )
    {
        if (data == null)
            yield break;


        SpawnEffectOnTarget(
            data,
            target
        );


        yield return WaitForImpact(
            data
        );
    }


    // =========================================================
    // 畫面中央完整播放
    // =========================================================

    public IEnumerator PlayAtCenter(
        CardHitEffectData data
    )
    {
        if (data == null)
            yield break;


        SpawnEffectAtCenter(
            data
        );


        yield return WaitForImpact(
            data
        );
    }


    // =========================================================
    // Audio
    // =========================================================

    private void PlayHitSound(
        CardHitEffectData data
    )
    {
        if (data == null)
            return;

        if (data.hitSfx == null)
            return;


        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }


        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }


        audioSource.PlayOneShot(
            data.hitSfx,
            data.hitSfxVolume
        );
    }
}
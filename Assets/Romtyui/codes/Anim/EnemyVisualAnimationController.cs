using System.Collections;
using UnityEngine;

public class EnemyVisualAnimationController : MonoBehaviour
{
    [Header("Runtime Visuals")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject darkVisual;

    [Header("Runtime Data")]
    [SerializeField] private EnemyData enemyData;

    private Animator[] normalAnimators;
    private Animator[] darkAnimators;
    [Header("Audio")]
    public AudioSource audioSource;

    public void Bind(GameObject normalVisual, GameObject darkVisual, EnemyData enemyData)
    {
        this.normalVisual = normalVisual;
        this.darkVisual = darkVisual;
        this.enemyData = enemyData;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        RefreshAnimators();

        PlayIdle();
    }

    public void RefreshAnimators()
    {
        normalAnimators = normalVisual != null
            ? normalVisual.GetComponentsInChildren<Animator>(true)
            : new Animator[0];

        darkAnimators = darkVisual != null
            ? darkVisual.GetComponentsInChildren<Animator>(true)
            : new Animator[0];
    }

    public void PlayIdle()
    {
        PlayAnimation(EnemyAnimationType.Idle);
    }

    public IEnumerator PlayAttack()
    {
        PlayAnimation(EnemyAnimationType.Attack);
        yield return new WaitForSeconds(GetDuration(EnemyAnimationType.Attack));
        PlayIdle();
    }

    public IEnumerator PlayBlock()
    {
        PlayAnimation(EnemyAnimationType.Block);
        yield return new WaitForSeconds(GetDuration(EnemyAnimationType.Block));
        PlayIdle();
    }

    public IEnumerator PlaySpecialAttack()
    {
        PlayAnimation(EnemyAnimationType.SpecialAttack);
        yield return new WaitForSeconds(GetDuration(EnemyAnimationType.SpecialAttack));
        PlayIdle();
    }

    public IEnumerator PlayHurt()
    {
        PlayAnimation(EnemyAnimationType.Hurt);
        yield return new WaitForSeconds(GetDuration(EnemyAnimationType.Hurt));
        PlayIdle();
    }

    public IEnumerator PlayDeath()
    {
        PlayAnimation(EnemyAnimationType.Death);
        yield return new WaitForSeconds(GetDuration(EnemyAnimationType.Death));
    }

    public void PlayAnimation(EnemyAnimationType animationType)
    {
        string triggerName = GetTriggerName(animationType);

        if (string.IsNullOrWhiteSpace(triggerName))
            return;

        PlayTriggerOnAnimators(normalAnimators, triggerName);
        PlayTriggerOnAnimators(darkAnimators, triggerName);

        PlaySfx(animationType);
    }

    /// <summary>
    /// 這一組動畫用到的所有 trigger。切換動畫前要把其他的清掉，
    /// 否則上一個還排隊中的 trigger 會在下一次轉場時突然生效。
    /// </summary>
    private static readonly string[] AllTriggers =
        { "idle", "atk", "hurt", "death", "block", "special" };

    /// <summary>
    /// 已經抱怨過的 (controller, trigger) 組合。**只是為了不洗版** ——
    /// 缺動畫是內容問題，值得講一次，但不值得每一幀講一次。
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<string> warnedMissing
        = new System.Collections.Generic.HashSet<string>();

    private void PlayTriggerOnAnimators(Animator[] animators, string triggerName)
    {
        if (animators == null)
            return;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
                continue;

            // ⚠️ 沒有指定 Controller 的 Animator，讀 .parameters 或呼叫
            //    ResetTrigger / SetTrigger 都會噴 "Animator is not playing an
            //    AnimatorController"。場上確實有這種（BlockAnimator_01~03），
            //    所以要先擋掉，否則每一次動畫切換都會噴一輪。
            if (animator.runtimeAnimatorController == null)
                continue;

            // 只清「這個 controller 真的有」的 trigger。
            // 各敵人的 controller 參數不一致（有的沒有 block、有的沒有 special），
            // 無條件清會噴 "Parameter 'block' does not exist."
            for (int t = 0; t < AllTriggers.Length; t++)
            {
                if (HasTrigger(animator, AllTriggers[t]))
                    animator.ResetTrigger(AllTriggers[t]);
            }

            if (HasTrigger(animator, triggerName))
            {
                animator.SetTrigger(triggerName);
            }
            else
            {
                // 這一支是真的想播卻播不出來 —— 跟上面「清掉不存在的 trigger」不同，
                // 所以要講。但同一個組合只講一次
                string key = animator.runtimeAnimatorController.name + "/" + triggerName;
                if (warnedMissing.Add(key))
                {
                    Debug.LogWarning(
                        $"[敵人動畫] Controller「{animator.runtimeAnimatorController.name}」" +
                        $"沒有 trigger「{triggerName}」，這個動作不會有動畫。", animator);
                }
            }
        }
    }

    /// <summary>
    /// 這個 Animator 的 controller 有沒有這個 trigger 參數。
    ///
    /// `EnemyUnit` 裡有一支同名的，但它是 private 也不是 static，跨類別用不到；
    /// 為了不動他的類別結構，這裡放一份小的。**兩邊的判斷要保持一致。**
    /// </summary>
    private static bool HasTrigger(Animator animator, string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        if (string.IsNullOrWhiteSpace(triggerName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];

            if (p == null)
                continue;

            if (p.type != AnimatorControllerParameterType.Trigger)
                continue;

            if (p.name == triggerName)
                return true;
        }

        return false;
    }

    private string GetTriggerName(EnemyAnimationType animationType)
    {
        if (enemyData == null)
            return "";

        switch (animationType)
        {
            case EnemyAnimationType.Idle:
                return enemyData.idleTrigger;

            case EnemyAnimationType.Attack:
                return enemyData.attackTrigger;

            case EnemyAnimationType.Hurt:
                return enemyData.hurtTrigger;

            case EnemyAnimationType.Death:
                return enemyData.deathTrigger;

            case EnemyAnimationType.Block:
                return enemyData.blockTrigger;

            case EnemyAnimationType.SpecialAttack:
                return enemyData.specialAttackTrigger;

            default:
                return "";
        }
    }

    private float GetDuration(EnemyAnimationType animationType)
    {
        if (enemyData == null)
            return 0.5f;

        switch (animationType)
        {
            case EnemyAnimationType.Attack:
                return enemyData.attackAnimDuration;

            case EnemyAnimationType.Hurt:
                return enemyData.hurtAnimDuration;

            case EnemyAnimationType.Death:
                return enemyData.deathAnimDuration;

            case EnemyAnimationType.Block:
                return enemyData.blockAnimDuration;

            case EnemyAnimationType.SpecialAttack:
                return enemyData.specialAttackAnimDuration;

            default:
                return 0.1f;
        }
    }

    private void PlaySfx(EnemyAnimationType animationType)
    {
        if (enemyData == null)
            return;

        if (audioSource == null)
            return;

        AudioClip clip = GetSfx(animationType);

        if (clip == null)
            return;

        audioSource.PlayOneShot(clip, enemyData.sfxVolume);
    }

    private AudioClip GetSfx(EnemyAnimationType animationType)
    {
        if (enemyData == null)
            return null;

        switch (animationType)
        {
            case EnemyAnimationType.Attack:
                return enemyData.attackSfx;

            case EnemyAnimationType.Hurt:
                return enemyData.hurtSfx;

            case EnemyAnimationType.Death:
                return enemyData.deathSfx;

            case EnemyAnimationType.Block:
                return enemyData.blockSfx;

            case EnemyAnimationType.SpecialAttack:
                return enemyData.specialAttackSfx;

            default:
                return null;
        }
    }
}
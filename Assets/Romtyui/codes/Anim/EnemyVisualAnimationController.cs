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

    private void PlayTriggerOnAnimators(Animator[] animators, string triggerName)
    {
        if (animators == null)
            return;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
                continue;

            animator.ResetTrigger("idle");
            animator.ResetTrigger("atk");
            animator.ResetTrigger("hurt");
            animator.ResetTrigger("death");
            animator.ResetTrigger("block");
            animator.ResetTrigger("special");

            animator.SetTrigger(triggerName);
        }
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
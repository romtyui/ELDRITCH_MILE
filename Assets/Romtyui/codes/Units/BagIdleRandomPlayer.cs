using System.Collections;
using UnityEngine;

public class BagIdleRandomPlayer : MonoBehaviour
{
    public Animator animator;

    [Header("State Names")]
    public string idleStaticStateName = "bag_idle_static";
    public string idleAnimStateName = "bag_idle_anim";

    [Header("Interval")]
    public float minInterval = 3f;
    public float maxInterval = 6f;

    [Header("Animation")]
    public float idleAnimLength = 0.8f;

    private Coroutine routine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        routine = StartCoroutine(IdleRoutine());
    }

    private void OnDisable()
    {
        if (routine != null)
            StopCoroutine(routine);
    }

    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            if (animator != null)
                animator.Play(idleStaticStateName, 0, 0f);

            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            if (animator != null)
                animator.Play(idleAnimStateName, 0, 0f);

            yield return new WaitForSeconds(idleAnimLength);
        }
    }
}
using System.Collections;
using UnityEngine;

public class BagDrawAnimationController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("State Names")]
    public string idleStateName = "bag_idle";
    public string drawStateName = "bag_anim";
    public string drawTriggerName = "PlayBagDraw";

    [Header("Timing")]
    [Tooltip("bag_anim 播到這個時間點後暫停，等待抽牌完成。")]
    public float pauseTime = 0.2f;

    [Tooltip("bag_anim 的總長度。你的動畫目前大約是 0.35 秒。")]
    public float animationLength = 0.35f;

    private bool reachedPausePoint;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public IEnumerator PlayAndWaitForDraw(System.Func<IEnumerator> drawRoutine)
    {
        if (animator == null)
        {
            Debug.LogWarning("[BagDrawAnimationController] animator 沒有指定");

            if (drawRoutine != null)
                yield return drawRoutine();

            yield break;
        }

        reachedPausePoint = false;

        animator.speed = 1f;
        animator.ResetTrigger(drawTriggerName);
        animator.SetTrigger(drawTriggerName);

        // 等待 Animator 真的進入 bag_anim
        yield return WaitUntilState(drawStateName);

        // 等待播放到 pauseTime
        yield return WaitUntilAnimationTime(pauseTime);

        // 暫停在 0.2 秒
        animator.speed = 0f;
        reachedPausePoint = true;

        Debug.Log("[BagDrawAnimationController] 包包動畫暫停，開始抽牌");

        // 執行所有抽牌動畫
        if (drawRoutine != null)
            yield return drawRoutine();

        Debug.Log("[BagDrawAnimationController] 抽牌完成，包包動畫繼續");

        // 繼續播放 0.2 ~ 0.35
        animator.speed = 1f;

        yield return WaitUntilAnimationTime(animationLength);

        // 切回 idle
        animator.speed = 1f;
        animator.Play(idleStateName, 0, 0f);
    }

    private IEnumerator WaitUntilState(string stateName)
    {
        while (true)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName(stateName))
                yield break;

            yield return null;
        }
    }

    private IEnumerator WaitUntilAnimationTime(float targetTime)
    {
        while (true)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(drawStateName))
            {
                yield return null;
                continue;
            }

            float currentTime = stateInfo.normalizedTime * animationLength;

            if (currentTime >= targetTime)
                yield break;

            yield return null;
        }
    }
}
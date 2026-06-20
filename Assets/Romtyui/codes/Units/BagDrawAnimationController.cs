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
    public float pauseTime = 0.2f;
    public float animationLength = 0.35f;

    [Header("Debug")]
    public bool debugLog = true;
    public float waitStateTimeout = 2f;

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

        if (pauseTime >= animationLength)
        {
            Debug.LogWarning($"[BagDrawAnimationController] pauseTime 必須小於 animationLength。pauseTime={pauseTime}, animationLength={animationLength}");
        }

        if (debugLog)
            Debug.Log($"[BagDraw] 開始播放，Trigger={drawTriggerName}, DrawState={drawStateName}");

        animator.speed = 1f;
        animator.ResetTrigger(drawTriggerName);
        animator.SetTrigger(drawTriggerName);

        bool enteredState = false;
        yield return WaitUntilState(drawStateName, result => enteredState = result);

        if (!enteredState)
        {
            Debug.LogWarning($"[BagDraw] 沒有進入狀態 {drawStateName}，請檢查 Animator State 名稱 / Trigger / Transition");

            if (drawRoutine != null)
                yield return drawRoutine();

            yield break;
        }

        if (debugLog)
            Debug.Log($"[BagDraw] 已進入 {drawStateName}，等待到 {pauseTime} 秒");

        yield return WaitUntilAnimationTime(pauseTime);

        if (debugLog)
            Debug.Log("[BagDraw] 到達暫停點，暫停並開始抽牌");

        animator.speed = 0f;

        if (drawRoutine != null)
            yield return drawRoutine();

        if (debugLog)
            Debug.Log("[BagDraw] 抽牌完成，繼續播放背包動畫");

        animator.speed = 1f;

        yield return WaitUntilAnimationTime(animationLength);

        if (debugLog)
            Debug.Log($"[BagDraw] 播放完成，切回 {idleStateName}");

        animator.Play(idleStateName, 0, 0f);
        animator.speed = 1f;
    }

    private IEnumerator WaitUntilState(string stateName, System.Action<bool> result)
    {
        float timer = 0f;

        while (timer < waitStateTimeout)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName(stateName))
            {
                result?.Invoke(true);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        result?.Invoke(false);
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
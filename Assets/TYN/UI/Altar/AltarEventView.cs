using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇特殊事件的**表現層**。只負責演，不負責發牌。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【流程】（2026-10-12 美術稿）
    ///   1. 兩隻手從畫面外伸進來。**這時候不能互動** —— 只是讓玩家看清楚有兩隻手
    ///   2. 顯示「你願意相信嗎?」與 YES / NO
    ///   3. YES → 提示收掉，兩隻手變成可以點 → 點哪一隻，那隻就把牌遞給你
    ///      NO  → 兩隻手縮回去，什麼都不拿，直接離開
    ///
    /// 【為什麼表現層與給牌分開】給哪一張牌是 `SpecialEventStageController.offers`
    /// 的事，它本來就會處理進牌組、寫紀錄、播收尾台詞。這支只回報
    /// 「玩家選了第幾隻手」或「他說不」，資料怎麼動與這裡無關 ——
    /// 之後改成三隻手、或改成別的題目，這支都不用動。
    ///
    /// 【左右手對應哪張牌】左手 = offers[0]、右手 = offers[1]。
    /// 照組員的決定，**兩隻手不顯示名字**，靠視覺與之後要加的特效區分。
    /// </summary>
    public class AltarEventView : MonoBehaviour
    {
        [Header("兩隻手")]
        [Tooltip("左手 = offers[0]")]
        public AltarHand leftHand;

        [Tooltip("右手 = offers[1]")]
        public AltarHand rightHand;

        [Tooltip("兩隻手錯開幾秒伸出來。0 = 同時到（會像機械手臂）")]
        [Min(0f)] public float handStagger = 0.25f;

        [Tooltip("兩隻手都到位之後，等多久才問那句話")]
        [Min(0f)] public float pauseBeforePrompt = 0.5f;

        [Header("眼睛")]
        public AltarEye eye;

        [Header("氣氛")]
        [Tooltip("背景壓暗那一層。留空會在同一個物件上找。沒有的話只是不壓暗，流程照跑")]
        public AltarMood mood;

        [Header("問句與 YES / NO")]
        public CanvasGroup promptGroup;
        public TextMeshProUGUI promptText;
        public Button yesButton;
        public Button noButton;

        [TextArea(1, 2)] public string promptLine = "你願意相信嗎?";

        [Tooltip("選了 YES 之後、手變成可點時顯示的提示。留空則不顯示")]
        [TextArea(1, 2)] public string pickLine = "伸出你的手。";

        [Min(0f)] public float fadeSeconds = 0.35f;

        /// <summary>玩家選了第幾隻手（0 = 左、1 = 右）。</summary>
        public event Action<int> OnHandChosen;

        /// <summary>玩家說不。什麼都不給，直接離開。</summary>
        public event Action OnDeclined;

        private Coroutine flow;
        private bool answered;

        private void Awake()
        {
            if (promptGroup != null)
            {
                promptGroup.alpha = 0f;
                promptGroup.blocksRaycasts = false;
                promptGroup.interactable = false;
            }

            if (mood == null) mood = GetComponent<AltarMood>();

            if (yesButton != null) yesButton.onClick.AddListener(HandleYes);
            if (noButton != null) noButton.onClick.AddListener(HandleNo);

            if (leftHand != null) leftHand.OnPicked += HandlePicked;
            if (rightHand != null) rightHand.OnPicked += HandlePicked;
        }

        private void OnDestroy()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(HandleYes);
            if (noButton != null) noButton.onClick.RemoveListener(HandleNo);
            if (leftHand != null) leftHand.OnPicked -= HandlePicked;
            if (rightHand != null) rightHand.OnPicked -= HandlePicked;
        }

        /// <summary>從頭演一次。Stage 進場、開場白講完之後呼叫。</summary>
        public void Begin()
        {
            if (flow != null) StopCoroutine(flow);
            answered = false;
            flow = StartCoroutine(BeginRoutine());
        }

        private IEnumerator BeginRoutine()
        {
            if (leftHand != null) leftHand.PrepareHidden();
            if (rightHand != null) rightHand.PrepareHidden();
            if (promptGroup != null) { promptGroup.alpha = 0f; promptGroup.blocksRaycasts = false; }

            if (promptText != null) promptText.text = promptLine;
            if (mood != null) mood.SetPhase(AltarMood.Phase.Hidden);

            // 1. 兩隻手伸出來 —— 這段不能互動
            // 背景在手進來之前**先**開始壓暗。等手到了才暗會看起來像兩件事
            if (mood != null) mood.SetPhase(AltarMood.Phase.Reaching);
            if (leftHand != null) leftHand.ReachIn(0f);
            if (rightHand != null) rightHand.ReachIn(handStagger);

            float wait = (leftHand != null ? leftHand.ReachSeconds : 0f) + handStagger + pauseBeforePrompt;
            yield return new WaitForSecondsRealtime(wait);

            // 2. 問那句話。問話的同時再壓深一點，視線收到中間
            if (mood != null) mood.SetPhase(AltarMood.Phase.Asking);
            yield return FadePrompt(1f, true);

            flow = null;
        }

        private IEnumerator FadePrompt(float target, bool interactive)
        {
            if (promptGroup == null) yield break;

            promptGroup.blocksRaycasts = interactive;
            promptGroup.interactable = interactive;

            float from = promptGroup.alpha;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                promptGroup.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            promptGroup.alpha = target;
        }

        // ==========================================
        private void HandleYes()
        {
            if (answered) return;
            answered = true;
            StartCoroutine(YesRoutine());
        }

        private IEnumerator YesRoutine()
        {
            // 問句收掉，但如果有「伸出你的手」就留著當指引
            if (string.IsNullOrEmpty(pickLine))
            {
                yield return FadePrompt(0f, false);
            }
            else
            {
                yield return FadePrompt(0f, false);
                if (promptText != null) promptText.text = pickLine;
                if (yesButton != null) yesButton.gameObject.SetActive(false);
                if (noButton != null) noButton.gameObject.SetActive(false);
                yield return FadePrompt(1f, false);   // 只是字，不收點擊
            }

            // 3. 現在才開放點手。暗角同時收緊 —— 只剩兩隻手看得清楚
            if (mood != null) mood.SetPhase(AltarMood.Phase.Picking);
            if (leftHand != null) leftHand.SetPickable(true);
            if (rightHand != null) rightHand.SetPickable(true);
        }

        private void HandleNo()
        {
            if (answered) return;
            answered = true;
            StartCoroutine(NoRoutine());
        }

        private IEnumerator NoRoutine()
        {
            yield return FadePrompt(0f, false);
            if (mood != null) mood.SetPhase(AltarMood.Phase.Leaving);

            if (leftHand != null) leftHand.Withdraw();
            if (rightHand != null) rightHand.Withdraw();

            yield return new WaitForSecondsRealtime(leftHand != null ? leftHand.ReachSeconds * 0.8f : 0.5f);

            var h = OnDeclined;
            if (h != null) h();
        }

        private void HandlePicked(AltarHand hand)
        {
            StartCoroutine(PickedRoutine(hand));
        }

        private IEnumerator PickedRoutine(AltarHand hand)
        {
            int index = (hand == rightHand) ? 1 : 0;

            // 沒被選的那隻先退場，被選的那隻把牌遞出來
            AltarHand other = (hand == leftHand) ? rightHand : leftHand;
            if (other != null) other.Withdraw();

            yield return FadePrompt(0f, false);
            if (mood != null) mood.SetPhase(AltarMood.Phase.Granting);

            yield return hand.PlayGrant();

            if (mood != null) mood.SetPhase(AltarMood.Phase.Leaving);

            var h = OnHandChosen;
            if (h != null) h(index);
        }
    }
}

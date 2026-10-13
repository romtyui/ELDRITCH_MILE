using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using EldritchMile.Core;

namespace EldritchMile.UI
{
    /// <summary>
    /// 祭壇特殊事件的**表現層**。只負責演，發牌與收尾是 `SpecialEventStageController` 的事。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【流程】（2026-10-13 改版）
    ///   1. 兩隻手從畫面外伸進來。**這時候不能互動** —— 只是讓玩家看清楚有兩隻手
    ///   2. 「你願意相信嗎?」與 YES / NO。**YES 對應畫面左邊的手、NO 對應右邊**，
    ///      滑鼠移到選項上，對應的那隻手就會動
    ///   3. 點了之後兩隻手**都收回去**，對應的那隻再**拿著東西**伸出來：
    ///      · YES／左手：一張神牌（offers[0]，目前是 ocean_god），「收下吧。」點牌收下
    ///      · NO／右手：Romtyui 戰鬥結算的獎勵畫面，挑你要的（<see cref="AltarRewardBridge"/>）
    ///   4. 拿完，手收回去，回報 <see cref="OnFinished"/>
    ///
    /// 【改版前】是「YES 才能點手、NO 什麼都不給直接走」。現在兩邊都會給東西，
    /// 手本身也不收點擊了（見 <see cref="AltarHand"/>）。
    /// </summary>
    public class AltarEventView : MonoBehaviour
    {
        public enum Choice { Believe = 0, Refuse = 1 }

        [Header("兩隻手")]
        [Tooltip("畫面左邊 = YES。拿神牌出來")]
        public AltarHand leftHand;

        [Tooltip("畫面右邊 = NO。拿獎勵出來")]
        public AltarHand rightHand;

        [Tooltip("兩隻手錯開幾秒伸出來。0 = 鏡像同時動（組員要的是這個）")]
        [Min(0f)] public float handStagger = 0f;

        [Tooltip("兩隻手都到位之後，等多久才問那句話")]
        [Min(0f)] public float pauseBeforePrompt = 0.5f;

        [Header("眼睛")]
        public AltarEye eye;

        [Tooltip("送禮時眼睛往上讓多少。示意圖《給牌》上眼睛在 y≈305（平常 110）——\n"
                 + "不讓的話，牌的上緣會蓋住眼睛")]
        public Vector2 eyeGiveOffset = new Vector2(0f, 195f);

        [Min(0.05f)] public float eyeMoveSeconds = 0.6f;

        [Header("氣氛")]
        [Tooltip("背景壓暗那一層。留空會在同一個物件上找。沒有的話只是不壓暗，流程照跑")]
        public AltarMood mood;

        [Header("問句與 YES / NO")]
        public CanvasGroup promptGroup;
        public TextMeshProUGUI promptText;
        public Button yesButton;
        public Button noButton;

        [TextArea(1, 2)] public string promptLine = "你願意相信嗎?";

        [Min(0f)] public float fadeSeconds = 0.35f;

        [Header("選完之後")]
        [Tooltip("兩隻手收回去之後、拿東西伸出來之前停多久。0 會像沒收回去")]
        [Min(0f)] public float pauseBeforeGive = 0.3f;

        [Tooltip("畫面下方的那句話（「收下吧。」）")]
        public CanvasGroup captionGroup;
        public TextMeshProUGUI captionText;

        [Header("左手（YES）：神牌")]
        [Tooltip("掛在左手 Gift Anchor 底下的那張牌")]
        public BattleCardView giftCard;

        [Tooltip("點牌收下。留空就用 giftCard 身上的 Button")]
        public Button giftAcceptButton;

        [TextArea(1, 2)] public string giftLine = "收下吧。";

        [Tooltip("收下之後，手放開牌自己收回去，牌在畫面中間停多久才淡出")]
        [Min(0f)] public float giftHoldSeconds = 0.35f;

        [Min(0.05f)] public float giftFadeSeconds = 0.6f;

        [Tooltip("淡出時牌放大多少（1 = 不放大）。往鏡頭靠近一點，像是被你拿走")]
        [Min(0.5f)] public float giftFadeScale = 1.08f;

        [Header("右手（NO）：獎勵")]
        public AltarRewardBridge rewards;

        [TextArea(1, 2)] public string rewardLine = "";

        /// <summary>
        /// 演完了。Believe = 玩家收下了神牌（要由 Stage 真的把牌發出去）；
        /// Refuse = 獎勵畫面已經結束（獎勵由那個畫面自己發，Stage 只要收尾）。
        /// </summary>
        public event Action<Choice> OnFinished;

        private Coroutine flow;
        private bool answered;
        private bool accepted;
        private RunContext run;
        private CardVisualData giftVisual;
        private RectTransform eyeRoot;
        private Vector2 eyeHome;

        // 牌原本掛在左手 GiftAnchor 底下的位置。收下時會拆出來留在畫面上，下次要掛回去
        private Transform giftHomeParent;
        private int giftHomeSibling;
        private Vector3 giftHomePos, giftHomeScale;
        private Quaternion giftHomeRot;
        private CanvasGroup giftGroup;

        private void Awake()
        {
            if (mood == null) mood = GetComponent<AltarMood>();
            if (eye != null) { eyeRoot = (RectTransform)eye.transform; eyeHome = eyeRoot.anchoredPosition; }
            if (giftAcceptButton == null && giftCard != null) giftAcceptButton = giftCard.GetComponent<Button>();
            if (giftCard != null)
            {
                Transform g = giftCard.transform;
                giftHomeParent = g.parent; giftHomeSibling = g.GetSiblingIndex();
                giftHomePos = g.localPosition; giftHomeRot = g.localRotation; giftHomeScale = g.localScale;
                giftGroup = giftCard.GetComponent<CanvasGroup>();
                if (giftGroup == null) giftGroup = giftCard.gameObject.AddComponent<CanvasGroup>();
            }

            SetGroup(promptGroup, 0f, false);
            SetGroup(captionGroup, 0f, false);
            if (giftCard != null) giftCard.Hide();

            if (yesButton != null) { yesButton.onClick.AddListener(HandleYes); AddHover(yesButton, () => leftHand); }
            if (noButton != null) { noButton.onClick.AddListener(HandleNo); AddHover(noButton, () => rightHand); }
            if (giftAcceptButton != null) giftAcceptButton.onClick.AddListener(HandleAccept);
        }

        private void OnDestroy()
        {
            if (yesButton != null) yesButton.onClick.RemoveListener(HandleYes);
            if (noButton != null) noButton.onClick.RemoveListener(HandleNo);
            if (giftAcceptButton != null) giftAcceptButton.onClick.RemoveListener(HandleAccept);
        }

        /// <summary>
        /// 從頭演一次。<paramref name="gift"/> = 左手要拿的那張牌的卡面（offers[0]）；
        /// <paramref name="context"/> 給右手的獎勵畫面用。
        /// </summary>
        public void Begin(CardVisualData gift, RunContext context)
        {
            giftVisual = gift;
            run = context;
            if (flow != null) StopCoroutine(flow);
            answered = false;
            accepted = false;
            flow = StartCoroutine(BeginRoutine());
        }

        private IEnumerator BeginRoutine()
        {
            if (leftHand != null) leftHand.PrepareHidden();
            if (rightHand != null) rightHand.PrepareHidden();
            RestoreGift();
            if (giftCard != null) giftCard.Hide();
            SetGroup(promptGroup, 0f, false);
            SetGroup(captionGroup, 0f, false);
            if (yesButton != null) yesButton.gameObject.SetActive(true);
            if (noButton != null) noButton.gameObject.SetActive(true);
            if (promptText != null) promptText.text = promptLine;
            if (eyeRoot != null) eyeRoot.anchoredPosition = eyeHome;

            // 1. 兩隻手伸出來 —— 這段不能互動。背景在手進來之前**先**開始壓暗
            if (mood != null) mood.SetPhase(AltarMood.Phase.Reaching);
            if (leftHand != null) leftHand.ReachIn(0f);
            if (rightHand != null) rightHand.ReachIn(handStagger);

            yield return new WaitForSecondsRealtime(HandSeconds() + handStagger + pauseBeforePrompt);

            // 2. 問那句話
            if (mood != null) mood.SetPhase(AltarMood.Phase.Asking);
            yield return Fade(promptGroup, 1f, true);

            flow = null;
        }

        // ==========================================
        private void HandleYes() { Answer(Choice.Believe); }
        private void HandleNo() { Answer(Choice.Refuse); }

        private void Answer(Choice choice)
        {
            if (answered) return;
            answered = true;
            if (flow != null) StopCoroutine(flow);
            flow = StartCoroutine(GiveRoutine(choice));
        }

        private IEnumerator GiveRoutine(Choice choice)
        {
            AltarHand giver = choice == Choice.Believe ? leftHand : rightHand;

            // 3. 問句收掉，兩隻手都收回去
            yield return Fade(promptGroup, 0f, false);
            if (leftHand != null) { leftHand.SetHovered(false); leftHand.Withdraw(); }
            if (rightHand != null) { rightHand.SetHovered(false); rightHand.Withdraw(); }
            if (mood != null) mood.SetPhase(AltarMood.Phase.Picking);

            yield return new WaitForSecondsRealtime(HandSeconds() * 0.75f + pauseBeforeGive);

            // 對應的那隻拿著東西伸出來
            if (choice == Choice.Believe && giftCard != null)
            {
                giftCard.Show(giftVisual);
                if (giftAcceptButton != null) giftAcceptButton.interactable = false;
            }

            if (mood != null) mood.SetPhase(AltarMood.Phase.Granting);
            StartCoroutine(MoveEye(eyeHome + eyeGiveOffset));
            if (giver != null) giver.ReachToGive(0f);
            yield return new WaitForSecondsRealtime(HandSeconds());

            if (choice == Choice.Believe) yield return BelieveRoutine();
            else yield return RefuseRoutine();

            // 4. 收回去、亮回來。YES 的話牌已經拆下來了 —— 手**自己**收回去，牌留在畫面中間
            if (giver != null) giver.Withdraw();

            // ⚠️ YES 時牌還留在中間 —— 眼睛與背景要等牌淡出之後才回去，
            //    不然眼睛會滑回原位、正好壓在牌上
            if (choice != Choice.Believe) BeginReturn();
            yield return new WaitForSecondsRealtime(HandSeconds() * 0.75f);

            if (choice == Choice.Believe)
            {
                yield return FadeOutGift();
                BeginReturn();
                yield return new WaitForSecondsRealtime(eyeMoveSeconds);
            }
            if (giftCard != null) giftCard.Hide();
            RestoreGift();

            flow = null;
            var h = OnFinished;
            if (h != null) h(choice);
        }

        private IEnumerator BelieveRoutine()
        {
            ShowCaption(giftLine);
            yield return Fade(captionGroup, 1f, false);

            accepted = false;
            if (giftAcceptButton != null) giftAcceptButton.interactable = true;
            else accepted = true;   // 沒有按鈕就不卡人

            while (!accepted) yield return null;

            if (giftAcceptButton != null) giftAcceptButton.interactable = false;
            yield return Fade(captionGroup, 0f, false);

            // 手放開牌：把牌從 GiftAnchor 拆出來掛到 AltarUI，世界座標不變 ——
            // 接下來手自己收回去，牌留在原地（2026-10-13 美術回報：牌應該留在中間）
            DetachGift();
        }

        private void DetachGift()
        {
            if (giftCard == null) return;
            Transform g = giftCard.transform;
            g.SetParent(transform, true);

            // 排在兩隻手之上、眼睛與字之下 —— 收回去的手從牌後面退場
            int above = 0;
            if (leftHand != null) above = Mathf.Max(above, leftHand.transform.GetSiblingIndex() + 1);
            if (rightHand != null) above = Mathf.Max(above, rightHand.transform.GetSiblingIndex() + 1);
            g.SetSiblingIndex(above);
        }

        private IEnumerator FadeOutGift()
        {
            if (giftCard == null) yield break;
            if (giftHoldSeconds > 0f) yield return new WaitForSecondsRealtime(giftHoldSeconds);

            Transform g = giftCard.transform;
            Vector3 from = g.localScale;
            float t = 0f;
            while (t < giftFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / giftFadeSeconds);
                float e = 1f - (1f - u) * (1f - u);
                g.localScale = from * Mathf.Lerp(1f, giftFadeScale, e);
                if (giftGroup != null) giftGroup.alpha = 1f - e;
                yield return null;
            }
        }

        /// <summary>牌掛回左手 GiftAnchor 原本的位置，下次進祭壇才拿得出來。</summary>
        private void RestoreGift()
        {
            if (giftCard == null || giftHomeParent == null) return;
            Transform g = giftCard.transform;
            if (g.parent != giftHomeParent) g.SetParent(giftHomeParent, false);
            g.SetSiblingIndex(giftHomeSibling);
            g.localPosition = giftHomePos; g.localRotation = giftHomeRot; g.localScale = giftHomeScale;
            if (giftGroup != null) giftGroup.alpha = 1f;
        }

        private IEnumerator RefuseRoutine()
        {
            if (!string.IsNullOrEmpty(rewardLine))
            {
                ShowCaption(rewardLine);
                yield return Fade(captionGroup, 1f, false);
            }

            bool done = false;
            if (rewards != null) rewards.Open(run, () => done = true);
            else done = true;

            while (!done) yield return null;

            yield return Fade(captionGroup, 0f, false);
        }

        private void HandleAccept() { accepted = true; }

        /// <summary>收場：背景亮回來、眼睛回原位。</summary>
        private void BeginReturn()
        {
            if (mood != null) mood.SetPhase(AltarMood.Phase.Leaving);
            StartCoroutine(MoveEye(eyeHome));
        }

        private IEnumerator MoveEye(Vector2 to)
        {
            if (eyeRoot == null) yield break;
            Vector2 from = eyeRoot.anchoredPosition;
            float t = 0f;
            while (t < eyeMoveSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / eyeMoveSeconds);
                u = u * u * (3f - 2f * u);
                eyeRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, u);
                yield return null;
            }
            eyeRoot.anchoredPosition = to;
        }

        // ==========================================
        private float HandSeconds()
        {
            if (leftHand != null) return leftHand.ReachSeconds;
            if (rightHand != null) return rightHand.ReachSeconds;
            return 0.8f;
        }

        private void ShowCaption(string line)
        {
            if (captionText != null) captionText.text = line;
        }

        /// <summary>
        /// 選項 hover → 對應的手動一下。**用 EventTrigger 在程式裡掛**，
        /// 不在 prefab 上手接 —— 手接的話換按鈕就會斷，而且不會有任何錯誤訊息。
        /// </summary>
        private void AddHover(Button button, Func<AltarHand> hand)
        {
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (!answered && hand() != null) hand().SetHovered(true); });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (hand() != null) hand().SetHovered(false); });
            trigger.triggers.Add(exit);
        }

        private IEnumerator Fade(CanvasGroup group, float target, bool interactive)
        {
            if (group == null) yield break;

            group.blocksRaycasts = interactive;
            group.interactable = interactive;

            float from = group.alpha;
            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / fadeSeconds));
                yield return null;
            }
            group.alpha = target;
        }

        private static void SetGroup(CanvasGroup g, float alpha, bool interactive)
        {
            if (g == null) return;
            g.alpha = alpha;
            g.blocksRaycasts = interactive;
            g.interactable = interactive;
        }
    }
}

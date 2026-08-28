using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI.ProbabilityDialogue
{
    using EldritchMile.Core;
    using EldritchMile.Core.ProbabilityDialogue;

    /// <summary>
    /// 把 <see cref="ProbabilityDialogueSession"/> 接到畫面上。
    ///
    /// ⚠️ **這一支只接結果，不做任何判斷**（規格 §8：EventView 不可記錄主要 State）。
    /// 機率怎麼算、成不成功，全部在 Session 裡；這裡只負責畫出來與收使用者的輸入。
    ///
    /// 【顏色從哪來】<see cref="AttributeChartData"/> —— **不在這裡另外維護一張對照表**。
    /// 卡框的顏色（本我紅／超我藍／自我綠）是美術畫在圖裡的，
    /// 回答的色點必須跟它一致；兩邊指向同一份資料才不會各自漂移。
    /// </summary>
    public class ProbabilityDialogueView : MonoBehaviour
    {
        [Header("屬性顏色")]
        [Tooltip("屬性 → 顏色／名稱的來源。**跟探索打牌用的是同一份**。\n" +
                 "留空會退回一組寫死的預設色，並發一次警告")]
        public AttributeChartData attributeChart;

        [Header("NPC")]
        public Image backgroundImage;
        public Image npcPortrait;
        public TextMeshProUGUI npcNameText;

        [Tooltip("NPC 的問話。失敗後會換成語氣更強的下一段")]
        public TextMeshProUGUI promptText;

        [Header("回答")]
        [Tooltip("回答按鈕的容器")]
        public RectTransform answerRoot;
        public ProbabilityAnswerUI answerPrefab;

        [Header("卡牌")]
        [Tooltip("手牌區的容器")]
        public RectTransform handRoot;
        public ProbabilityCardUI cardPrefab;

        [Header("提示")]
        [Tooltip("卡牌沒有影響到任何回答時顯示（規格 R8：要顯示 No Effect）")]
        public GameObject noEffectHint;

        [Min(0f)] public float noEffectSeconds = 1.2f;

        [Header("節奏")]
        [Tooltip("判定結果顯示多久才跑後續。太短玩家看不到自己成功了沒")]
        [Min(0f)] public float resolveDisplaySeconds = 1.0f;

        // ==========================================
        private ProbabilityDialogueSession session;
        private readonly List<ProbabilityAnswerUI> answerUIs = new List<ProbabilityAnswerUI>();
        private readonly List<ProbabilityCardUI> cardUIs = new List<ProbabilityCardUI>();
        private Coroutine noEffectRoutine;

        /// <summary>UI 自己的輸入鎖。Session 也有 State，這裡只是避免動畫播到一半又收到點擊。</summary>
        private bool inputLocked;

        public void Attach(ProbabilityDialogueSession s, CharacterDatabase charDb = null)
        {
            session = s;
            if (session == null) return;

            session.OnStarted += HandleStarted;
            session.OnCardPlayed += HandleCardPlayed;
            session.OnOptionResolved += HandleOptionResolved;
            session.OnOptionDisabled += HandleOptionDisabled;
            session.OnPromptChanged += HandlePromptChanged;
            session.OnHandChanged += RebuildHand;
            session.OnEnded += HandleEnded;

            this.charDb = charDb;
        }

        private CharacterDatabase charDb;

        public void Detach()
        {
            if (session == null) return;
            session.OnStarted -= HandleStarted;
            session.OnCardPlayed -= HandleCardPlayed;
            session.OnOptionResolved -= HandleOptionResolved;
            session.OnOptionDisabled -= HandleOptionDisabled;
            session.OnPromptChanged -= HandlePromptChanged;
            session.OnHandChanged -= RebuildHand;
            session.OnEnded -= HandleEnded;
            session = null;
        }

        // ==========================================
        /// <summary>
        /// 屬性的顯示顏色。**單一來源是 `AttributeChartData`** ——
        /// 卡框圖的顏色就是照它畫的，這裡再定義一次就會有兩個真相。
        /// </summary>
        public Color ColorOf(EldritchMile.Core.ExploreAttribute attr)
        {
            if (attributeChart != null) return attributeChart.ColorOf(attr);

            if (!warnedNoChart)
            {
                warnedNoChart = true;   // 每場只吵一次，不然每個色點都印一行
                Debug.LogWarning(
                    "[機率對話] View 沒有指定 Attribute Chart，色點先用預設色。\n" +
                    "⚠️ 這會讓色點跟卡框的顏色對不上 —— 把 AttributeChart 拉進來就好。", this);
            }

            switch (attr)
            {
                case EldritchMile.Core.ExploreAttribute.Id:       return new Color(0.86f, 0.34f, 0.32f);
                case EldritchMile.Core.ExploreAttribute.Superego: return new Color(0.36f, 0.60f, 0.86f);
                case EldritchMile.Core.ExploreAttribute.Ego:      return new Color(0.44f, 0.76f, 0.48f);
                default:                                          return new Color(0.72f, 0.72f, 0.72f);
            }
        }

        private bool warnedNoChart;

        // ==========================================
        private void HandleStarted()
        {
            inputLocked = false;

            if (backgroundImage != null && session.Data.background != null)
            {
                backgroundImage.sprite = session.Data.background;
                backgroundImage.enabled = true;
            }

            CharacterData npc = charDb != null ? charDb.GetById(session.Data.npcId) : null;
            if (npcNameText != null) npcNameText.text = npc != null ? npc.Label : session.Data.npcId;
            if (npcPortrait != null)
            {
                npcPortrait.sprite = npc != null ? npc.portrait : null;
                npcPortrait.enabled = npcPortrait.sprite != null;
            }

            RebuildAnswers();
            RebuildHand();
        }

        private void RebuildAnswers()
        {
            for (int i = 0; i < answerUIs.Count; i++) if (answerUIs[i] != null) Destroy(answerUIs[i].gameObject);
            answerUIs.Clear();

            if (answerRoot == null || answerPrefab == null) return;

            foreach (ProbabilityDialogueSession.RuntimeOption o in session.Options)
            {
                ProbabilityAnswerUI ui = Instantiate(answerPrefab, answerRoot);
                ui.Bind(o, ColorOf);
                ui.OnClicked += HandleAnswerClicked;
                answerUIs.Add(ui);
            }
        }

        private void RebuildHand()
        {
            for (int i = 0; i < cardUIs.Count; i++) if (cardUIs[i] != null) Destroy(cardUIs[i].gameObject);
            cardUIs.Clear();

            if (handRoot == null || cardPrefab == null || session == null) return;

            foreach (CardDataExplore c in session.Hand)
            {
                ProbabilityCardUI ui = Instantiate(cardPrefab, handRoot);
                ui.Bind(c);
                ui.OnPlayRequested += HandleCardPlayRequested;
                ui.OnAimChanged += HandleCardAim;
                cardUIs.Add(ui);
            }
        }

        // ==========================================
        private void HandleCardPlayRequested(ProbabilityCardUI ui)
        {
            if (inputLocked || session == null) { ui.ReturnHome(); return; }
            if (!session.PlayCard(ui.Data)) ui.ReturnHome();
        }

        /// <summary>拖曳／指到卡片時，把屬性相符且**還可用**的回答亮起來（規格 §3.1）。</summary>
        private void HandleCardAim(ProbabilityCardUI ui, bool aiming)
        {
            if (session == null) return;

            foreach (ProbabilityAnswerUI a in answerUIs)
            {
                if (a == null || a.Bound == null) continue;

                // 用 Session 同一支判定 —— 亮起來的和真的會加機率的必須是同一批，
                // 各寫一次遲早會不一致
                bool match = aiming
                             && a.Bound.available
                             && ProbabilityCardRules.Affects(ui.Data, a.Bound.source.acceptedAttributes);

                a.SetHighlighted(match);
            }
        }

        private void HandleCardPlayed(
            CardDataExplore card,
            List<ProbabilityDialogueSession.RuntimeOption> targets,
            List<int> before, List<int> after)
        {
            // 亮度收掉
            foreach (ProbabilityAnswerUI a in answerUIs) if (a != null) a.SetHighlighted(false);

            for (int i = 0; i < targets.Count; i++)
            {
                ProbabilityAnswerUI ui = FindUI(targets[i]);
                if (ui != null) ui.AnimateProbability(before[i], after[i]);
            }

            // 規格 R8：沒有影響到任何回答時要講
            if (targets.Count == 0) ShowNoEffect();
        }

        private void ShowNoEffect()
        {
            if (noEffectHint == null) return;
            if (noEffectRoutine != null) StopCoroutine(noEffectRoutine);
            noEffectRoutine = StartCoroutine(NoEffectRoutine());
        }

        private IEnumerator NoEffectRoutine()
        {
            noEffectHint.SetActive(true);
            yield return new WaitForSecondsRealtime(noEffectSeconds);
            noEffectHint.SetActive(false);
            noEffectRoutine = null;
        }

        // ==========================================
        private void HandleAnswerClicked(ProbabilityAnswerUI ui)
        {
            if (inputLocked || session == null || ui.Bound == null) return;

            // 規格 §5.1：選了之後馬上鎖住所有輸入
            inputLocked = true;
            session.SelectOption(ui.Bound);
        }

        private void HandleOptionResolved(ProbabilityDialogueSession.RuntimeOption o, int roll, bool success)
        {
            // ⚠️ **成功時要把 successText 畫出來**（規格 §9.2 第 4 步）。
            //
            // 失敗那條路是 Session 換 CurrentPrompt、透過 OnPromptChanged 通知，
            // 但成功不走那條 —— 所以這裡不寫的話，玩家會看到「按下去沒反應、然後跳走」。
            if (success && promptText != null && o?.source != null
                && !string.IsNullOrEmpty(o.source.successText))
            {
                promptText.text = o.source.successText;
            }

            StartCoroutine(UnlockAfterDisplay());
        }

        private IEnumerator UnlockAfterDisplay()
        {
            yield return new WaitForSecondsRealtime(resolveDisplaySeconds);
            inputLocked = false;
        }

        private void HandleOptionDisabled(ProbabilityDialogueSession.RuntimeOption o)
        {
            ProbabilityAnswerUI ui = FindUI(o);
            if (ui != null) ui.SetDisabled();
        }

        private void HandlePromptChanged(int failureCount, string prompt)
        {
            if (promptText != null) promptText.text = prompt;
        }

        private void HandleEnded(bool success)
        {
            inputLocked = true;
            foreach (ProbabilityCardUI c in cardUIs) if (c != null) c.gameObject.SetActive(false);
        }

        private ProbabilityAnswerUI FindUI(ProbabilityDialogueSession.RuntimeOption o)
        {
            for (int i = 0; i < answerUIs.Count; i++)
                if (answerUIs[i] != null && answerUIs[i].Bound == o) return answerUIs[i];
            return null;
        }
    }
}

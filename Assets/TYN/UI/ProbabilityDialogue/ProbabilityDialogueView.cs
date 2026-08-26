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
    /// 【顏色從哪來】規格用 colorId 字串串起卡牌與回答，但**畫面需要真的顏色**。
    /// 對照表放在這裡（Inspector 可調）—— 不放在卡牌上是因為
    /// 「橘色」應該整個事件一致，而不是每張卡各自宣告一次。
    /// </summary>
    public class ProbabilityDialogueView : MonoBehaviour
    {
        [System.Serializable]
        public class ColorEntry
        {
            [Tooltip("跟卡牌的 Color Id、回答的 Accepted Color Ids 一模一樣")]
            public string colorId = "";
            public Color color = Color.white;
        }

        [Header("顏色對照")]
        [Tooltip("colorId → 實際顏色。**沒登記的 id 會用白色並發警告** ——\n" +
                 "打錯字是這套資料最容易出的錯，而且不吵的話畫面只會「顏色怪怪的」")]
        public List<ColorEntry> colors = new List<ColorEntry>();

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
        public Color ColorOf(string colorId)
        {
            for (int i = 0; i < colors.Count; i++)
                if (colors[i] != null && colors[i].colorId == colorId) return colors[i].color;

            Debug.LogWarning(
                $"[機率對話] 顏色 id「{colorId}」沒有登記在 View 的顏色對照表裡，先用白色。\n" +
                "⚠️ 這通常是資料打錯字 —— 卡牌與回答的 colorId 必須一模一樣。", this);
            return Color.white;
        }

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

            foreach (ProbabilityCardData c in session.Hand)
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

        /// <summary>拖曳／指到卡片時，把同色且**還可用**的回答亮起來（規格 §3.1）。</summary>
        private void HandleCardAim(ProbabilityCardUI ui, bool aiming)
        {
            if (session == null) return;

            foreach (ProbabilityAnswerUI a in answerUIs)
            {
                if (a == null || a.Bound == null) continue;

                bool match = aiming
                             && a.Bound.available
                             && a.Bound.source.acceptedColorIds != null
                             && a.Bound.source.acceptedColorIds.Contains(ui.Data.colorId);

                a.SetHighlighted(match);
            }
        }

        private void HandleCardPlayed(
            ProbabilityCardData card,
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
            // 判定完先讓玩家看到結果，再讓 Session 的後續（換問話／結束）顯示出來。
            // Session 已經算完了，這裡只是節奏
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

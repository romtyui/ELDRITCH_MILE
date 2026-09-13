using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EldritchMile.Core;

namespace EldritchMile.UI
{
    /// <summary>
    /// 戰鬥以外的 HP／SAN 顯示。外觀是從戰鬥那一顆燈（`lamp_Panel/Root`）複製出來的。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼不直接沿用戰鬥的 BattleHUDUI】那一支讀的是 `BattleUnit` 與 `EnergySystem`，
    /// 兩者都只活在戰鬥 Stage 裡。戰鬥以外的數值在 `RunStateManager`，
    /// 我方的唯一入口是 <see cref="PlayerVitals"/>。
    ///
    /// 【哪些環節顯示】由同物件上的 `UIPanel.visibleInStages` 決定（目前只有 Explore）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【只在數值變動時出現】（2026-09-15，應急做法）
    /// 它跟 EXIT 共用右下角，所以平常藏著、EXIT 在；數值一變：
    ///
    ///   EXIT 淡出 → 等 EXIT **完全消失** → 這裡淡入 → **淡入完成才開始跑數字** →
    ///   停一下 → 淡出 → EXIT 淡回來
    ///
    /// 「淡入完成才開始跑數字」是刻意的：要保證玩家看得到數字變動的過程，
    /// 邊淡入邊跑的話，跑最快的前半段剛好是半透明的。
    ///
    /// 過程中又變了一次（例如連吃兩樣）：不重播淡入，從目前顯示的數字接著跑到新的值。
    /// </summary>
    public class VitalsHudUI : MonoBehaviour
    {
        /// <summary>目前啟用中的那一個。EXIT 靠它決定要不要讓位</summary>
        public static VitalsHudUI Active { get; private set; }

        [Header("元件")]
        [Tooltip("HP 條（Image Type = Filled）")]
        public Image hpFill;

        [Tooltip("SAN 條（Image Type = Filled）")]
        public Image sanFill;

        public TMP_Text hpText;
        public TMP_Text sanText;

        [Tooltip("整塊的淡入淡出。留空會在自己身上找")]
        public CanvasGroup group;

        [Header("格式")]
        [Tooltip("{0} = 目前、{1} = 上限")]
        public string hpFormat = "{0}";

        [Tooltip("{0} = 目前、{1} = 上限")]
        public string sanFormat = "{0}";

        [Header("只在數值變動時出現")]
        [Tooltip("勾選：平常隱藏，數值變動時才淡入（讓位給 EXIT）。\n取消：一直顯示")]
        public bool pulseOnChange = true;

        [Tooltip("淡入／淡出各多久（秒）")]
        [Min(0.01f)] public float fadeSeconds = 0.3f;

        [Tooltip("數字從舊值跑到新值要多久（秒）")]
        [Min(0f)] public float countSeconds = 0.8f;

        [Tooltip("跑完之後停多久才淡出（秒）")]
        [Min(0f)] public float holdSeconds = 1.5f;

        /// <summary>現在要佔用右下角嗎（等 EXIT 讓位中也算）</summary>
        public bool WantsScreen { get { return !pulseOnChange || state != State.Hidden; } }

        private enum State { Hidden, WaitingForExit, FadingIn, Counting, Holding, FadingOut }
        private State state = State.Hidden;

        private int targetHp, targetSan, maxHp, maxSan;
        private float shownHp, shownSan, fromHp, fromSan;
        private float timer;

        private void OnEnable()
        {
            Active = this;
            if (group == null) group = GetComponent<CanvasGroup>();

            // 進場時不演 —— 那不是「數值變了」，只是這一站開始顯示而已
            ReadVitals();
            shownHp = targetHp;
            shownSan = targetSan;
            ApplyNumbers();

            state = State.Hidden;
            SetAlpha(pulseOnChange ? 0f : 1f);
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            state = State.Hidden;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            int oldHp = targetHp, oldSan = targetSan, oldMaxHp = maxHp, oldMaxSan = maxSan;
            ReadVitals();
            bool changed = targetHp != oldHp || targetSan != oldSan || maxHp != oldMaxHp || maxSan != oldMaxSan;

            if (!pulseOnChange)
            {
                SetAlpha(1f);
                if (changed) BeginCount();
                if (state == State.Counting) TickCount(dt);
                return;
            }

            if (changed)
            {
                switch (state)
                {
                    case State.Hidden: state = State.WaitingForExit; break;
                    case State.FadingOut: state = State.FadingIn; break;
                    case State.Counting:
                    case State.Holding: BeginCount(); break;
                    // 等待中、淡入中：淡入完成時才開始跑，會自然接到新的值
                }
            }

            switch (state)
            {
                case State.WaitingForExit:
                {
                    SharedExitUI exit = SharedExitUI.Instance;
                    if (exit == null || !exit.isActiveAndEnabled || !exit.IsRequested || exit.IsFullyHidden)
                        state = State.FadingIn;
                    break;
                }
                case State.FadingIn:
                    SetAlpha(Mathf.MoveTowards(Alpha, 1f, dt / fadeSeconds));
                    if (Alpha >= 1f) BeginCount();
                    break;
                case State.Counting:
                    TickCount(dt);
                    break;
                case State.Holding:
                    timer += dt;
                    if (timer >= holdSeconds) state = State.FadingOut;
                    break;
                case State.FadingOut:
                    SetAlpha(Mathf.MoveTowards(Alpha, 0f, dt / fadeSeconds));
                    if (Alpha <= 0f) state = State.Hidden;
                    break;
            }
        }

        private void BeginCount()
        {
            fromHp = shownHp;
            fromSan = shownSan;
            timer = 0f;
            state = State.Counting;
        }

        private void TickCount(float dt)
        {
            timer += dt;
            float t = countSeconds <= 0f ? 1f : Mathf.Clamp01(timer / countSeconds);
            float e = Mathf.SmoothStep(0f, 1f, t);

            shownHp = Mathf.Lerp(fromHp, targetHp, e);
            shownSan = Mathf.Lerp(fromSan, targetSan, e);
            ApplyNumbers();

            if (t < 1f) return;

            state = pulseOnChange ? State.Holding : State.Hidden;
            timer = 0f;
        }

        private void ReadVitals()
        {
            targetHp = PlayerVitals.Hp;
            maxHp = PlayerVitals.MaxHp;
            targetSan = PlayerVitals.San;
            maxSan = PlayerVitals.MaxSan;
        }

        private void ApplyNumbers()
        {
            int hp = Mathf.RoundToInt(shownHp);
            int san = Mathf.RoundToInt(shownSan);

            if (hpFill != null) hpFill.fillAmount = maxHp > 0 ? Mathf.Clamp01(shownHp / maxHp) : 0f;
            if (hpText != null) hpText.text = string.Format(hpFormat, hp, maxHp);
            if (sanFill != null) sanFill.fillAmount = maxSan > 0 ? Mathf.Clamp01(shownSan / maxSan) : 0f;
            if (sanText != null) sanText.text = string.Format(sanFormat, san, maxSan);
        }

        private float Alpha { get { return group != null ? group.alpha : 1f; } }

        private void SetAlpha(float a)
        {
            if (group == null) return;
            group.alpha = a;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}

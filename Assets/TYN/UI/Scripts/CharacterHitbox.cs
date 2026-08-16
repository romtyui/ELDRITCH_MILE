using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace EldritchMile.UI
{
    using EldritchMile.Core;

    /// <summary>
    /// 蓋在「畫在背景圖上的角色」身上的隱形觸發區。
    ///
    /// 【為什麼需要它】商店的店主不是一個獨立的物件，他是背景圖的一部分 ——
    /// 沒有 Collider、沒有 Button，點不到。所以在他身上蓋一塊透明的 Image
    /// 來收 Click。這是 2D 手遊的標準做法（點角色會有反應的那個）。
    ///
    /// 【透明的 Image 收得到點擊，停用的收不到】
    ///   · `color.a = 0` + `raycastTarget = true` → **看不見但點得到**，正是我們要的
    ///   · `image.enabled = false`                → 收不到任何 raycast，而且不會報錯
    /// 這兩件事只差一個字，症狀是「點了完全沒反應」，很難查。所以 Awake 會自己設好。
    ///
    /// 【氣泡錨點也放這裡】點擊區的位置本來就對著角色，順手在上面掛一個
    /// 空的 RectTransform 當氣泡的錨點，兩者永遠一致。
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CharacterHitbox : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("角色")]
        [Tooltip("這塊區域是誰。對應 CharacterDatabase 裡的 id")]
        public string characterId = "";

        [Header("氣泡")]
        [Tooltip("氣泡要指的位置。通常放在頭頂上方一點。\n" +
                 "留空則用本物件自己的位置（那會指在角色正中央，通常太低）")]
        public RectTransform bubbleAnchor;

        [Tooltip("自己在 OnEnable 就說寒暄。\n\n" +
                 "⚠️ **預設關閉，由 Stage 呼叫 Greet()**。原因是 OnEnable 的執行順序不保證 ——\n" +
                 "氣泡與本元件在同一個 prefab 裡，誰先 Awake 沒有定論，\n" +
                 "搶先的話 SpeechBubbleUI.Instance 還是 null，寒暄就靜靜地消失了。\n" +
                 "這個 Stage 沒有在管的獨立角色（例如街上的路人）才打開它")]
        public bool greetOnEnter = false;

        [Tooltip("點擊時輪流講閒聊")]
        public bool chatterOnClick = true;

        [Tooltip("玩家發呆這麼久就自己講一句閒聊。**0 = 不主動講**。\n" +
                 "文件寫的是「在商店頁面待機／點擊商人」—— 待機也算觸發")]
        [Min(0f)] public float idleChatterSeconds = 14f;

        [Tooltip("目前成立的條件（對應 CharacterData.conditionalChatter 的 Condition Id）。\n\n" +
                 "⚠️ 現在是**手填**的。「坎貝爾不在隊伍內」這種條件需要「協助者／隊伍」系統，\n" +
                 "而那個系統還不存在。等它做好，這個清單改由程式填")]
        public System.Collections.Generic.List<string> activeConditions = new System.Collections.Generic.List<string>();

        [Header("回饋")]
        [Tooltip("滑鼠移上去時的提示。可留空")]
        public GameObject hoverHighlight;

        /// 被點了。Stage 想接管點擊行為時訂閱這個。
        public event Action<CharacterHitbox> OnClicked;

        public CharacterData Character => GameFlowManager.Character(characterId);

        /// <summary>氣泡該指的位置。沒設錨點就是自己。</summary>
        public Transform Anchor => bubbleAnchor != null ? (Transform)bubbleAnchor : transform;

        private int chatterIndex;
        private System.Collections.Generic.List<string> chatterPool;
        private float nextIdleChatterAt;

        /// <summary>
        /// 重新組閒聊池。條件變了（例如坎貝爾離隊）之後要呼叫一次。
        ///
        /// 池子存起來而不是每次現查 —— 玩家點第 5 下時池子必須跟第 1 下一樣，
        /// 否則中途條件變了會讓輪播跳號、重複或漏句。
        /// </summary>
        public void RebuildChatterPool()
        {
            CharacterData c = Character;
            chatterPool = c != null ? c.BuildChatterPool(activeConditions) : null;
            chatterIndex = 0;
        }

        private void Awake()
        {
            var image = GetComponent<Image>();

            // 看不見但點得到。**不要改成 enabled = false**（見類別說明）
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            image.raycastTarget = true;

            if (hoverHighlight != null) hoverHighlight.SetActive(false);
        }

        private void OnEnable()
        {
            RebuildChatterPool();
            PostponeIdleChatter();

            if (greetOnEnter) Greet();
        }

        private void Update()
        {
            if (idleChatterSeconds <= 0f || nextIdleChatterAt <= 0f) return;
            if (Time.unscaledTime < nextIdleChatterAt) return;

            SayNextChatter();
        }

        /// <summary>把「發呆多久才自言自語」的計時重新推遲。每次講話都要呼叫。</summary>
        private void PostponeIdleChatter()
        {
            nextIdleChatterAt = idleChatterSeconds > 0f
                ? Time.unscaledTime + idleChatterSeconds
                : 0f;
        }

        /// <summary>講下一句閒聊。點擊與待機共用同一個輪播索引，不會各輪各的。</summary>
        public void SayNextChatter()
        {
            if (chatterPool == null) RebuildChatterPool();
            if (chatterPool == null || chatterPool.Count == 0)
            {
                // 沒有閒聊可講的角色不該每幀都來報到
                nextIdleChatterAt = 0f;
                return;
            }

            int i = chatterIndex % chatterPool.Count;
            chatterIndex++;

            Say(chatterPool[i]);
        }

        /// <summary>說一句進場寒暄。沒有角色資料就靜靜地不做事。</summary>
        public void Greet()
        {
            CharacterData c = Character;
            if (c == null) return;

            RunContext run = GameFlowManager.Instance != null ? GameFlowManager.Instance.Run : null;
            var rng = new System.Random(run != null ? run.runSeed : Environment.TickCount);

            string line = c.PickGreeting(rng);
            if (string.IsNullOrEmpty(line)) return;

            Say(line);
        }

        /// <summary>讓這個角色說一句話。氣泡會指到他頭上。</summary>
        public void Say(string line)
        {
            PostponeIdleChatter();   // 剛講完話就不算發呆

            SpeechBubbleUI bubble = SpeechBubbleUI.Instance;
            if (bubble == null)
            {
                Debug.LogWarning($"[角色] 場上沒有 SpeechBubbleUI，「{characterId}」的話無處可說：{line}");
                return;
            }

            CharacterData c = Character;
            bubble.Show(line, Anchor, c != null ? c.Label : characterId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;

            OnClicked?.Invoke(this);

            if (chatterOnClick) SayNextChatter();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverHighlight != null) hoverHighlight.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (hoverHighlight != null) hoverHighlight.SetActive(false);
        }
    }
}

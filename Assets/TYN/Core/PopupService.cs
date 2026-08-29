using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 跨 Stage 共用的訊息服務。取代封存的 UIManager。
    ///
    /// 【職責】只管「什麼時候顯示什麼訊息」與「排隊避免蓋來蓋去」，
    /// 實際畫面交給 DialogueBoxUI —— 探索、商店、對話因此共用同一個對話框，
    /// 不必每個 Stage 各做一份面板。
    ///
    /// 【為什麼要排隊】玩家開箱子拿到東西的同時，房間可能剛好清空要跳總結。
    /// 沒有排隊的話後者會直接蓋掉前者。
    /// </summary>
    public class PopupService : MonoBehaviour
    {
        public static PopupService Instance { get; private set; }

        [Header("顯示載體")]
        [Tooltip("共用對話框。所有訊息都透過它顯示")]
        public DialogueBoxUI dialogueBox;

        /// 所有訊息都播完、對話框關閉時觸發。
        /// C13 的「要探索其他的東西嗎？」等這個訊號才跳出來。
        public event Action OnAllClosed;

        private readonly Queue<PendingMessage> pending = new Queue<PendingMessage>();
        private bool closeAfterDrain;

        [Header("自動推進")]
        [Tooltip("結算類訊息（獲得道具等）自動推進的停留秒數。\n" +
                 "**預設 0 = 等玩家點擊**，因為玩家通常想自己控制看完的節奏。\n" +
                 "留這個欄位是給長文本用的 —— 一大串內容要玩家連點很多下才煩人，" +
                 "那種情況再調成 1.5~2.5 秒")]
        public float settlementAutoAdvance = 0f;

        private struct PendingMessage
        {
            public string body;
            public string speaker;      // null = 系統提示
            public Sprite portrait;
            public float autoAdvance;   // >0 = 這則自動推進
        }

        public bool IsAnyOpen => dialogueBox != null && dialogueBox.IsShowing;

        /// <summary>還有沒有排隊等著播的訊息。</summary>
        public bool HasPending => pending.Count > 0;

        /// <summary>
        /// 現在是不是「話都講完了」——沒有排隊、也沒有在打字。
        ///
        /// 【為什麼不用 OnAllClosed 判斷】那個事件是在 `Drain()` 發現佇列空了才發，
        /// 而 `Drain()` 只有玩家**點擊推進**時才會被呼叫。
        /// 想在「最後一句打完的當下」做事（例如顯示結束鍵），用這個。
        /// </summary>
        public bool IsIdle => !HasPending && (dialogueBox == null || !dialogueBox.IsTyping);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (dialogueBox != null) dialogueBox.OnAdvanced += HandleAdvanced;
        }

        private void OnDisable()
        {
            if (dialogueBox != null) dialogueBox.OnAdvanced -= HandleAdvanced;
        }

        // ==========================================
        // 對外 API
        // ==========================================

        /// <summary>系統提示。若已有訊息在顯示則排隊。</summary>
        public void ShowText(string content)
        {
            Enqueue(new PendingMessage { body = content, speaker = null });
        }

        /// <summary>角色說話。</summary>
        public void ShowSpeech(string speaker, string content, Sprite portrait = null)
        {
            Enqueue(new PendingMessage { body = content, speaker = speaker, portrait = portrait });
        }

        /// <summary>
        /// 系統提示，但借用立繪位置顯示一張特寫圖。
        /// 用於「你正在處理這個東西」的情境 —— 開箱、檢查物件、與 NPC 周旋。
        /// </summary>
        public void ShowSystemWithCloseUp(string content, Sprite closeUp)
        {
            Enqueue(new PendingMessage { body = content, speaker = null, portrait = closeUp });
        }

        /// <summary>
        /// 等目前排隊的訊息都播完再關閉對話框。
        ///
        /// 【為什麼不直接 CloseAll】玩家可能正在讀最後一句。直接關掉會把話截斷，
        /// 但放著不管又會留下一個沒人負責關的框。折衷是「播完就收」。
        /// </summary>
        public void CloseWhenDrained()
        {
            if (!IsAnyOpen && pending.Count == 0)
            {
                dialogueBox?.Hide();
                return;
            }

            closeAfterDrain = true;
        }

        /// <summary>
        /// 開啟容器 —— 走系統提示公版。
        /// 格式（「打開了 X。」「獲得了：…」）在 DialogueBoxUI 的 Inspector 調，不寫死在程式裡。
        /// </summary>
        public void ShowLoot(string containerName, IReadOnlyList<string> items)
        {
            if (dialogueBox == null)
            {
                Warn($"開啟 {containerName}");
                return;
            }

            // 開箱結果屬於結算內容 —— 自動推進，玩家不必再點
            Enqueue(new PendingMessage
            {
                body = BuildLootText(containerName, items),
                speaker = null,
                autoAdvance = settlementAutoAdvance,
            });
        }

        /// <summary>與 ShowText 相同，保留舊名稱以示語意：這則訊息不急，排在後面。</summary>
        public void QueueText(string content) => ShowText(content);

        /// <summary>
        /// 【即時替換，不排隊、不需點擊】把對話框正文直接換掉。
        ///
        /// 用於打牌環節的判定結果 —— C18③ 要求「即時反應」。
        /// 若走一般的排隊流程，玩家每出一張牌就得點掉一則訊息才能出下一張，
        /// 而 C18 的設計是連續嘗試，那樣會被打斷得很嚴重。
        /// </summary>
        public void ShowInstant(string content)
        {
            if (dialogueBox == null)
            {
                Warn(content);
                return;
            }

            // 蓋掉排隊中的內容 —— 判定結果永遠是當下最該看到的東西
            pending.Clear();
            dialogueBox.ShowSystem(content);
        }

        /// <summary>
        /// 即時替換，但**帶說話者**（有名字框）。
        /// 事件的結果文字裡「半魚人：…」那種段落用這一支。
        /// </summary>
        public void ShowSpeechInstant(string speaker, string content)
        {
            if (dialogueBox == null)
            {
                Warn(content);
                return;
            }

            pending.Clear();
            dialogueBox.ShowSpeech(speaker, content);
        }

        public void CloseAll()
        {
            pending.Clear();
            if (dialogueBox != null) dialogueBox.Hide();
        }

        // ==========================================
        // 排隊
        // ==========================================

        private void Enqueue(PendingMessage msg)
        {
            if (string.IsNullOrEmpty(msg.body)) return;

            if (dialogueBox == null)
            {
                Warn(msg.body);
                return;
            }

            pending.Enqueue(msg);
            if (!IsAnyOpen) Drain();
        }

        private void HandleAdvanced()
        {
            Drain();
        }

        private void Drain()
        {
            if (pending.Count > 0)
            {
                PendingMessage msg = pending.Dequeue();

                if (string.IsNullOrEmpty(msg.speaker))
                {
                    // 系統提示也可能帶特寫圖（借立繪位置）
                    if (msg.portrait != null) dialogueBox.ShowSystem(msg.body, msg.portrait);
                    else dialogueBox.ShowSystem(msg.body);
                }
                else
                {
                    dialogueBox.ShowSpeech(msg.speaker, msg.body, msg.portrait);
                }

                dialogueBox.ScheduleAutoAdvance(msg.autoAdvance);
                return;
            }

            if (closeAfterDrain)
            {
                closeAfterDrain = false;
                dialogueBox?.Hide();
            }

            OnAllClosed?.Invoke();
        }

        private string BuildLootText(string containerName, IReadOnlyList<string> items)
        {
            if (items == null || items.Count == 0)
            {
                return string.Format(dialogueBox.emptyContainerFormat, containerName);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format(dialogueBox.containerOpenedFormat, containerName));
            for (int i = 0; i < items.Count; i++)
            {
                sb.AppendLine(string.Format(dialogueBox.itemLineFormat, items[i]));
            }
            return sb.ToString().TrimEnd();
        }

        private static bool warnedOnce;

        private static void Warn(string content)
        {
            if (!warnedOnce)
            {
                warnedOnce = true;
                Debug.LogError(
                    "[訊息] PopupService 沒有指定 Dialogue Box —— 所有訊息都不會顯示。\n" +
                    "請在 Inspector 把場景裡的 DialogueBoxUI 拖上去。",
                    Instance
                );
            }
            Debug.Log($"[訊息] {content}");
        }
    }
}

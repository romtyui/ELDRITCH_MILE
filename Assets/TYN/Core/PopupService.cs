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

        private struct PendingMessage
        {
            public string body;
            public string speaker;      // null = 系統提示
            public Sprite portrait;
        }

        public bool IsAnyOpen => dialogueBox != null && dialogueBox.IsShowing;

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

            if (IsAnyOpen)
            {
                // 已有訊息在顯示，轉成一般文字排隊（格式先組好）
                Enqueue(new PendingMessage { body = BuildLootText(containerName, items), speaker = null });
                return;
            }

            dialogueBox.ShowContainerOpened(containerName, items);
        }

        /// <summary>與 ShowText 相同，保留舊名稱以示語意：這則訊息不急，排在後面。</summary>
        public void QueueText(string content) => ShowText(content);

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
                    dialogueBox.ShowSystem(msg.body);
                }
                else
                {
                    dialogueBox.ShowSpeech(msg.speaker, msg.body, msg.portrait);
                }
                return;
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("彈出文本 UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI popupText;
    public Button closePopupButton;

    [Header("容器拾取 (Loot) UI")]
    public GameObject lootPanel;
    public TextMeshProUGUI lootTitleText;
    public TextMeshProUGUI lootItemsText; // 暫時用一個大文字框顯示所有道具
    public Button takeAllButton;

    // 用來暫存「房間探索完成」的文本
    private string pendingRoomClearText = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        popupPanel.SetActive(false);
        lootPanel.SetActive(false);

        if (closePopupButton != null) closePopupButton.onClick.AddListener(HidePopupText);
        if (takeAllButton != null) takeAllButton.onClick.AddListener(HideLootUI);
    }

    // --- 文本彈窗邏輯 ---
    public void ShowPopupText(string content)
    {
        popupText.text = content;
        popupPanel.SetActive(true);
    }

    public void HidePopupText()
    {
        popupPanel.SetActive(false);
        CheckPendingClearText(); // 關閉視窗後，檢查有沒有排隊中的房間總結
    }

    // --- 容器 Loot 邏輯 ---
    public void ShowLootUI(string containerName, List<string> items)
    {
        lootTitleText.text = $"開啟了: {containerName}";
        
        // MVP: 將 List 轉為換行的字串顯示
        string combinedItems = "";
        foreach(var item in items) { combinedItems += $"- {item}\n"; }
        lootItemsText.text = string.IsNullOrEmpty(combinedItems) ? "裡面空空如也..." : combinedItems;

        lootPanel.SetActive(true);
    }

    public void HideLootUI()
    {
        // 這裡可以觸發實際將道具加入玩家背包的程式碼
        lootPanel.SetActive(false);
        CheckPendingClearText(); // 關閉 Loot 後，檢查有沒有排隊中的房間總結
    }

    // --- 排隊系統 (避免 UI 重疊) ---
    public void QueueRoomClearText(string text)
    {
        pendingRoomClearText = text;
        
        // 如果當前沒有任何 UI 開著，直接彈出；否則等玩家關閉當前 UI 後觸發
        if (!popupPanel.activeSelf && !lootPanel.activeSelf)
        {
            CheckPendingClearText();
        }
    }

    private void CheckPendingClearText()
    {
        if (!string.IsNullOrEmpty(pendingRoomClearText))
        {
            string txt = pendingRoomClearText;
            pendingRoomClearText = ""; // 顯示前清空，避免無限循環
            ShowPopupText($"【探索完成】\n{txt}");
        }
    }
}
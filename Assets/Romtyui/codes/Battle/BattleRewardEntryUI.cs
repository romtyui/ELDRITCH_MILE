using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardEntryUI : MonoBehaviour
{
    [Tooltip("控制整欄透明度與互動。")]
    public CanvasGroup rowGroup;

    [Tooltip("reward_name_text。")]
    public TMP_Text rewardNameText;

    [Tooltip("number_text。")]
    public TMP_Text numberText;

    [Tooltip("Icon。")]
    public Image iconImage;

    [Tooltip("Button 元件。")]
    public Button actionButton;

    [Tooltip("Button 的 Image。")]
    public Image buttonImage;

    [Tooltip("Button 下的 Text (TMP)。")]
    public TMP_Text buttonText;

    [Tooltip("金幣與遺物按鈕使用的 a_IMAGE。")]
    public Sprite receiveButtonSprite;

    [Tooltip("武器按鈕使用的 B_IMAGE。")]
    public Sprite chooseButtonSprite;

    [Tooltip("藥水或食物欄位已滿時使用的按鈕圖片。")]
    public Sprite fullButtonSprite;

    [Tooltip("藥水或食物欄位已滿時顯示的文字。")]
    public string fullButtonText = "裝備已滿";

    private bool isWeapon;
    private bool isFull;
    private bool isCompleted;

    [Range(0f, 1f)]
    [Tooltip("已領取或放棄後的整欄透明度。")]
    public float completedAlpha = 0.35f;

    private BattleRewardUI owner;
    private int index;

    public void Bind(BattleRewardUI rewardUI, int rewardIndex, string rewardName, string amount, Sprite icon, bool weapon)
    {
        owner = rewardUI;
        index = rewardIndex;
        isWeapon = weapon;
        isFull = false;
        isCompleted = false;

        if (rowGroup == null) rowGroup = GetComponent<CanvasGroup>();
        if (rowGroup == null) rowGroup = gameObject.AddComponent<CanvasGroup>();

        if (rewardNameText != null) rewardNameText.text = rewardName;
        if (numberText != null) numberText.text = amount;
        if (buttonText != null) buttonText.text = weapon ? "選擇" : "收下";

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (buttonImage != null) buttonImage.sprite = weapon ? chooseButtonSprite : receiveButtonSprite;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnClicked);
            actionButton.onClick.AddListener(OnClicked);
            actionButton.interactable = true;
        }

        rowGroup.alpha = 1f;
        rowGroup.interactable = true;
        rowGroup.blocksRaycasts = true;
    }

    public void SetFull(bool full)
    {
        isFull = full;
        if (isCompleted) return;

        if (buttonText != null) buttonText.text = isFull ? fullButtonText : (isWeapon ? "選擇" : "收下");
        if (buttonImage != null) buttonImage.sprite = isFull ? fullButtonSprite : (isWeapon ? chooseButtonSprite : receiveButtonSprite);
        if (actionButton != null) actionButton.interactable = !isFull;
    }

    public void SetCompleted()
    {
        isCompleted = true;

        if (rowGroup != null)
        {
            rowGroup.alpha = completedAlpha;
            rowGroup.interactable = false;
            rowGroup.blocksRaycasts = false;
        }

        if (actionButton != null) actionButton.interactable = false;
    }

    private void OnClicked()
    {
        if (owner == null || isFull || isCompleted) return;
        owner.ClickReward(index);
    }
}
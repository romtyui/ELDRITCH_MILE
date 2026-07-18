using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // 為了使用預設 Text
using TMPro; // 為了使用 TextMeshPro

[RequireComponent(typeof(ExplorationInteractableTarget))]
public class DialogueOptionInteractable : MonoBehaviour, ICardInteractable
{
    [Header("對話選項設定")]
    public string optionTitle = "選項名稱";
    
    [Tooltip("此選項的無形難度倍率。1.0=機率不變, 1.5=變容易, 0.5=變困難")]
    public float hiddenMultiplier = 1.0f;

    [Header("文字變更設定 (自動抓取)")]
    [Tooltip("檢定成功後要顯示的文字")]
    public string successText = "【檢定成功】";
    [Tooltip("檢定失敗後要顯示的文字")]
    public string failText = "【檢定失敗】";

    [Header("結算事件 (可直接在 Inspector 拖曳指定)")]
    public UnityEvent OnSuccess;
    public UnityEvent OnFail;

    private bool hasResolved = false;

    // 用來儲存抓到的文字組件
    private Text legacyText;
    private TextMeshProUGUI tmpText;

    private void Awake()
    {
        // 自動在自身或子物件中尋找文字組件
        legacyText = GetComponentInChildren<Text>();
        tmpText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public bool OnCardPlayed(float baseProbability)
    {
        if (hasResolved) 
        {
            Debug.Log($"選項 [{optionTitle}] 已經結算過了！");
            return false;
        }

        // 1. 計算最終機率 (基礎機率 x 隱藏倍率)，並限制在 0% ~ 100% 之間
        float finalProbability = Mathf.Clamp01(baseProbability * hiddenMultiplier);
        
        Debug.Log($"[對話檢定] 投入卡牌機率: {baseProbability*100}% | 選項倍率: x{hiddenMultiplier} | 最終機率: {finalProbability*100}%");

        // 2. 擲骰子判定 (產生 0.0 ~ 1.0 的隨機數)
        float roll = Random.value;

        if (roll <= finalProbability)
        {
            // 成功
            Debug.Log($"<color=green>【{optionTitle}】檢定成功！</color> (骰出 {roll:F2} <= 目標 {finalProbability:F2})");
            if (UIManager.Instance != null) UIManager.Instance.ShowPopupText($"「{optionTitle}」大成功！");
            
            // 自動變更按鈕上的文字
            UpdateOptionText(successText, Color.green);

            OnSuccess?.Invoke();
            hasResolved = true;
            return true;
        }
        else
        {
            // 失敗
            Debug.Log($"<color=red>【{optionTitle}】檢定失敗！</color> (骰出 {roll:F2} > 目標 {finalProbability:F2})");
            if (UIManager.Instance != null) UIManager.Instance.ShowPopupText($"「{optionTitle}」搞砸了...");
            
            // 自動變更按鈕上的文字
            UpdateOptionText(failText, Color.red);

            OnFail?.Invoke();
            hasResolved = true;
            return false;
        }
    }

    // 輔助方法：變更文字內容與顏色
    private void UpdateOptionText(string newText, Color newColor)
    {
        if (tmpText != null)
        {
            tmpText.text = newText;
            tmpText.color = newColor;
        }
        else if (legacyText != null)
        {
            legacyText.text = newText;
            legacyText.color = newColor;
        }
    }

    // 讓其他腳本（或 UnityEvent）可以重置此選項
    public void ResetOption()
    {
        hasResolved = false;
        
        // 恢復原本的文字 (可選：如果你希望重置後變回原本的名字)
        UpdateOptionText(optionTitle, Color.black); // 預設顏色可以自己改
    }
}
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapBannerUI : MonoBehaviour
{
    [Header("UI 元件")]
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;
    public Button backToMenuButton; // 回主選單的按鈕

    [Header("動畫設定")]
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.2f;
    public float mapTitleHoldTime = 1.0f; // 地圖文字停留時間

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        // 確保一開始按鈕是隱藏的
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
        
        HideImmediately();
    }

    public void HideImmediately()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // 顯示一般地圖標題 (顯示後自動消失)
    public IEnumerator ShowMapTitle(string title)
    {
        gameObject.SetActive(true);
        if (backToMenuButton != null) backToMenuButton.gameObject.SetActive(false);
        
        messageText.text = title;

        yield return FadeTo(1f, fadeInDuration);
        yield return new WaitForSeconds(mapTitleHoldTime);
        yield return FadeTo(0f, fadeOutDuration);
        
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 顯示訊息並停留在畫面上（不自動消失）。
    ///
    /// 【Phase 3 新增】取代 ShowEndGame(message, menuSceneName) ——
    /// 舊版在一個「顯示橫幅」的 UI 元件裡面直接呼叫 SceneManager.LoadScene，
    /// 等於讓 UI 元件擅自做流程決策。現在回主選單由 GameFlowManager 負責，
    /// 按鈕要接什麼由呼叫端決定。
    /// </summary>
    public IEnumerator ShowHeld(string message, System.Action onButtonClicked = null)
    {
        gameObject.SetActive(true);
        messageText.text = message;

        yield return FadeTo(1f, fadeInDuration);

        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(onButtonClicked != null);
            backToMenuButton.onClick.RemoveAllListeners();

            if (onButtonClicked != null)
            {
                backToMenuButton.onClick.AddListener(() => onButtonClicked());
            }
        }
    }

    // [已棄用] 舊版：UI 元件自行載入場景。改用 ShowHeld()。
    // 待舊的 PerspectiveMapGenerator 封存後即可刪除。
    public IEnumerator ShowEndGame(string message, string menuSceneName)
    {
        gameObject.SetActive(true);
        messageText.text = message;

        yield return FadeTo(1f, fadeInDuration);

        // 動畫跑完後，顯示按鈕並綁定事件
        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(true);
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene(menuSceneName));
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}
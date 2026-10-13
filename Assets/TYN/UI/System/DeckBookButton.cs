using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>
    /// 掛在任何一顆 BOOK 按鈕上，按下去就打開全域卡冊（<see cref="DeckBookUI"/>）。
    ///
    /// 【為什麼是獨立元件，不是 ShopStageController 的欄位】
    /// 組員說過「之後應該到處都能打開那本書」。做成元件的話，下一個要放 BOOK 的地方
    /// 只要把這個拖上去，不用每個 Stage 各寫一次接線。
    ///
    /// 戰鬥裡那本是 Romtyui 自己的（左下角包包），不經過這裡。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DeckBookButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OpenBook);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OpenBook);
        }

        private void OpenBook()
        {
            if (DeckBookUI.Instance == null)
            {
                Debug.LogWarning("[卡冊] 場景裡沒有 DeckBookUI（EventScene 的 [UI_ROOT]/DeckBook），BOOK 按了不會有事", this);
                return;
            }

            DeckBookUI.Instance.Open();
        }
    }
}

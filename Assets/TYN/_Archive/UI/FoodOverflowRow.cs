using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EldritchMile.UI
{
    /// <summary>食物欄滿了的選擇視窗裡的一列：一個食物，加上「吃掉」「丟掉」。</summary>
    public class FoodOverflowRow : MonoBehaviour
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;

        [Tooltip("新拿到的那一個才顯示")]
        public GameObject newBadge;

        [Tooltip("只有舊的能吃 —— 新的那個還沒到手")]
        public Button eatButton;

        public Button discardButton;
    }
}

using UnityEngine;

namespace EldritchMile.Explore
{
    /// <summary>
    /// 房間內的「預定位子」。C6：
    /// 「寶箱會以各種角度[隨機]出現在預定的位子(室內室外皆有)」
    ///
    /// 【與舊版的差別】封存的 RoomController 是直接 GetComponentsInChildren 掃
    /// 已經擺好的物件 —— 完全靜態，每次進同一個房間看到的東西一模一樣。
    /// 改成 slot 之後，位置是設計好的，內容與角度是隨機的。
    /// </summary>
    public class SpawnSlot : MonoBehaviour
    {
        public enum Placement
        {
            Any,
            Indoor,
            Outdoor,
        }

        [Header("位置屬性")]
        public Placement placement = Placement.Any;

        [Tooltip("只有標記相同 tag 的內容才會放進這個位子。留空 = 不限")]
        public string contentTag = "";

        [Header("隨機角度（C6：以各種角度出現）")]
        [Tooltip("生成物繞 Z 軸的隨機旋轉範圍（度）")]
        public Vector2 rotationRange = new Vector2(-12f, 12f);

        [Tooltip("隨機縮放範圍。1,1 表示不縮放")]
        public Vector2 scaleRange = new Vector2(1f, 1f);

        [Header("機率")]
        [Tooltip("這個位子有東西的機率。1 = 一定有")]
        [Range(0f, 1f)] public float fillChance = 1f;

        /// 執行時由 RoomController 設定
        public bool IsOccupied { get; private set; }

        public void MarkOccupied() => IsOccupied = true;

        public bool Accepts(Placement contentPlacement, string tag)
        {
            if (placement != Placement.Any &&
                contentPlacement != Placement.Any &&
                placement != contentPlacement)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(contentTag) && contentTag != tag) return false;

            return true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.green : new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.4f);
        }
    }
}

using UnityEngine;

namespace EldritchMile.Map3D
{
    /// <summary>
    /// 地圖相機的支架：用「繞著地圖中心的球座標」擺相機，而不是手動填座標。
    ///
    /// 【為什麼要這一層】
    /// 直接調 Transform 的話，改俯角就要重算位置、改距離又要重算一次，
    /// 而且旋轉之後就回不去了。改成 yaw / pitch / distance 三個數字之後，
    /// 「轉一點」「拉遠一點」各自獨立，調起來才有辦法收斂。
    ///
    /// 階段 2（縮放平移）與階段 3（旋轉）都直接動這三個數字就好，
    /// 不必再碰 Transform —— 這一層就是為那兩階段準備的。
    ///
    /// 【yaw 為什麼預設 45】
    /// 地圖是正方形。相機正對邊的話畫面上是梯形；轉 45° 讓「角」朝向鏡頭，
    /// 才會是美術稿那個菱形。平面版也是靠把圖轉 45° 做出同一件事。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class MapCameraRig : MonoBehaviour
    {
        [Header("看著誰")]
        [Tooltip("環繞的中心。留空則用世界原點加上下面的偏移")]
        public Transform pivot;

        [Tooltip("在 pivot 之上的偏移。地圖是平的，通常抬一點點就好")]
        public Vector3 pivotOffset = Vector3.zero;

        [Header("角度")]
        [Tooltip("水平旋轉。45 = 地圖的角朝向鏡頭（美術稿那個菱形）")]
        [Range(-180f, 180f)] public float yaw = 45f;

        [Tooltip("俯角。0 = 貼著地面平視，90 = 正上方垂直俯視")]
        [Range(5f, 89f)] public float pitch = 45f;

        [Tooltip("與中心的距離")]
        [Min(1f)] public float distance = 18f;

        [Header("視野")]
        [Tooltip("越小透視越弱、越接近等角。太大會讓地圖邊緣變形")]
        [Range(10f, 70f)] public float fieldOfView = 32f;

        [Header("夾住的範圍（階段 3 會用到）")]
        [Tooltip("左右可以轉多少度。地圖有推進方向，能轉 360 度玩家會迷路")]
        [Range(0f, 180f)] public float yawLimit = 40f;

        [Tooltip("yaw 的中心。夾住的範圍是 yawCenter 正負 yawLimit")]
        [Range(-180f, 180f)] public float yawCenter = 45f;

        public Vector2 pitchLimit = new Vector2(25f, 70f);
        public Vector2 distanceLimit = new Vector2(8f, 30f);

        private Camera cam;

        private void LateUpdate() { Apply(); }
        private void OnValidate() { Apply(); }

        /// <summary>把三個數字套到 Transform 上。夾住範圍也在這裡做。</summary>
        public void Apply()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            yaw = Mathf.Clamp(yaw, yawCenter - yawLimit, yawCenter + yawLimit);
            pitch = Mathf.Clamp(pitch, pitchLimit.x, pitchLimit.y);
            distance = Mathf.Clamp(distance, distanceLimit.x, distanceLimit.y);

            Vector3 centre = (pivot != null ? pivot.position : Vector3.zero) + pivotOffset;

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = centre - rot * Vector3.forward * distance;
            transform.rotation = rot;

            cam.fieldOfView = fieldOfView;
        }
    }
}

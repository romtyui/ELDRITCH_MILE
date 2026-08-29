using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一個 Stage 進場時墊在後面的場景美術。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼不直接放進 Stage 的 prefab】
    /// 對話與事件的 Stage 都住在 **Canvas** 底下，而背景是 SpriteRenderer（世界空間）——
    /// 塞進 RectTransform 會被父層的縮放整個扭掉。
    /// 房間美術本來就是掛在 `WorldRoot` 上的，跟著它才對得起來。
    ///
    /// 【為什麼 y 要是 1】
    /// `Room_Village_*` 的 prefab 根 localPosition 是 **(0, 1, 0)**，
    /// 剛好等於相機中心（正交、size 5、位在 y = 1）。
    ///
    /// 擺在 y = 0 的話圖的上緣只到 5.08，而相機看到 6.00 ——
    /// 畫面最上面就會露出一條天空色的空白（1080p 約 78px）。
    /// **房間之所以沒有空白，是因為它整個往上擺了 1**，不是因為縮放不同。
    /// 這一條查了很久，不要再改掉。
    /// </summary>
    public class StageBackdrop : MonoBehaviour
    {
        [Tooltip("要生成的場景美術（例如 `Art_Village_Outdoor`）。\n" +
                 "留空 = 不生成，畫面就沿用上一站留下的東西（會看到底色）")]
        public GameObject prefab;

        [Tooltip("掛在哪個世界空間的父物件底下。留空會去找名為 WorldRoot 的物件")]
        public string parentName = "WorldRoot";

        [Tooltip("擺在哪。**預設 (0,1,0) 就是房間 prefab 的擺法**，不要隨便改 ——\n" +
                 "那個 y = 1 是相機中心，少了它畫面上緣會露出底色（見類別說明）")]
        public Vector3 offset = new Vector3(0f, 1f, 0f);

        [Tooltip("縮放。房間 prefab 裡的美術都是 0.5")]
        [Min(0.01f)] public float scale = 0.5f;

        [Tooltip("放好之後檢查有沒有蓋滿相機，沒蓋滿就在 Console 提醒。\n\n" +
                 "**只是提醒，不會自己動位置** —— 取景是美術調過的\n" +
                 "（見 commit「房間美術重建：對齊美術原場景的實際畫面」），\n" +
                 "程式自作主張置中會把那份調校蓋掉")]
        public bool warnIfNotCovering = true;

        /// 這一站生成出來的那一個。離開時要收掉，不然會一路留到下一站
        private GameObject spawned;

        /// <summary>進場時呼叫。重複呼叫會先收掉舊的。</summary>
        public void Spawn()
        {
            Despawn();
            if (prefab == null) return;

            Transform parent = null;
            if (!string.IsNullOrEmpty(parentName))
            {
                GameObject host = GameObject.Find(parentName);
                if (host != null) parent = host.transform;
                else Debug.LogWarning(
                    $"[背景] 找不到「{parentName}」，改掛在場景根上。\n" +
                    "WorldRoot 是原點且沒有縮放，所以位置會一樣 —— 但層級會比較亂。", this);
            }

            spawned = Instantiate(prefab, parent);
            spawned.transform.localPosition = offset;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.transform.localScale = new Vector3(scale, scale, 1f);

            if (warnIfNotCovering) WarnIfNotCovering();
        }

        /// <summary>離場時呼叫。**一定要呼叫** —— 不收的話背景會留到下一站。</summary>
        public void Despawn()
        {
            if (spawned == null) return;

            Destroy(spawned);
            spawned = null;
        }

        private void OnDisable() => Despawn();

        // ==========================================
        private void WarnIfNotCovering()
        {
            Camera cam = Camera.main;
            if (cam == null || spawned == null || !cam.orthographic) return;

            // 取面積最大的那個 renderer 當「背景本體」——
            // 美術 prefab 底下還有一堆前景小物件，拿它們算會誤判
            SpriteRenderer bg = null;
            float best = 0f;

            SpriteRenderer[] all = spawned.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].sprite == null) continue;

                float area = all[i].bounds.size.x * all[i].bounds.size.y;
                if (area <= best) continue;

                best = area;
                bg = all[i];
            }

            if (bg == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;

            Bounds b = bg.bounds;
            bool covers = b.min.x <= c.x - halfW && b.max.x >= c.x + halfW
                       && b.min.y <= c.y - halfH && b.max.y >= c.y + halfH;

            if (covers) return;

            Debug.LogWarning(
                $"[背景]「{spawned.name}」蓋不滿相機，畫面邊緣會露出底色。\n" +
                $"　背景 x {b.min.x:0.00}~{b.max.x:0.00}　y {b.min.y:0.00}~{b.max.y:0.00}\n" +
                $"　相機 x {c.x - halfW:0.00}~{c.x + halfW:0.00}　y {c.y - halfH:0.00}~{c.y + halfH:0.00}\n" +
                "　Offset 預設 (0,1,0) 是照房間 prefab 的擺法；換了不同比例的圖要重調。", this);
        }
    }
}

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace EldritchMile.UI
{
    /// <summary>
    /// 文字特效標註（2026-11-23 文案組需求：個別詞句像恐怖遊戲那樣抖動）。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【文案怎麼寫】在任何會顯示出來的文字裡（事件內文、對話、選項、道具說明……）直接標：
    ///
    ///     他的聲音<shake>不像人類</shake>。
    ///     <shake=2>快逃</shake>          ← =後面是強度，預設 1
    ///     <wave>海浪在呼喚你</wave>       ← 上下起伏
    ///     <flicker>那盞燈</flicker>       ← 忽明忽滅
    ///
    /// 中文標籤也可以：<抖>…</抖>、<波>…</波>、<閃>…</閃>（同上，可加 =強度）。
    /// 可以跟 TMP 原本的標籤混用（<color>、<b>、<size>…），**但特效之間不能互相包**
    /// （<shake><wave>…</wave></shake> 只會留外面那一層）—— 底層借的是 TMP 的 <link>，它不能巢狀。
    ///
    /// 【不用另外掛】只要文字裡出現上面的標籤，這支會自己掛到那個 TMP 上（見 <see cref="Bootstrap"/>），
    /// 不需要去每個 prefab 加元件。沒有標籤的文字完全不受影響。
    ///
    /// 【原理】
    ///   1. <see cref="ITextPreprocessor"/>：TMP 解析之前把 &lt;shake&gt; 換成 &lt;link="fx:shake:1"&gt;
    ///   2. TMP 解析完，<c>textInfo.linkInfo</c> 就知道哪幾個字在特效範圍裡
    ///   3. 每幀從快取的原始頂點加上位移／透明度，<c>UpdateVertexData</c> 送回去 —— 不重排版、不重建網格
    /// 打字機（maxVisibleCharacters）每跳一個字 TMP 會重建網格，這時重新快取，還沒打出來的字不動。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class TextFx : MonoBehaviour, ITextPreprocessor
    {
        [Header("抖動 <shake>")]
        [Tooltip("位移幅度，佔字級的比例（強度 1 時）")]
        public float shakeAmount = 0.06f;

        [Tooltip("每秒換幾次位置。低一點像抽搐，高一點像顫抖")]
        public float shakeRate = 24f;

        [Header("起伏 <wave>")]
        public float waveAmount = 0.12f;
        public float waveSpeed = 4f;

        [Tooltip("相鄰兩個字差多少相位")]
        public float waveSpread = 0.6f;

        [Header("閃爍 <flicker>")]
        [Tooltip("最暗的時候剩多少不透明度")]
        [Range(0f, 1f)] public float flickerMin = 0.15f;
        public float flickerRate = 14f;

        private const string LinkPrefix = "fx:";

        private TMP_Text text;
        private TMP_MeshInfo[] cached;
        private bool hasFx;

        // ==========================================
        // 自動掛載
        // ==========================================

        /// <summary>
        /// 全域監聽「有文字重建網格了」。文字裡帶特效標籤、而那個 TMP 還沒有這支 → 掛上去並重新解析一次。
        /// 同一幀內完成，玩家看不到標籤原文閃過。
        /// </summary>
        private static class Bootstrap
        {
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
            private static void Init()
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnAnyTextChanged);
                TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnAnyTextChanged);
            }

            private static void OnAnyTextChanged(Object obj)
            {
                var t = obj as TMP_Text;
                if (t == null || !ContainsFxTag(t.text)) return;
                if (t.textPreprocessor is TextFx) return;

                TextFx fx = t.GetComponent<TextFx>();
                if (fx == null) fx = t.gameObject.AddComponent<TextFx>();
                fx.Attach();
                t.ForceMeshUpdate();
            }
        }

        private void Awake() { Attach(); }

        private void Attach()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            if (text != null && !(text.textPreprocessor is TextFx)) text.textPreprocessor = this;
        }

        private void OnEnable()
        {
            Attach();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        // ==========================================
        // 標籤轉換
        // ==========================================

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            { "shake", "shake" }, { "抖", "shake" }, { "抖動", "shake" },
            { "wave", "wave" }, { "波", "wave" }, { "波浪", "wave" },
            { "flicker", "flicker" }, { "閃", "flicker" }, { "閃爍", "flicker" },
        };

        public static bool ContainsFxTag(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('<') < 0) return false;
            foreach (string k in Aliases.Keys)
                if (s.IndexOf("<" + k, System.StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary>&lt;shake=2&gt; → &lt;link="fx:shake:2"&gt;、&lt;/shake&gt; → &lt;/link&gt;。其他標籤原樣保留</summary>
        public string PreprocessText(string s)
        {
            if (!ContainsFxTag(s)) return s;

            var sb = new StringBuilder(s.Length + 32);
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] == '<')
                {
                    int close = s.IndexOf('>', i + 1);
                    if (close > i)
                    {
                        string inner = s.Substring(i + 1, close - i - 1);
                        string converted = ConvertTag(inner);
                        if (converted != null)
                        {
                            sb.Append(converted);
                            i = close + 1;
                            continue;
                        }
                    }
                }
                sb.Append(s[i]);
                i++;
            }
            return sb.ToString();
        }

        private static string ConvertTag(string inner)
        {
            bool closing = inner.StartsWith("/");
            string body = closing ? inner.Substring(1) : inner;
            string name = body, arg = "1";
            int eq = body.IndexOf('=');
            if (eq >= 0) { name = body.Substring(0, eq); arg = body.Substring(eq + 1).Trim().Trim('"'); }

            string fx;
            if (!Aliases.TryGetValue(name.Trim(), out fx)) return null;
            if (closing) return "</link>";

            float strength;
            if (!float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out strength)) strength = 1f;
            return "<link=\"" + LinkPrefix + fx + ":" + strength.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\">";
        }

        // ==========================================
        // 動畫
        // ==========================================

        private void OnTextChanged(Object obj)
        {
            if (obj != text) return;
            cached = text.textInfo.CopyMeshInfoVertexData();
            hasFx = false;
            TMP_TextInfo info = text.textInfo;
            for (int i = 0; i < info.linkCount; i++)
            {
                string id = info.linkInfo[i].GetLinkID();
                if (id != null && id.StartsWith(LinkPrefix)) { hasFx = true; break; }
            }
        }

        private void LateUpdate()
        {
            if (!hasFx || text == null || cached == null) return;

            TMP_TextInfo info = text.textInfo;
            if (info.meshInfo.Length != cached.Length) { cached = info.CopyMeshInfoVertexData(); return; }

            // 先全部還原成原始頂點（上一幀的位移不能留著）
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                if (cached[m].vertices == null || info.meshInfo[m].vertices == null) continue;
                System.Array.Copy(cached[m].vertices, info.meshInfo[m].vertices, Mathf.Min(cached[m].vertices.Length, info.meshInfo[m].vertices.Length));
                if (cached[m].colors32 != null && info.meshInfo[m].colors32 != null)
                    System.Array.Copy(cached[m].colors32, info.meshInfo[m].colors32, Mathf.Min(cached[m].colors32.Length, info.meshInfo[m].colors32.Length));
            }

            float t = Time.unscaledTime;
            int visibleLimit = text.maxVisibleCharacters;
            bool colorsTouched = false;

            for (int l = 0; l < info.linkCount; l++)
            {
                TMP_LinkInfo link = info.linkInfo[l];
                string id = link.GetLinkID();
                if (id == null || !id.StartsWith(LinkPrefix)) continue;

                string[] parts = id.Split(':');
                string kind = parts.Length > 1 ? parts[1] : "";
                float strength = 1f;
                if (parts.Length > 2) float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out strength);

                int first = link.linkTextfirstCharacterIndex;
                int last = Mathf.Min(first + link.linkTextLength, info.characterCount);

                for (int c = first; c < last; c++)
                {
                    if (c >= visibleLimit) break;
                    TMP_CharacterInfo ch = info.characterInfo[c];
                    if (!ch.isVisible) continue;

                    int mi = ch.materialReferenceIndex;
                    int vi = ch.vertexIndex;
                    Vector3[] v = info.meshInfo[mi].vertices;
                    if (v == null || vi + 3 >= v.Length) continue;

                    float size = ch.pointSize > 0f ? ch.pointSize : text.fontSize;

                    if (kind == "shake")
                    {
                        // 每 1/shakeRate 秒跳一個新位置；用 (時段, 字序) 雜湊，不吃 Random 的狀態
                        int step = Mathf.FloorToInt(t * shakeRate);
                        float amp = size * shakeAmount * strength;
                        Vector3 off = new Vector3(Hash(step, c, 1) * amp, Hash(step, c, 2) * amp, 0f);
                        for (int k = 0; k < 4; k++) v[vi + k] += off;
                    }
                    else if (kind == "wave")
                    {
                        float y = Mathf.Sin(t * waveSpeed + c * waveSpread) * size * waveAmount * strength;
                        Vector3 off = new Vector3(0f, y, 0f);
                        for (int k = 0; k < 4; k++) v[vi + k] += off;
                    }
                    else if (kind == "flicker")
                    {
                        Color32[] col = info.meshInfo[mi].colors32;
                        if (col == null || vi + 3 >= col.Length) continue;
                        int step = Mathf.FloorToInt(t * flickerRate);
                        float r = (Hash(step, l, 3) + 1f) * 0.5f;              // 整段一起閃
                        float a = Mathf.Lerp(flickerMin, 1f, r * r) ;
                        a = Mathf.Lerp(1f, a, Mathf.Clamp01(strength));
                        for (int k = 0; k < 4; k++) { Color32 cc = col[vi + k]; cc.a = (byte)(cc.a * a); col[vi + k] = cc; }
                        colorsTouched = true;
                    }
                }
            }

            text.UpdateVertexData(colorsTouched
                ? TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32
                : TMP_VertexDataUpdateFlags.Vertices);
        }

        /// <summary>-1 ~ 1 的穩定雜湊</summary>
        private static float Hash(int a, int b, int salt)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
                return (h & 0xFFFF) / 32767.5f - 1f;
            }
        }
    }
}

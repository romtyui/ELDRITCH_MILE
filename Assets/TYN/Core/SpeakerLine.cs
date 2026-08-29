using System.Collections.Generic;

namespace EldritchMile.Core
{
    /// <summary>
    /// 「角色名：台詞」這種寫法的解析。
    ///
    /// ────────────────────────────────────────────────────────
    /// 【為什麼需要】文案在資產裡是這樣寫的：
    ///
    /// 　　你經過一條小巷子，一個體型龐大的半魚人擋住了你的去路。
    /// 　　半魚人：餓...好餓.....給我.....吃的——
    ///
    /// 第一句是旁白、第二句是角色講話。旁白走系統提示的公版（沒有名字框），
    /// 角色講話要走 `ShowSpeech`（**有名字框**）。
    /// 差別就在開頭那個「半魚人：」。
    ///
    /// 【為什麼要指定名單，不是看到冒號就當人名】
    /// 旁白裡本來就有冒號（「他看向你：那是一種請求」），
    /// 用「有沒有冒號」判斷會把旁白誤判成台詞，名字框就會冒出一個奇怪的名字。
    /// 所以**只認事先講好的那幾個名字**。
    ///
    /// 機率對話（`ProbabilityDialogueView`）與事件（`EventStageController`）
    /// 共用這一支 —— 兩邊各寫一份的話規則遲早會不一樣。
    /// </summary>
    public static class SpeakerLine
    {
        /// <summary>全形與半形的冒號都要認 —— 文案兩種都會出現。</summary>
        private static readonly string[] Marks = { "：", ":" };

        /// <summary>
        /// 試著把一段文字拆成「誰在講」與「講了什麼」。
        /// </summary>
        /// <param name="text">一段文字（通常是一頁／一段）。</param>
        /// <param name="names">認得的說話者。null 或空 = 一律當旁白。</param>
        /// <param name="speaker">認出來的名字。旁白時是空字串。</param>
        /// <param name="body">去掉「名字：」之後的內容。旁白時就是原文。</param>
        /// <returns>是不是角色台詞。</returns>
        public static bool TrySplit(string text, IList<string> names, out string speaker, out string body)
        {
            speaker = "";
            body = text;

            if (string.IsNullOrEmpty(text) || names == null || names.Count == 0) return false;

            for (int n = 0; n < names.Count; n++)
            {
                string name = names[n];
                if (string.IsNullOrEmpty(name)) continue;

                for (int m = 0; m < Marks.Length; m++)
                {
                    string prefix = name + Marks[m];
                    if (!text.StartsWith(prefix)) continue;

                    speaker = name;

                    // ⚠️ 只 TrimStart —— 台詞結尾的空白可能是文案刻意留的停頓
                    body = text.Substring(prefix.Length).TrimStart();
                    return true;
                }
            }

            return false;
        }
    }
}

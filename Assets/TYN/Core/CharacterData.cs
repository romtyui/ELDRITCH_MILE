using System;
using System.Collections.Generic;
using UnityEngine;

namespace EldritchMile.Core
{
    /// <summary>
    /// 一個角色。劇情大綱裡的「情報碎片」們（坎貝爾、克拉夫特、卡羅麗、時藏、伊麗沙白）。
    ///
    /// 【為什麼要有這個】在這之前，說話的人是每個 Stage prefab 上手打的 `speaker` 字串。
    /// 同一個角色在對話節點、商店、特殊事件各打一次，改名字要改三個地方，
    /// 立繪也各自指定 —— 而立繪是同一張圖。
    ///
    /// 【它管什麼、不管什麼】
    ///   管：這個人叫什麼、長什麼樣、會說哪些**沒有分支的話**（寒暄）
    ///   不管：有分支的對話樹。那是 Stage 的事，這裡塞不下也不該塞
    ///
    /// 業界把這兩種分得很開：進場自動冒出來的叫 **bark**，點下去展開的叫 **dialog tree**。
    /// bark 是「氣氛」，可以隨機、可以打斷、漏看也沒差；dialog tree 是「內容」，
    /// 有狀態、要記錄玩家選了什麼。混在一起的話，隨機寒暄會蓋掉劇情。
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "Eldritch/Character")]
    public class CharacterData : ScriptableObject
    {
        [Header("識別")]
        [Tooltip("唯一 id。跟道具一樣，**定了就不要改** —— 對話資料會用它指人")]
        public string id = "";

        [Header("顯示")]
        [Tooltip("玩家看到的名字。這個可以隨時改")]
        public string displayName = "";

        [Tooltip("預設立繪（平常表情）。對話框的特寫圖與氣泡的小頭像共用")]
        public Sprite portrait;

        [Serializable]
        public class MoodPortrait
        {
            [Tooltip("表情的名字。自己取，但**要跟 Stage 上填的字一模一樣**。\n" +
                     "建議用中文短詞：得意／冷淡／驚訝／苦笑")]
            public string mood = "";

            public Sprite sprite;
        }

        [Tooltip("表情差分。填了才會用，**沒填就一律用上面的預設立繪**。\n\n" +
                 "所以現在沒有素材也不影響 —— 等美術給圖再往這裡加，程式不用改。")]
        public List<MoodPortrait> moodPortraits = new List<MoodPortrait>();

        /// <summary>
        /// 取某個表情的立繪。**找不到就退回預設**，不會變成空白。
        ///
        /// 【為什麼是「退回」而不是報錯】表情是漸進補上的資產：
        /// 文案可能先寫好「這句要得意」，但美術還沒畫。
        /// 那時應該照常顯示平常表情、把台詞演完，而不是讓角色消失或跳一堆警告。
        /// </summary>
        public Sprite GetPortrait(string mood)
        {
            if (string.IsNullOrEmpty(mood) || moodPortraits == null) return portrait;

            for (int i = 0; i < moodPortraits.Count; i++)
            {
                MoodPortrait m = moodPortraits[i];
                if (m == null || m.sprite == null) continue;

                if (string.Equals(m.mood, mood, System.StringComparison.OrdinalIgnoreCase))
                    return m.sprite;
            }

            return portrait;
        }

        [Header("分類")]
        [Tooltip("角色標籤。例如 [漁村, 商人]。「這個區域派誰來說話」就是靠它查 —— 見 CharacterPool")]
        public List<string> tags = new List<string>();

        /// <summary>有沒有這個標籤。大小寫不敏感 —— 與 <see cref="ItemData.HasTag"/> 同一個形狀。</summary>
        public bool HasTag(string tag)
        {
            if (string.IsNullOrEmpty(tag) || tags == null) return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], tag, System.StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        [Header("寒暄 (Bark)")]
        [Tooltip("進場自動冒出來的那一句。多句就隨機挑一句 —— 每次進店講同一句話很快就膩")]
        [TextArea(2, 3)] public List<string> greetings = new List<string>();

        [Tooltip("成交時說的話。隨機挑一句。\n" +
                 "**這些是完整的句子，不帶商品名** —— 文件寫的就是這個形狀，\n" +
                 "不要硬塞 {0} 進去")]
        [TextArea(2, 3)] public List<string> purchaseLines = new List<string>();

        [Tooltip("閒聊。**照順序輪，不隨機** ——\n" +
                 "隨機的話玩家連點會撞到同一句，看起來像壞了")]
        [TextArea(2, 3)] public List<string> chatter = new List<string>();

        [Header("對話節點的即時反饋")]
        [Tooltip("判定**成功**時說的話。隨機挑一句。\n" +
                 "與商店的成交台詞是同一個形狀 —— 完整的句子，不帶變數")]
        [TextArea(2, 3)] public List<string> successLines = new List<string>();

        [Tooltip("判定**失敗**時說的話。隨機挑一句")]
        [TextArea(2, 3)] public List<string> failureLines = new List<string>();

        [Tooltip("通用結語。這一段對話結束時說。隨機挑一句")]
        [TextArea(2, 3)] public List<string> farewells = new List<string>();

        [Serializable]
        public class ConditionalLines
        {
            [Tooltip("條件的名字。由外面決定它現在成不成立 —— 這裡只存「什麼條件對應什麼台詞」")]
            public string conditionId = "";

            [TextArea(2, 3)] public List<string> lines = new List<string>();
        }

        [Tooltip("有條件的閒聊（彩蛋）。條件成立時混進閒聊池裡一起輪。\n\n" +
                 "⚠️ **條件成不成立目前沒有人在判斷** —— 例如「坎貝爾不在隊伍內」需要\n" +
                 "「協助者／隊伍」這個系統，而那個系統還不存在。\n" +
                 "現在是由 CharacterHitbox 的 Active Conditions 手動填，等隊伍系統做好再接上去")]
        public List<ConditionalLines> conditionalChatter = new List<ConditionalLines>();

        [Header("備註")]
        [TextArea(2, 4)]
        [Tooltip("給企劃看的。原型、關聯場景之類，不影響行為")]
        public string notes = "";

        public string Label => string.IsNullOrEmpty(displayName) ? id : displayName;

        /// <summary>隨機挑一句寒暄。沒有就回空字串。</summary>
        public string PickGreeting(System.Random rng) => PickFrom(greetings, rng);

        /// <summary>判定成功時說的話。</summary>
        public string PickSuccessLine(System.Random rng) => PickFrom(successLines, rng);

        /// <summary>判定失敗時說的話。</summary>
        public string PickFailureLine(System.Random rng) => PickFrom(failureLines, rng);

        /// <summary>通用結語。</summary>
        public string PickFarewell(System.Random rng) => PickFrom(farewells, rng);

        /// <summary>
        /// 從一組台詞裡隨機挑一句。空的就回空字串（呼叫端一律用「空就不說」處理）。
        ///
        /// ⚠️ 這裡的亂數**不該綁 run 種子** —— 綁了的話同一場 run 每次都聽到同一句，
        /// 三句等於只有一句。詳見 HANDOFF §4.5。
        /// </summary>
        private static string PickFrom(List<string> lines, System.Random rng)
        {
            if (lines == null || lines.Count == 0) return "";
            if (rng == null) return lines[0];

            return lines[rng.Next(lines.Count)];
        }

        /// <summary>隨機挑一句成交台詞。沒有就回空字串。</summary>
        public string PickPurchaseLine(System.Random rng) => PickFrom(purchaseLines, rng);

        /// <summary>
        /// 組出這次要輪的閒聊池：固定閒聊 + 條件成立的彩蛋。
        ///
        /// 【為什麼要先組池，不是每次現查】玩家點第 5 下時池子必須跟第 1 下一樣，
        /// 否則中途條件變了會讓輪播跳號、重複或漏句。呼叫端組一次、存起來輪。
        /// </summary>
        public List<string> BuildChatterPool(ICollection<string> activeConditions)
        {
            var pool = new List<string>();

            if (chatter != null) pool.AddRange(chatter);

            if (conditionalChatter != null && activeConditions != null)
            {
                for (int i = 0; i < conditionalChatter.Count; i++)
                {
                    ConditionalLines c = conditionalChatter[i];
                    if (c == null || string.IsNullOrEmpty(c.conditionId)) continue;

                    if (activeConditions.Contains(c.conditionId)) pool.AddRange(c.lines);
                }
            }

            return pool;
        }

        /// <summary>第 n 句閒聊，循環。不含條件台詞 —— 要含的話用 <see cref="BuildChatterPool"/>。</summary>
        public string ChatterAt(int index)
        {
            if (chatter == null || chatter.Count == 0) return "";

            int i = index % chatter.Count;
            if (i < 0) i += chatter.Count;

            return chatter[i];
        }
    }
}

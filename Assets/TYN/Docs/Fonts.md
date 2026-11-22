# 字體：裝在哪、哪裡用哪一套

2026-11-15 依美術組《字體&尺寸表》統一安裝。所有字體都在 `Assets/TYN/ART/Fonts/`，TextMeshPro 請直接選 **`… SDF.asset`**（不要選 .ttf/.otf）。戰鬥、大廳也用這一份，不要再各自另裝。

## 字型資產

| 表上的字體 | 粗細 | 要選的 TMP 字型資產 | 來源 |
|---|---|---|---|
| 思源宋體 | Heavy | `SourceHanSerif/SourceHanSerifTW-Heavy SDF` | Adobe 官方 GitHub（台灣字形子集），SIL OFL，授權檔在同資料夾 |
| 思源宋體 | Bold | `SourceHanSerif/SourceHanSerifTW-Bold SDF` | 同上 |
| Corbel | Bold | `Corbel/Corbel-Bold SDF` | 本機 Windows 字型 |
| Abril | Regular | **還沒裝**（見下方） | Adobe Fonts 專屬 |
| Source Code Variable | Light | `SourceCodePro/SourceCodePro-Light SDF` | Adobe 官方 GitHub，SIL OFL，就是 Source Code 的 Light 靜態版 |
| Bahnschrift | Bold | `Bahnschrift/Bahnschrift-Bold SDF` | 本機 Windows 字型（原檔是可變字型，已切出固定粗細） |
| Bahnschrift | Light | `Bahnschrift/Bahnschrift-Light SDF` | 同上 |
| 微軟正黑體 | Regular | `MicrosoftJhengHei/MicrosoftJhengHei-Regular SDF` | 本機 Windows 字型 |

- 全部都是動態字型（Dynamic）：用到哪個字才產生哪個字，不必先烘字表；出包時會清空暫存的字。
- **粗細是分開的檔案**，Heavy 就選 Heavy 那份，不要用 TMP 的 **B** 按鈕假粗。
- 英數字型（Corbel、Source Code、Bahnschrift）沒有中文字，遇到中文會自動改用**微軟正黑體**補字。表上微軟正黑體沒有指定用途，所以目前只當補字用。

## 哪裡用哪一套（照表）

| 用途 | 字型資產 | 表上的尺寸 |
|---|---|---|
| **探索** 選項內文 | 思源宋體 Heavy | 8.5pt |
| 事件劇情內文（黑框內） | 思源宋體 Heavy | 9.5pt |
| 祭壇「願意相信嗎」 | 思源宋體 Heavy | 27pt |
| 角色姓名 ＋ 姓名下的編號 | Source Code Light | 12.5pt ＋ 7pt |
| 探索選項數字 % | Bahnschrift Bold | 15.2pt |
| 寶箱鎖鏈上數字 | Bahnschrift Light | 19pt |
| 祭壇 yes / no | Bahnschrift Light | 27pt |
| **通用** 上方區域名稱 | 思源宋體 Heavy | 18pt |
| 區域名稱旁的小字 | Corbel Bold | 14pt |
| 上方區域關卡數字 | Abril | 28.7pt |
| 藥水攜帶已滿提醒：丟棄 | 思源宋體 Bold | 31pt |
| 藥水攜帶已滿提醒：道具名稱及敘述 | 思源宋體 Heavy | 22.75pt |
| 藥水攜帶已滿提醒：橫幅 | 思源宋體 Heavy | 55pt |
| **商店** 點擊商品詳閱：已滿提醒 | 思源宋體 Bold | 27.4pt |
| 點擊商品詳閱：商品敘述 | 思源宋體 Bold | 26pt |
| 點擊商品詳閱：商品名稱 | Source Code Light | 34.75pt |
| 點擊商品詳閱：商品價格 | Source Code Light | 35.5pt |
| 商店價目表 | Source Code Light | 23pt |
| **戰鬥** 屬性細節文字敘述 | 思源宋體 Bold | 12pt |
| 左側跟隨夥伴說話 | 思源宋體 Bold | 10pt |
| 左側跟隨夥伴角色名字 | Source Code Light | 11.8pt |
| 普通怪物名稱 | 思源宋體 Heavy | 20.8pt |
| Boss 怪物名稱 | 思源宋體 Heavy | 29.6pt |
| 戰鬥完［選擇獎勵］ | 思源宋體 Heavy | 30.5pt |
| 領取獎勵表：獎勵名稱 | 思源宋體 Heavy | 33pt |
| 領取獎勵表：右側按鈕文字 | 思源宋體 Heavy | 30pt |
| 領取獎勵表：動作未完成提醒橫幅 | 思源宋體 Heavy | 55pt |
| 領取獎勵表：橫幅「下次不再提醒」 | 思源宋體 Heavy | 22.6pt |
| 領取獎勵表：獎勵數量數字 | Source Code Light | 32.3pt |
| 書本：卡片敘述 | 思源宋體 Bold | 15.3pt |
| 書本：上方分類標籤中文 | 思源宋體 Heavy | 31.75pt |
| 書本：上方分類標籤數字、下方頁碼 | Abril | 55pt、35pt |
| 普通怪物血量 / Boss 血量 / Boss 護盾 | Abril | 22.3 / 44.7 / 19.4pt |
| 怪物 buff 疊層數字 | Abril | 7.6pt |
| 我方護盾量 / 血量 / SAN 值 | Abril | 18.2 / 33.6 / 128pt |
| **大廳** 開始遊戲選項 | 思源宋體 Heavy | 33pt |
| 整備區：攻守比例標題 | 思源宋體 Heavy | 35.5pt |
| 整備區倉庫：分類項 / 選擇按鈕 | 思源宋體 Heavy | 18.5 / 22pt |
| 整備區倉庫：道具詳細敘述 | 思源宋體 Bold | 14pt |
| 整備區倉庫：道具名稱 | Source Code Light | 18pt |
| 圖書館圖鑑：怪物名稱 | 思源宋體 Heavy | 29pt |
| 圖書館圖鑑：項目標題 / 內文 | 思源宋體 Bold | 15.7 / 14pt |
| 圖書館圖鑑：怪物編號 | Bahnschrift Light | 25.4pt |
| 圖書館圖鑑：下方頁碼 | Abril | 30.85pt |

## 還沒定的事

1. **Abril 還沒裝。** 它只在 Adobe Fonts 上，要登入 Adobe 帳號啟用，拿不到可以放進專案的字型檔。請持有授權的組員把 Abril Regular 的 .otf／.ttf 檔給我們，收到後照同樣方式建字型資產。戰鬥的血量、護盾、SAN 數字都用它。
2. **pt 要換算。** 遊戲畫面以 1920×1080 為基準。如果美術的設計稿也是 1920×1080，表上的 pt 可以直接當 TMP 的 Font Size；如果不是，要照比例換算。請美術確認設計稿的畫布尺寸。另外探索選項內文 8.5pt、事件內文 9.5pt 在 1920×1080 上會非常小，也請一併確認。
   - **2026-11-23 部分確認：**《藥水已滿》那份稿的畫面寬 677.5mm ＝ 1920pt，**所以那份稿上 1pt ＝ 遊戲裡 1px**，標的 pt 直接填 Font Size 就對（1mm ≈ 2.834px）。探索那幾頁還是要問畫布多大。

## 為什麼字級常常要設得比表上大（2026-11-23 查證）

兩個原因疊在一起：

**一、有些文字的父物件被縮放過**，填的字級要再乘上那個倍率才是畫面上看到的大小。全部 13 張畫布都是 ScaleWithScreenSize 1920×1080、縮放 1，**畫布本身沒問題**；問題在下面這幾個父物件：

| 位置 | 被縮放的父物件 | 倍率 | 例：填的字級 → 畫面上 |
|---|---|---|---|
| 遭遇打牌的三個選項 | `DialogueUI/option_box/answer_1~3` | **×0.32** | 選項文字 64 → 20.5、預覽 90 → 28.8 |
| 對話框（黑框） | `DialogueUI/dialogbox/text_box` | ×0.93 | 姓名 36 → 33.5、內文 32 → 29.8 |
| 系統選單（齒輪）按鈕 | `OptionMenuPanel/MainPage/…Button`（Romtyui） | ×3.0 | 19 → 57 |
| 系統選單設定頁 | `OptionMenuPanel/SettingsPage` | ×1.75 | 72 → 126 |
| 寶箱／祭壇獎勵卡、商店詳閱的卡 | `card_template`（Romtyui 的卡） | ×1.2～2.3 | 只用來組字，不顯示 |

遭遇選項那一個最嚴重：字要填三倍大才看起來正常，就是這個 ×0.32。要修的話是把父物件縮放改回 1、尺寸和字級照比例乘回去，畫面不會變，以後填的字級就是看到的大小。**還沒改**，等確認要不要動。

**二、表上的 pt 來自不同大小的設計稿。** 同一張表裡，藥水已滿那份是 1920pt 寬（1pt＝1px），探索那幾頁明顯小很多（扣掉上面的縮放，事件內文還是差約 3 倍）。每一份稿要知道畫布寬多少 pt，才換算得出來。

## 現有畫面套用狀態（2026-11-15）

判斷方式：現在的字級和表上的尺寸差 25% 以內算「相近」，直接換字體、字級不動；差更多的先記下來不動。
換成真的 Bold／Heavy 檔的地方，原本 TMP 的假粗（**B**）已取消，避免粗上加粗。

### 已換
| 畫面 | 物件 | 換成 | 現在字級 / 表上 |
|---|---|---|---|
| 主選單 | START／RESTART／SETTINGS | 思源宋體 Heavy | 31 / 33（對照大廳「開始遊戲選項」） |
| 祭壇 | 「你願意相信嗎?」 | 思源宋體 Heavy | 27 / 27 |
| 祭壇 | YES／NO | Bahnschrift Light | 27 / 27 |
| 祭壇 | 字幕（收下吧。…） | 思源宋體 Heavy | 27 / 表上沒列，跟問句同一畫面所以一起換 |
| 商店 | 貨架價格 | Source Code Light | 26 / 23 |
| 商店 | 商品詳閱：名稱、價格 | Source Code Light | 30 / 34.75、32 / 35.5 |
| 商店 | 商品詳閱：敘述 | 思源宋體 Bold | 28 / 26 |
| 寶箱、祭壇獎勵表 | 獎勵名稱 | 思源宋體 Heavy | 36 / 33 |
| 寶箱、祭壇獎勵表 | 獎勵數量數字 | Source Code Light | 36 / 32.3 |
| 寶箱、祭壇獎勵表 | 右側按鈕文字 | 思源宋體 Heavy | 24 / 30 |
| 寶箱、祭壇獎勵表 | 「選擇一個」 | 思源宋體 Heavy | 31.5 / 30.5 |
| 寶箱、祭壇獎勵表 | 未完成提醒橫幅（自動縮放 18–72） | 思源宋體 Heavy | 43.35 / 55 |
| 寶箱、祭壇獎勵表 | 「本趟旅程不再顯示」 | 思源宋體 Heavy | 21.7 / 22.6 |
| 牌組書本 | 牌組區／棄牌區 標籤 | 思源宋體 Heavy | 29.69 / 31.75 |
| 牌組書本 | 卡片敘述（自動縮放 11–20） | 思源宋體 Bold | 11–20 / 15.3 |
| 食物欄滿了 | 道具名稱、敘述 | 思源宋體 Heavy | 27.7（2026-11-23 改版，照新稿標註；表上是 22.75） |
| 食物欄滿了 | 丟棄按鈕 | 思源宋體 Bold | 31 / 31 |
| 食物欄滿了 | 橫幅「藥水攜帶已滿，請捨棄一些裝備」 | 思源宋體 Heavy | 55 / 55 |

### 記下來沒動：尺寸落差明顯
| 畫面 | 物件 | 表上 | 現在 | 差 |
|---|---|---|---|---|
| 對話框 | 角色姓名 | Source Code Light 12.5 | 36 | 約 2.9 倍 |
| 對話框 | 事件劇情內文（黑框內） | 思源宋體 Heavy 9.5 | 32 | 約 3.4 倍 |
| 遭遇打牌 | 選項內文 | 思源宋體 Heavy 8.5 | 28（自動縮放 12–24） | 約 3 倍 |
| 遭遇打牌 | 選項數字 % | Bahnschrift Bold 15.2 | 30 | 約 2 倍 |
| 寶箱 | 鎖鏈上數字 | Bahnschrift Light 19 | 42（執行時跟著鎖鏈等比縮放） | 約 2.2 倍 |
| 寶箱、祭壇獎勵表 | 標頭的區域名稱 | 思源宋體 Heavy 18 | 26.7 | 多 48% |
| 寶箱、祭壇獎勵表 | 標頭的 NORTHBOUND／NO RETURN 小字 | Corbel Bold 14 | 18.2 | 多 30% |

**探索欄的項目都大約是表上的 3 倍**，比例很一致，看起來那一頁設計稿的畫布比 1920×1080 小（例如 640×360）。請美術確認後，我們照比例換算就能一次換完。

### 等 Abril
獎勵表標頭的關卡數字（III - IV）、書本的分類數字和頁碼、HUD 的 HP／SAN 數字。

### 戰鬥（Romtyui）
`Stage_Battle_v7` 是 Romtyui 戰鬥的打包快照，下次重打包會被覆蓋，所以沒有改。表上「戰鬥」欄請他在自己的場景裡換，字型資產用上面那份。

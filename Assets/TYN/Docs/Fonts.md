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
3. 現有畫面的字還沒換。換字會改變排版寬度，等上面兩點確認後再一起換。

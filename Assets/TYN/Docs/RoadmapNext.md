# ELDRITCH_MILE — 推進方針（2026-08-14）

> 銜接 [Status.md](Status.md)（2026-08-08 快照）與 [SceneConsolidationPlan.md](SceneConsolidationPlan.md)（v4）。
> 本文件只記錄「接下來做什麼」，架構決策與約束仍以 v4 設計文件為準，不重複展開。

---

## 0. 與 Status.md 的落差（先校正現況）

`Status.md` 停在 2026-08-08，但分支 `recovery-progress` 之後已有 8 個新 commit（8/11 最新一筆「UI流程維護」），且工作區目前還有一批**進行中、尚未完工**的改動。校正後的現況：

| 項目 | Status.md 記錄 | 實際現況 |
|---|---|---|
| Phase 4c 打牌 UI | 🔴 完全未做 | 🔶 **第一批已完成**（拖曳出牌、hover 全選項預覽、即時判定、逐次衰減、`EncounterTargetView` 對話框特寫圖）。詳見 [Phase4c_CardPlay.md](Phase4c_CardPlay.md) |
| 卡牌屬性資料 | `AttrA~AttrD` 佔位、單一機率階梯 | 已重構為 **A/B/C 三屬性 × 0/20/40/60/80/100 六階梯**，各自配 `_vis` 視覺資產（`Card_data/v3/explore_{A,B,C}_{0..100}.asset`） |
| `TwoStageConfirm.cs` | 保留，服務 C8/C14 | **已封存**（非刪除，`.meta` 一起搬到 `_Archive/Scripts/`，符合封存慣例）。**已靜態確認安全**：全專案 `.unity`/`.prefab` 沒有任何物件掛它（`git grep` GUID 零命中）。C14 實際靠 `BookmarkHover`（滑下）+ 普通 `Button`（`ExitTag.onClick → ShowContinueAsk()`）+ `ContinueAskPanel` 構成兩段式，C8 抓取靠 `InteractableBase` 直接呼叫 `CursorManager` 的 `HoverChest`/`HoldChest`。兩者都不依賴 `TwoStageConfirm`，封存不影響功能 |
| `ExplorationHandUIController.cs` | 待改寫的 8 個之一 | 已封存到 `_Archive/Scripts/`，由新的 `ExploreHandUI` 取代 |
| 未提交改動規模 | 174 rename + 108 delete | ~~更大~~ **已於 2026-08-14 19:41 commit `f8bf980「Phase4」`**，工作區目前乾淨。原本的風險項目已解除 |

> `f8bf980` 涵蓋了 `Phase4c_CardPlay.md` 文末「下一批（4c 剩餘）」四項的部分工作：`DialogueBoxUI` / `PopupService` / `ExploreStageController` / `ChestInteractable` / `InteractableBase` / `DialogueEncounterController` / `ProbabilityCheck` 都有改動，加上 `EncounterTargetView` 新檔與屬性卡資料重構。**尚未確認這批改動是否已經讓「主要目標選定 UI」「在試一次？確認」「角色對話框連動」三項功能完整可玩，還是只是墊基礎**——需要走一次 Play Mode 驗收才能確定，見下方「立即優先」。

---

## 1. 立即優先（本輪收尾）

### 1.1 `f8bf980` 的實際涵蓋範圍（2026-08-14 逐檔查證）

上一節說「不確定是墊基礎還是做完」，查完程式碼的結論是**墊基礎**。逐項：

| 項目 | 實際狀態 |
|---|---|
| `EncounterTargetView`（對話框特寫圖／拖曳目標／機率標籤） | ✅ **真的做完了** |
| C18① 主要目標選定 | 🔶 Core 層 API 全部備妥（`SelectPrimaryTarget` / `OnPrimaryTargetChanged` / `ShouldBroadcastPreview`），但**全專案零呼叫端** —— `PrimaryTarget` 永遠是 `null`，UI 完全沒做 |
| C12「在試一次？」 | ✅ **本輪已完成**（見 1.2） |
| C18③ 判定結果反映 | 🔶 **上一節那句已過時**。`ChestInteractable.OnCheckResult` 早就走 `PopupService.ShowInstant(successText/failText)` 即時替換對話框正文，不是「只寫 Console 與彈窗」。真正缺的只有**選項內文**那半，那確實要等 Phase 6 |
| `EncounterUIController` | 🔶 **本輪已建立**，但只裝了 C12（見 1.3） |

### 1.2 C12 回合感 — 本輪完成（**做法已改，不是確認視窗**）

程式已完成、編譯 0 error 0 warning。操作與驗收見 [Phase4c2_RetryAsk.md](Phase4c2_RetryAsk.md)（2 分鐘）。

**確認視窗的做法已否決。** 先做了一版「失敗跳詢問」，檢討後認為每次失敗都跳是錯的：

- 它傳達的資訊（「你還可以再試」）**只有第一次是新的**，之後純摩擦
- 原本的前提「失敗沒有出口」**不成立** —— 「結束」按鈕整個環節都在畫面上
- 「再試要付什麼代價」**hover 的預覽數字早就答了**（看得到機率下降）
- 打斷 C18 的核心：連續嘗試。5 張手牌失敗 4 次 = 點掉 4 個彈窗

**改成**：回合感寫進判定結果文字，零打斷。第二次起附加「這是你嘗試的第 {x} 次。」，
搭配下降中的預覽數字構成回合感。`DialogueEncounterController.attemptSuffixFormat` 可在 Inspector 調文案。

新增檔案：

| 檔案 | 狀態 |
|---|---|
| `Core/PanelToggle.cs` | 面板顯示的三段 fallback，共用型別 |
| `Core/EncounterUIController.cs` | 確認視窗實作。`Ask Mode` **預設 `Never`（不啟用）**，留著是為了能在 Play Mode 直接比較兩種手感 |

既有檔案的改動只有兩處：`DialogueEncounterController` 加 `attemptSuffixFormat` / `WithAttemptLine()`，
`ChestInteractable.OnCheckResult` 呼叫它（一行）。

### 1.2a 徹底失敗的結案路徑 — 本輪完成（修掉一個會卡住房間的 bug）

**症狀**：手牌 5 張全部失敗後，寶箱仍可再次互動、再抽一手牌。

**根因**：`MarkDone()` 只在 `Open()`（成功）裡呼叫。判定失敗**沒有任何結案路徑**，
所以 `hasInteracted` 永遠 `false`、`CanInteract` 永遠 `true`。

連帶兩個後果（比表面症狀嚴重）：

| 後果 | 說明 |
|---|---|
| **房間永遠清不掉** | `RoomController.ReportInteracted` 只有 `MarkDone()` 會呼叫 → `interactedCount` 到不了 `tracked.Count` → **C13「要探索其他的東西嗎？」永遠不會自動跳**，玩家只剩 ExitTag 一條路 |
| **保證 0% 的假迴圈** | 重進去時目標衰減已歸零，新抽的每張牌都必定失敗；而棄牌堆會 `ReshuffleDiscardIntoDraw` 循環，所以牌不會耗盡 —— 永遠跑不完 |

**改法**：`IProbabilityTarget` 新增 `OnAttemptsExhausted()`，由 `ExploreStageController`
在環節結束且 `!HasCardsLeft` 時呼叫（中途按結束**不呼叫** —— 那是暫停不是用盡）。
`InteractableBase` 新增 `MarkFailed()`：換 `failedSprite`、**絕不消失**、回報房間、
設 `FailedPermanently` 讓再次點擊能給出提示而不是沉默。

> 失敗**不能**共用 `interactedSprite`（那是「打開的寶箱」）—— 沒撬開的箱子長成打開的樣子
> 會直接誤導玩家以為成功了。所以另開 `Failed Sprite` 欄位。

**順手修的潛在 bug**：`RoomController.ReportInteracted` 收了 `interactable` 參數卻沒用，
無條件 `interactedCount++`。只要有物件回報兩次，房間就會在還有東西沒互動時宣告清空。
已改為用 `HashSet` 去重。

### 1.2b 重試／逆轉 — 段 A 已完成並驗收，段 B 待跨隊

**已決定的設計**（2026-08-15）：

```
物件
├── 一般物件 → 直接開，不進打牌環節（OpenMode.Direct，已存在）
└── 特殊物件 → 進打牌環節
    ├── 不可重複嘗試 → 手牌用盡即結案（§1.2a 已完成）
    └── 可重複嘗試 → 付出全域代價後重來
        ├── 消耗特殊道具
        └── 消耗 HP／SAN（代價 = 大類型固有倍率 × 遞增倍率）
```

**重試語意**：付出代價後**衰減重置回初始**、**手牌重抽**，之後機率照樣逐次衰減；
只要還付得起就能一直重來。

**嘗試次數不重置** —— 重來後顯示「第 6 次」而不是回到「第 1 次」。衰減既然重置了，
「總共在這東西上耗掉多少」就成了唯一還看得見的代價紀錄，而且它正好是遞增倍率的依據。

**HP／SAN 歸零**：預設**直接死亡**（`ReportComplete(StageResult.PlayerDied)`，流程已存在）。
另做一個「保底留 1、探索不會死」的**可啟用狀態** —— 既方便測試，日後也能當成特殊道具的效果。

#### HP／SAN 的現況（查證結果，與先前推測不同）

**不需要自己做，而且大部分已經存在：**

| 需要的 | 現況 |
|---|---|
| HP／SAN 數值 | ✅ `RunStateManager`（Romtyui）已有 `savedPlayerMaxHp/CurrentHp`、`savedMaxEnergy/CurrentEnergy`，跨戰鬥保存、`DontDestroyOnLoad`。**Romtyui 已經把 Energy 就叫 SAN**（見其 log 字串） |
| 歸屬 | ✅ 設計文件 §8 早已定案：HP／能量以 `RunStateManager` 為準，`RunContext` 不重複儲存 |
| 「探索中死亡」結局 | ✅ `StageResult.PlayerDied` + `GameFlowManager.cs:186` + `ContributeToMeta` 都在 |
| TYN 讀寫的橋 | ❌ **唯一缺的**。TYN 至今從未在程式裡引用 `RunStateManager`，只有註解提到 |

#### ⚠️ 跨隊待辦（要跟 Romtyui 談，不要自己動手）

**HP／SAN 的初始化時機。** `savedPlayerCurrentHp` 只有在 `SaveFromBattle()` 之後才有值 ——
也就是**第一場戰鬥打完才存在**。玩家若在任何戰鬥之前先進探索房間，HP 是 `0`。
探索要扣值就必須要求「**run 開始時就初始化 HP／SAN**」。

與 §7.2 的 `OnBattleEnded` 是同一類需求，現在提比事後補便宜。

> 另：`RunStateManager.cs` 的中文註解疑似不是存成 UTF-8（讀出來是亂碼）。不影響執行，可順帶提醒。

#### 實作分兩段

| 段 | 內容 | 阻塞 |
|---|---|---|
| **A（可立刻做）** | 消耗**特殊道具**的重試。`RunContext.HasItem/ConsumeItem` 已存在，純 TYN，零跨隊依賴 —— 整套重試手感（詢問 UI、衰減重置、手牌重抽、次數累加）都能先端到端跑起來 | 無 |
| **B（等談定）** | HP／SAN 成本。段 A 做完後，這只是把「檢查／扣除」換一個來源的薄轉接層 | 初始化時機 |

**仍待補的資料**：大類型的清單與各自的固有倍率、代價基礎值與遞增規則。
建議比照 `AttributeChartData` 做成 ScriptableObject，讓數值在 Inspector 調，程式不寫死。

### ~~1.2b~~ 1.2d 舊記錄：逆轉曾卡在兩個設計決定（已解）

構想是手牌用盡時給特殊狀態，問玩家要不要逆轉。**現在寫出來會是假選擇**：

`DecayStep = 1 / 手牌數`（建議設定），出完 N 張後衰減倍率正好是 **0**；
`ProbabilityCheck` 算 `base × 相剋 × 衰減` 且對 `finalRate <= 0` 直接短路成失敗。
所以玩家點「逆轉」得到的是保證 0% 的嘗試 —— 比不問更糟。

| 待決 | 為什麼卡住 |
|---|---|
| 逆轉如何處理衰減 | 重置回 1.0？某個比例？還是逆轉牌無視衰減？不解決這條功能等於沒有 |
| 逆轉要付什麼代價 | 免費的額外嘗試會廢掉 C18⑤（手牌數 ＝ 嘗試次數上限）。候選：消耗道具／HP 或理智／一次遭遇限一次／Phase 7 神牌 |

決定後：`AskMode` 加 `OnHandExhausted`，並在 `PlayCard` 的「手牌用盡自動結束」加攔截點（約 5 行）。

### 1.2c 一般物件 vs 特殊物件（新方向，待展開）

企劃方向：**普通箱子打開就是打開**（`OpenMode.Direct`，已存在）；
**只有帶特殊效果的物件**（陷阱、逆轉等）才進打牌環節留手牌給玩家嘗試。

這會讓打牌環節變稀有而更有份量，也讓上面的「逆轉」有正當的落點。
但「特殊效果」目前**不存在於資料模型**裡 —— `ChestInteractable` 只有
`openMode` / `attribute` / `lootItems` / `grantedItemIds`，沒有承載陷阱或逆轉的欄位。
要做需要先定義效果的表達方式，屬於獨立一批。

### 1.3 C18① 主要目標選定 — 建議延到 Phase 6

本輪**刻意沒做**，兩個理由：

1. **現階段是空轉的**。`ExploreStageController` 的 `BeginEncounter` 永遠只傳一個目標
   （`new List<IProbabilityTarget> { encounterTarget }`）。單一目標時「選定」與「未選定」
   行為完全一樣 —— 廣播全選項就是廣播那一個。要到 Phase 6 多選項對話才看得出差別。
2. **它跟現有操作搶點擊**。點對話框裡的大圖，目前語意是「兩段式出牌的第二段」
   （`EncounterTargetView.OnClicked` → `TryPlaySelectedCardOn`）。C18① 若做成
   「點大圖＝選定主要目標」，兩者會撞在同一個點擊上，得先決定怎麼分
   （右鍵？長按？還是只在目標數 > 1 時才啟用選定？）—— 這是個**待決的設計問題**。

### 1.4 Phase 4c 現況：✅ 已全部驗收（2026-08-15）

三批都已在 Play Mode 跑過：

| 批次 | 內容 | 狀態 |
|---|---|---|
| 第一批 | 拖曳出牌、hover 全選項預覽、即時判定、逐次衰減、對象大圖 | ✅ |
| 第二批 | C12 回合感（次數提示，取代確認視窗） | ✅ |
| 第三批 | 徹底失敗結案、房間清空修復、付代價重試（段 A 道具） | ✅ |

**收尾時修掉的三個訊息被吃掉的來源**（改這一帶時務必先讀
[Phase4c3_RetryCost.md](Phase4c3_RetryCost.md) 的「訊息被吃掉的三個來源」）：
`ShowInstant` 蓋掉正文、`AdvanceImmediate` 無條件執行、詢問面板蓋住還在打的字。

### 1.5 接下來

**先 commit** —— 這是一個驗收過的乾淨斷點，§4 早就提醒過不要讓未提交的東西越堆越高。

之後建議順序：

1. **Q7a 屬性正式命名**（見 §3）—— **這一項現在到期了**。§3 原本就寫「Phase 4c 收尾前定案」，
   而 4c 已經收尾。再拖下去 Phase 6 的對話選項也要跟著用佔位名，而且美術端已經在照 A/B/C 準備素材
2. **Phase 4d 收尾驗證**（見 §2）—— 便宜。但注意 `chest_RequiresKey` 的權重目前是 0（測試腳手架），
   要驗鑰匙寶箱得先補回來
3. **跟 Romtyui 談一次，兩件事一起提**（見下）
4. Phase 5 戰鬥接入 / Phase 6 對話與商店

#### 🔴 跨隊：兩個需求合併成一次對話

兩件事都卡在 Romtyui，分兩次提是浪費：

| 需求 | 為了什麼 | 出處 |
|---|---|---|
| `BattleManager.OnBattleEnded(BattleOutcome)` 事件 | Phase 5 戰鬥接入 | §7.2 |
| **run 開始時就初始化 HP／SAN**（不要等第一場戰鬥打完） | 段 B：探索扣 HP／SAN | §1.2b |

順帶可提：`RunStateManager.cs` 的中文註解疑似不是存成 UTF-8（讀出來是亂碼）。

#### 測試腳手架：正式配內容前要還原

| 項目 | 現況 | 還原成 |
|---|---|---|
| `RoomContent_Village` 的 `chest_RequiresKey` / `document` | `weight: 0`（刻意，讓測試寶箱必定出現） | 補回 1，否則這兩者永遠不生成 |
| `AttributeChart` 的 `AttrA → AttrC = None` | 測試用，為了驗得到 `✕` | C19 說 `None` 應少用；Q7a 定名時一起重看 |

---

## 2. 中期目標（接續 Phase 4d → 5 → 6）

### Phase 4d 收尾
- Spawn slot 隨機外觀、鑰匙寶箱、兩段式離開（C14）— 依 [SceneConsolidationPlan.md](SceneConsolidationPlan.md) §6 Phase 4 驗收表已大致就緒，但 `TwoStageConfirm` 被刪後**需要重新確認 ExitTag 的第二段確認還在**（Phase4a 文件步驟 6 說 ExitTag 靠 `BookmarkHover` + `ContinueAskPanel`，理論上不依賴 `TwoStageConfirm`，但需現場驗證，不要只看文件推論）。

### Phase 5 — 戰鬥接入
- 可與 Phase 4c 平行推進，不互相阻擋。
- **待辦（跨隊協調）**：向 Romtyui 要 `BattleManager.OnBattleEnded(BattleOutcome)` 事件（見 [SceneConsolidationPlan.md](SceneConsolidationPlan.md) §7.2）。現在提比事後補便宜，且我方每次重新打包 prefab 都能直接受益。
- 我方動作：從 Romtyui 的 Scene 打包成 prefab、包一層 `BattleStageController`、檢查 Camera/EventSystem/AudioListener 重複。

### Phase 6 — 對話與商店
- `DialogueData`(SO) + `DialogueStageController`，選項改實作 `IProbabilityTarget`。
- 這是 C18③（選項內文即時更新）真正該落地的地方——若第 1 節提前做了簡化版，這裡要合併掉，不要留兩份平行邏輯。
- 商店（C15）：購買後留在商店繼續買。

### Phase 7 — 特殊事件授予神牌
- 範圍小：`Stage_SpecialEvent.prefab` + 把神牌 `CardData` 加入 `RunStateManager.savedDeck`，效果與平衡歸 Romtyui。

---

## 3. 仍待決的設計問題

延續 [SceneConsolidationPlan.md](SceneConsolidationPlan.md) §10.2，目前的暫行做法都還在用，**尚未看到正式定案**：

| # | 問題 | 現況 | 最晚需要 |
|---|---|---|---|
| Q7a | 屬性正式名稱 | 已從「單一階梯」進展到「A/B/C 三屬性完整矩陣」，但名稱仍是佔位符 | Phase 4c 收尾前定案，否則 Phase 6 對話選項也要跟著用佔位名 |
| Q11 | 衰減級距：線性或比例 | `DialogueEncounterController` 有 `Decay Scaled To Hand Size` 開關，暫行線性 | 若本輪測試手感不對，這是第一個要調的參數 |
| Q12 | 主要目標可否更換 | 暫行可自由更換，各自獨立衰減 | 第 1 節做「主要目標選定 UI」時會直接遇到，順手定案 |
| Q13 | 未用手牌怎麼處理 | 暫行棄掉、下個事件重抽 | 同上 |

---

## 4. 風險與技術債（背景參考，非本輪待辦）

- **未提交規模持續增長**：目前工作區改動比 Status.md 記錄的 174 rename + 108 delete 更大。這不阻擋技術規劃，但建議在完成第 1 節「立即優先」四項、確認可跑之後，找一個自然斷點 commit，避免半成品堆得更高難以拆分。
- `_Archive/` 內 13 個被保留腳本間接依賴的舊檔仍在編譯範圍，Phase 4c/6 重寫完該區塊後可以再收斂。
- 卡牌資料新舊並存：`explore_forward_*` 已刪、`explore_{A,B,C}_*` 已補齊，若還有場景/prefab 引用舊資產名稱會在 Editor 出現 missing reference，建議收尾時全域搜尋一次 `explore_forward` 確認沒有殘留引用。
- 屬性名稱未定案（Q7a）：程式面不受影響（SO 承載），但美術與文案端可能已經照 A/B/C 在準備素材（新增的「機率牌框紅/藍」美術即是徵兆），越晚定案置換成本越高。

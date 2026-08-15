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

### 1.2b 手牌用盡時的「逆轉」— 卡在兩個設計決定

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

### 1.4 接下來

1. **走一次 [Phase4c2_RetryAsk.md](Phase4c2_RetryAsk.md) 的編輯器步驟 + 驗收**（20–30 分鐘）
2. 順便補跑 [Phase4c_CardPlay.md](Phase4c_CardPlay.md) §8 第一批的驗收清單 —— 那批至今仍未在 Play Mode 確認過
3. 之後接 Phase 4d 收尾或 Phase 5 戰鬥接入（見 §2），C18① 併入 Phase 6

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

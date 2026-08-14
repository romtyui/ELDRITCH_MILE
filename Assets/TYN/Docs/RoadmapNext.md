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
| `TwoStageConfirm.cs` | 保留，服務 C8/C14 | **已刪除** —— 出牌的兩段式確認改在 `ExploreCardDrag`/`EncounterTargetView` 上另外實作（形狀不同，見 Phase4c_CardPlay.md「為什麼不直接用 TwoStageConfirm」）。ExitTag(C14) 目前如何補位需要**現場確認**，見下方待辦 |
| `ExplorationHandUIController.cs` | 待改寫的 8 個之一 | 已封存到 `_Archive/Scripts/`，由新的 `ExploreHandUI` 取代 |
| 未提交改動規模 | 174 rename + 108 delete | **更大**（新增 Phase 4c 一批 + 屬性卡資料重構 + 上述封存）|

> 目前**進行中**的工作對應 `Phase4c_CardPlay.md` 文末「下一批（4c 剩餘）」四項：主要目標選定 UI、「在試一次？」確認、`EncounterUIController`、角色對話框連動（C18③）。修改集中在 `DialogueBoxUI` / `PopupService` / `ExploreStageController` / `ChestInteractable` / `InteractableBase` / `DialogueEncounterController` / `ProbabilityCheck`。

---

## 1. 立即優先（本輪收尾）

延續目前進行中的 Phase 4c 剩餘項目，建議順序：

1. **確認現有改動能跑**——先在 Editor 內把目前的改動走一次 [Phase4c_CardPlay.md](Phase4c_CardPlay.md) §8 驗收清單，確認沒有半成品破壞既有功能（尤其 `TwoStageConfirm` 刪除後，C14 兩段式離開是否還完整）。
2. **C18① 主要目標選定 UI**：選定後 hover 不再廣播全選項，畫面聚焦單一目標；未選定時維持現有全選項預覽。
3. **C12「在試一次？」確認**：判定失敗後跳出詢問，取代目前「玩家自己決定要不要再拖一張」的隱性設計，讓失敗有明確的重試/離開分岔。
4. **C18③ 角色對話框連動**：判定結果目前只寫 Console 與彈窗，需要同時反映在**選項內文**——`Phase4c_CardPlay.md` 註明這需要選項本身有文字元件，屬於 Phase 6 多選項對話會一起處理的範圍，若要在本輪提前做，需先決定「選項文字」現階段掛在哪個物件上。
5. **`EncounterUIController`**：把上述兩個 UI（目標選定、重試確認）與既有結束按鈕收攏成一個控制器，避免邏輯散落在 `ExploreStageController` 各處。

**驗收基準**：沿用 `Phase4c_CardPlay.md` §8 清單 + 新增「選定主要目標後 hover 其他選項無反應」「失敗後彈出重試詢問，YES 可再出牌、NO 直接走離開流程」兩項。

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

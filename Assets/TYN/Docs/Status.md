# ELDRITCH_MILE — 整合進度總覽

> 更新：2026-08-08 · 範圍限於 `Assets/TYN/`
>
> 設計依據：[SceneConsolidationPlan.md](SceneConsolidationPlan.md)（v4）

---

## 一句話現況

**架構重建已完成 Phase 0–3，最大的病根「狀態隨場景死亡」已根治。Phase 4（探索）程式完成、編輯器設定進行中，打牌環節 UI 是目前最大的缺口。**

---

## 1. 已完成

### Phase 0 — 清場 ✅

| 項目 | 結果 |
|---|---|
| `Assets/_Recovery/` | 已刪除（43 個垃圾場景、21MB，git 可還原） |
| 腳本封存 | 21 → 現 23 個移入 `_Archive/Scripts/` |
| `TESTING/` 資料夾 | 已解散，素材歸位 |
| `codes/` 資料夾 | 已解散 → `UI/Scripts/` |
| `Card_data` v1/v2 | 已封存，只留 v3 |

### Phase 1 — Core 層 ✅

`Assets/TYN/Core/` 22 個檔案。命名空間 `EldritchMile.Core`。

流程總管、資料層、轉場、判定服務、屬性系統、對話框、彈窗排隊全部到位。
**遺產機制的切分點已預留**（`RunContext.CreateNew` / `ContributeToMeta` ↔ `MetaProgressData`）。

### Phase 2 — 主選單 ✅

`Stage_Menu.prefab` + `MenuStageController`。START 接 `GameFlowManager.StartNewRun()`。
`MenuScene.unity` 已封存。

### Phase 3 — 地圖改常駐覆蓋層 ✅

| 檔案 | 職責 |
|---|---|
| `Core/MapGenerationSettings.cs` | 生成參數 SO |
| `Core/MapGenerator.cs` | 純邏輯生成，不碰 UI |
| `Map/MapView.cs` | 繪製 + 棋子移動，繼承 `MapOverlayController` |
| `Map/MapNodeUI.cs` | 單一節點 |

592 行的 `PerspectiveMapGenerator` 拆成四塊並封存。`UIScene.unity` 已封存。
節點進場改為**由底層往上逐層淡入**，連線可切三種顯示模式。

### Phase 4a / 4b / 4d — 探索 Stage 🔶 程式完成，編輯器設定中

`Explore/Scripts/` 8 個檔案，命名空間 `EldritchMile.Explore`。

已驗證可運作（來自 Editor.log）：
```
[房間] Room_Event 填入 4 / 4 個位子
[Run] 獲得道具：key_warehouse
[Run] 消耗道具：key_warehouse
```

### 檔案空間整理 ✅

| 項目 | 前 | 後 |
|---|---|---|
| `Explore/` | 117 MB | 24 MB |
| `map_漁村.png` | 9000×9000, 79 MB | 2048×2048, 6 MB（原圖備份在 repo 外） |
| Build Settings 啟用場景 | 4 | **1** |

---

## 2. 程式碼現況

| 位置 | 檔案 | 命名空間 |
|---|---:|---|
| `Core/` | 22 | `EldritchMile.Core` |
| `Map/` | 3 | `EldritchMile.Map` |
| `Explore/Scripts/` | 8 | `EldritchMile.Explore` |
| `Stages/` | 1 | 全域 |
| **合計** | **34 個 / 約 4400 行** | |
| `_Archive/Scripts/` | 23 個 / 2093 行 | 全域（仍會編譯） |

每次改動都以 `dotnet build` 對 Unity 6000.4.1f1 的真實組件驗證，**目前 0 error 0 warning**。

---

## 3. 企劃約束的實作狀態

| # | 約束 | 狀態 |
|---|---|---|
| C1 | 地圖下拉（常駐覆蓋層） | ✅ |
| C2 | 下拉是自動的 | ✅ |
| C3 | 探索是事件節點的內容 | ✅ |
| C4 | 統一判定服務 | ✅ `ProbabilityCheck` |
| C5 | 多張機率不疊加 | ✅ 由 C18 取代定義 |
| C6 | 寶箱隨機位置／角度 | ✅ `SpawnSlot` + 隨機外觀圖 |
| C7 | 鑰匙才能開 | ✅ 已實測通過 |
| C8 | 抓取手勢兩段式 | 🔶 游標切換完成，**確認點擊的語意待定** |
| C9 | 新手介紹 | ⬜ **Romtyui 負責**，我方只保留位置 |
| C10 | 正交 2D/2.5D | ✅ |
| C11 | 戰鬥 Prefab | ⬜ 未接（Phase 5） |
| C12 | 失敗可「在試一次」 | 🔶 資料層完成，**UI 未做** |
| C13 | 「要探索其他的東西嗎？」 | ✅ |
| C14 | 兩段式離開 | ✅ `BookmarkHover` + `TwoStageConfirm` |
| C15 | 商店可重複購買 | ⬜ 未做（Phase 6） |
| C16 | 特殊事件給神牌 | ⬜ 未做（Phase 7） |
| C17 | 屬性相剋 + hover 全選項預覽 | 🔶 資料層與廣播器完成，**UI 未做** |
| C18 | 打牌回合制 + 衰減 + 結束鈕 | 🔶 邏輯完成，**UI 未做** |
| C19 | 相剋 1× / 0.5× / 0× | ✅ |

**✅ 10　🔶 4　⬜ 5**

---

## 4. 尚未完成的功能

### Phase 4c — 打牌環節 UI 🔴 最大缺口

`DialogueEncounterController`（回合、衰減、結束鈕、主要目標）**邏輯已完成**，缺的全是畫面：

| 待做 | 說明 |
|---|---|
| 卡牌拖曳 | 重寫一套（封存的兩套平行實作都不用） |
| hover 全選項預覽 | 接 `HoverPreviewBroadcaster`，對應草圖的 `A 50 / B 50 / C 50` |
| 主要目標選定 UI | 選定後預覽收斂到單一目標 |
| 「在試一次？」確認 | C12 的重試迴圈出口 |
| 結束打牌按鈕 | C18⑥。**成功也不可自動結束**（蓄意失敗是合法策略） |
| 選項預覽標籤 | `ChestInteractable.previewLabel` 等待接線 |

### Phase 5 — 戰鬥接入

- 從 Romtyui 的 Scene 打包成 prefab（我方例行動作，非等待交付）
- 包一層 `BattleStageController`
- **需向 Romtyui 要一個 `OnBattleEnded` 事件** —— 否則分不出勝／敗／逃跑，做不出 Game Over

### Phase 6 — 對話與商店

- `DialogueData`(SO) + `DialogueStageController`；選項改實作 `IProbabilityTarget` 才吃得到屬性預覽
- `answer_1~3` 正好對應草圖三列
- 商店（C15）：購買後留在商店可繼續買

### Phase 7 — 特殊事件給神牌

範圍很小：神牌是戰鬥牌（Romtyui 領域），我方只負責「什麼時候給、給哪一張」→ 加進 `RunStateManager.savedDeck`。

---

## 5. 待決事項

| # | 問題 | 最晚需要 | 暫行做法 |
|---|---|---|---|
| Q7a | 屬性的**實際名稱與數量** | Phase 4c | `AttrA~AttrD` 佔位，改名不影響邏輯 |
| Q11 | 衰減級距：線性還是比例？ | Phase 4c | 線性，級距 = 1/手牌數 |
| Q12 | 主要目標選定後能否更換？ | Phase 4c | 可自由更換，各目標獨立衰減 |
| Q13 | 打牌結束後未使用的手牌怎麼處理？ | Phase 4c | 棄掉，下個事件重抽 |
| C8 | 抓取是「hover+單擊」還是「真的點兩下」？ | Phase 4c | 目前單擊 |
| — | 遺產機制的具體規則 | 未排期 | 切分點已預留，內容留空 |

---

## 6. 建議的下一步順序

1. **先 commit** —— 目前累積 174 rename + 108 delete + 39 新檔**尚未提交**。這是整個重構的成果，建議先存檔再往下做。
2. **把 Phase 4a 的編輯器設定收尾** —— 修 `Popup_Panel`/`ExitTag` 的 RectTransform 死值、接 `DialogueBoxUI`、跑通「進房間 → 點東西 → 離開 → 地圖下拉」。
3. **Phase 4c 打牌 UI** —— 遊戲最核心的玩法，也是目前唯一還沒有畫面的系統。
4. **Phase 5 戰鬥接入** —— 可與 4c 平行；記得先跟 Romtyui 講 `OnBattleEnded`。
5. Phase 6 → Phase 7。

---

## 7. 技術債與風險

| 項目 | 影響 | 處理時機 |
|---|---|---|
| **git 未提交** | 高 —— 174 個 rename 若遺失要重做 | **立即** |
| `_Archive/` 仍在編譯範圍 | 中 —— 2093 行死碼跟著編譯，且 13 個被保留腳本間接依賴 | Phase 4c/6 重寫完該區塊後 |
| `MapBannerUI.ShowEndGame` 已棄用 | 低 —— 全專案最後一個 `SceneManager` 呼叫 | 與 `PerspectiveMapGenerator` 一起刪 |
| `ExploreScene.unity` 未封存 | 低 | Phase 4 驗收後 |
| 5 個舊房間 prefab 掛封存腳本 | 中 —— 執行會 NullReference | 用不到就別開；要用再轉換 |
| 屬性名稱未定案 | 低 —— SO 承載，改名不動程式 | Phase 4c |

### 反覆踩到、值得記住的三個坑

1. **RectTransform 死值**：從別的場景複製 UI 過來，原本被 Unity 驅動的值（root Canvas、stretch 佈局）會變成 `Scale 0`、寬高 0。物件在 Hierarchy 看得到、`SetActive` 也成功，但畫面上完全不存在。`StageHost` 與 `PopupService` 已加自動偵測。
2. **`using` 的位置**：檔案最上方的 `using` 註冊在全域層，而同一層「宣告」永遠贏過「using 匯入」。與未封存的舊全域型別同名時會**綁到舊型別且編譯得過**，只在型別轉換時才爆。解法是把 `using` 寫進 `namespace` 內部。
3. **Unity Inspector 的 List `+` 會零填充**，不套用 C# 欄位初始值。`weight = 1f` 會變成 `0`，導致條目被靜默跳過。`RoomLibrary` / `RoomContentData` 已加針對性警告。

---

## 8. 工具

**UnityMCP 已連線**（2026-08-08）。之後可直接讀 Hierarchy、Inspector 欄位、Console，不必再 parse `.unity` 的 YAML 或 Editor.log。

> 連線注意：專案路徑含中文「文件」，必須設 `PYTHONUTF8=1`，否則永遠偵測到 0 個 instance。

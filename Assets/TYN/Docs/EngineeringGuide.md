# ELDRITCH_MILE — 工程規則書（Assets/TYN/ 範圍）

> 目的：把 `SceneConsolidationPlan.md` v4 執行過程中已經穩定下來的慣例寫成規則，讓之後新增的程式碼（不管是你自己還是我）風格一致，減少重讀舊 code 找慣例的成本。
> 範圍限於 `Assets/TYN/`。`Assets/Romtyui/` 是 Romtyui 的地盤，不套用本規則書，也不要跨界修改（見 folder-ownership 慣例）。

---

## 1. 命名空間與資料夾分工

| 命名空間 | 資料夾 | 用途 | 為什麼 |
|---|---|---|---|
| `EldritchMile.Core` | `Core/` | 流程總管、資料層、判定服務、屬性系統、共用 UI 基礎設施 | 避免與尚未封存的舊全域型別（如 `MapData`）撞名 |
| `EldritchMile.Map` | `Map/` | 地圖覆蓋層的繪製與節點 UI | 同上，且與地圖資料層分離 |
| `EldritchMile.Explore` | `Explore/Scripts/` | 探索 Stage 專屬邏輯（房間、互動物件、卡牌拖曳） | 探索是 Stage 的一種，不屬於 Core |
| 全域（無 namespace） | `Stages/` | 各 `StageController` 子類別 | Stage 層不會撞名，全域反而在 Inspector 綁定時少一層阻力（既定決策，見 Phase2 文件） |

**新增檔案時先問**：這是「規則引擎/跨 Stage 共用」還是「單一 Stage 的畫面邏輯」？前者進 `Core/`，後者進對應 Stage 的資料夾。不要因為圖方便把 Stage 專屬邏輯塞進 `Core/`。

### `using` 位置（踩過的坑，見 [SceneConsolidationPlan.md](SceneConsolidationPlan.md) §6 Phase 3）

C# 名稱解析中「同一層的宣告」永遠贏過「using 匯入」。若新檔案寫在全域命名空間並在檔案最上方 `using EldritchMile.Core;`，一旦與尚未封存的舊全域型別同名，會**靜默綁定到舊型別且編譯得過**，只在型別轉換時才報錯。

**規則**：`using` 一律寫在 `namespace { }` 內部，不要寫在檔案最上方（跨 `namespace` 的除外，例如 `using System;`）。

---

## 2. 腳本與檔案命名

| 對象 | 慣例 | 範例 |
|---|---|---|
| Stage 控制器 | `XxxStageController` | `ExploreStageController`、`MenuStageController` |
| Stage prefab | `Stage_Xxx.prefab` | `Stage_Explore.prefab`、`Stage_Menu.prefab` |
| 互動物件 | `XxxInteractable`，實作 `IInteractable`（需判定者再實作 `IProbabilityTarget`） | `ChestInteractable`、`InspectableInteractable` |
| 互動物件 prefab | 小寫底線風格 + 語意後綴 | `chest_Direct.prefab`、`chest_RequiresKey.prefab` |
| ScriptableObject 設定 | `XxxData` / `XxxSettings` | `RoomContentData`、`MapGenerationSettings`、`AttributeChartData` |
| 服務/單例（`Instance` 靜態屬性） | 名詞，不加 `Manager` 尾綴的視情況（`GameFlowManager` 是例外，其餘新的服務類別優先用職責名） | `ProbabilityCheck`、`ScreenFader`、`PopupService`、`HoverPreviewBroadcaster` |
| 介面 | `IXxx`，一個介面一個檔案 | `IInteractable`、`IProbabilityTarget` |
| 卡牌相關（探索） | `XxxExplore` 後綴，與戰鬥端 `Romtyui` 的同名概念區隔 | `CardDataExplore`、`CardViewUIExplore`、`ExplorationDeck` |

**C# 內部風格**（取自 `GameFlowManager.cs` / `EncounterTargetView.cs` 等既有程式碼）：

- `public` 欄位若要在 Inspector 顯示，直接 `public`，用 `[Header("分組名")]` + `[Tooltip("說明")]` 分組，**不要**額外包一層 `Get/Set` 除非有邏輯。
- 唯讀對外狀態用 `public X Y => field;` 或 `{ get; private set; }`，不要暴露可寫的 `public` 欄位給外部狀態（例如 `CurrentStage`、`IsMapOpen`）。
- 單例一律 `public static X Instance { get; private set; }`，`Awake()` 裡處理重複實例（`Destroy(gameObject)` 並 `return`）。
- 中文註解只寫「為什麼」，不寫「做什麼」——`EncounterTargetView.cs` 的 `FitToSprite` 是好例子：解釋為什麼不用 `preserveAspect`，而不是重複「這裡在調整尺寸」。
- 容易做錯、之前真的做錯過的地方，用 `⚠️` 開頭的註解標出來（例如 raycast 必須用 UI 的 `Image.raycastTarget` 而非 `Collider2D`）。這是專案已經在用的慣例，延續它。

---

## 3. 資料夾結構（現況即目標，見附錄 B）

```
Assets/TYN/
├── Core/            ← 流程總管、資料層、判定服務、屬性系統、共用 UI 基礎設施
├── Stages/          ← Stage_*.prefab（全域命名空間）
│   └── Battle/      ← 從 Romtyui Scene 打包來的戰鬥 prefab + 我方 wrapper
├── Map/             ← MapOverlay 相關
├── Menu/            ← 選單素材與腳本
├── Explore/
│   ├── Scripts/     ← EldritchMile.Explore 命名空間
│   ├── CARD/        ← 探索卡牌資料與 UI
│   └── PREFAB/Interactables/ ← 互動物件 prefab
├── UI/              ← 共用 UI 素材
├── ART/             ← 美術資產
├── Docs/            ← 設計文件（本文件與 RoadmapNext.md 也在這）
├── EventScene.unity ← 唯一場景
└── _Archive/        ← 封存腳本、舊場景、舊資料
```

**新建資料夾前先確認**：這個用途在上表有沒有位置？`TESTING/` 這類「暫時放一下」資料夾已被明確判定是遺留混亂的主因之一（見 legacy-cleanup 慣例），**不要再開**。臨時測試素材直接放進對應的正式資料夾，用檔名或 `_test` 前綴區分，事後刪除比事後搬移容易。

---

## 4. 程式碼架構原則

### 4.1 單一真相來源（Single Source of Truth）

專案吃過兩次虧：舊版判定公式兩套不一致（`DialogueOptionInteractable` 乘法、`EnemyInteractable` 減法）、舊版黑幕兩份互搶。**規則**：任何「預覽」與「實際生效」必須呼叫同一個計算函式。`ProbabilityCheck.CalculateRate`（純計算）被 hover 預覽與 `Roll`（擲骰）共用是範例——新增類似的「顯示值 vs 實際值」場景時比照辦理，不要各算各的。

### 4.2 規則與畫面分離

`DialogueEncounterController` 是規則引擎（回合、衰減、結束判定），UI 層（`ExploreCardDrag`、`ExploreHandUI`）只負責「畫出來」與「轉交玩家操作」，不做判斷。好處：像「判定成功不可自動結束」這種容易做錯的規則，只有一個地方可能寫錯。**新功能若同時牽涉規則與畫面，先問规则该放哪一層，不要圖方便寫進 UI 腳本的 callback 裡。**

### 4.3 流程總管是唯一入口

- 任何 Stage 切換、地圖開合，一律經過 `GameFlowManager`。**Stage prefab 內部不得出現 `SceneManager.*` 呼叫**——這是目標架構「單一場景」的核心，破例一次就會重新長出硬編碼場景名。
- Stage 結束是**自動**回報（`NotifyStageComplete`），不是玩家按鈕直接觸發下一步。玩家操作（按結束鈕、點離開）觸發的是「這個 Stage 的邏輯結束」，由 Stage 自己決定何時呼叫 `NotifyStageComplete`，不要讓玩家操作直接跳過總管。

### 4.4 廣播優於輪詢/反查

`HoverPreviewBroadcaster` 用廣播讓所有選項同時收到 hover 預覽，而非每個選項自己 `FindObjectsOfType` 或反查某個 Manager。舊架構的病根之一就是 `ExplorationManager` 反查 `PerspectiveMapGenerator.Instance`——**新程式碼裡看到「A 需要跑迴圈找 B」或「B 需要反查 A 的內部狀態」，優先考慮改成事件/廣播。**

### 4.5 場景物件 / prefab 的引用邊界

Stage 是動態生成的 prefab，**prefab 無法在 Inspector 引用場景物件**（反之場景物件也不能引用 prefab 內部）。專案的解法是：常駐於場景的服務（`DialogueEncounterController`、`ExploreHandUI` 所在的 `EncounterUI`）一律透過 `Instance` 靜態屬性讓 Stage 端在執行期解析，Inspector 欄位留空即可。新增「Stage 需要用到常駐 UI/服務」的情境時比照此模式，不要嘗試用 Inspector 硬拖。

### 4.6 UI 開關的擁有權（`UIPanel` 三分法）

見 [Phase4a_ExploreStage.md](Phase4a_ExploreStage.md) 步驟 1.5。每個 UI 只能有一個擁有者：

- **Panel**：屬於某個 Stage，換 Stage 就該消失 → 標 `Panel`，設 `Visible In Stages`，交給 `UIDirector`。
- **Dialog**：疊在當前畫面上、關掉回到原本畫面 → 標 `Dialog`，進 `UIDirector` 的堆疊，換 Stage 時自動全收。
- **Widget**：已經有專屬控制器在管（如 `dialogbox` 由 `DialogueBoxUI` 管）→ 標 `Widget`，`UIDirector` 完全不碰。

**規則**：新增 UI 前先判斷屬於哪一類。已經有專屬控制器的一律 `Widget`，不要讓 `UIDirector` 和專屬控制器同時搶著開關同一個物件——那會導致閃爍或狀態不同步。

### 4.7 資料與行為分離、跨場景狀態集中管理

`RunContext`（純資料類別，不繼承 `MonoBehaviour`）持有一整場 run 的狀態；`GameFlowManager` 持有 `RunContext` 並驅動流程。**新的跨 Stage 狀態（例如未來的商店庫存、對話進度）優先考慮加進 `RunContext`，而不是散落在各 Stage 的 `public` 欄位裡**——舊架構的病根 1 就是 `MapData` 曾經是 `PerspectiveMapGenerator` 的 `public` 欄位，隨場景卸載歸零。

`RunContext` 已預留跨輪迴（`MetaProgressData`）的切分點（`CreateNew` / `ContributeToMeta`），遺產機制相關的新欄位要先想清楚屬於「單場 run」還是「跨輪迴」，別混在一起。

### 4.8 封存不刪除

淘汰的程式碼移到 `Assets/TYN/_Archive/`，**必須連同 `.meta` 一起搬**（GUID 因此保留，場景引用不會斷）。`_Archive/` 內容不得被任何新場景或 prefab 引用。若某個封存腳本被保留腳本間接依賴，先在 `Core/` 建好替代介面/型別，讓依賴者先轉移，再封存被依賴的舊檔——不要因為看類別名覺得「應該可以直接封存」就跳過依賴檢查（`ICardInteractable` 定義在 `EnemyInteractable.cs` 裡就是活生生的例子）。

---

## 5. 與 Unity MCP 搭配的工作流建議

> 連線前提：`.mcp.json` 需設 `PYTHONUTF8=1`（專案路徑含中文「文件」），且 Unity 端 `MCPForUnity.UseHttpTransport` 需與 `.mcp.json` 的 transport 一致（目前是 stdio，該項應為 `false`）。這兩者任一沒對齊都會表現成「偵測到 0 個 instance」且沒有明顯錯誤訊息。

### 適合用 MCP 做的事（比手動操作 Editor 快）

- **讀 Hierarchy / Inspector 欄位驗證接線**：與其憑文件推測某個欄位有沒有拖好，直接查詢當前場景物件的元件與欄位值，尤其是 Phase 文件裡「常見錯誤」表格列出的那些容易漏接的欄位（`PopupService.Dialogue Box`、`ExploreStageController.Exploration Deck` 等）。
- **讀 Console**：驗收清單裡大量依賴特定 log 字串（如 `[房間] Room_xxx 填入 N / M 個位子`），比起要求你手動複製貼上，直接查詢 Console 更快確認。
- **腳本編譯狀態確認**：改完 Core 或 Explore 的腳本後，用 MCP 確認 0 error 0 warning，取代手動切回 Editor 看狀態列。
- **驗證 RectTransform 死值**：這是專案反覆踩到的坑（Scale 0 / 寬高 0），直接查詢數值比肉眼在 Scene 視圖找更可靠。

### 不適合、或需要你手動確認的事

- **視覺與手感判斷**（動畫節奏、UI 排版是否擁擠、美術素材是否對齊）——這些交給你在 Editor 內肉眼確認，MCP 只負責把「數值對不對、有沒有接線」的機械檢查做掉。
- **Play Mode 互動操作**（實際拖卡牌、點寶箱走一輪驗收清單）——MCP 可以讀狀態，但實際操作與「這個手感好不好」的判斷仍由你在 Editor 內執行。
- **prefab 結構性變更**（新建 prefab、拉 Stage 骨架）——這類一次性的編輯器操作，用 MCP 腳本化風險（誤改結構不好復原）通常大於手動拖拉的效率損失，除非是重複性高的批次操作（例如幫 N 張卡牌資產批次設定 `Attribute` 欄位）才值得寫腳本。

### 建議的分工節奏

1. 你在 Editor 內完成一批接線/建物件。
2. 我用 MCP 讀取驗證（欄位有沒有拖對、Console 有沒有預期的 log、有沒有編譯錯誤）。
3. 有問題我直接指出「哪個物件的哪個欄位不對」，你在 Editor 內修，不用來回猜。
4. 批次性、規則明確的操作（例如卡牌資產屬性批量設定、依 Phase 文件檢查清單逐項核對）可以考慮讓我直接透過 MCP 執行，減少手動重複勞動。

---

## 6. 與 Romtyui 的邊界（重申）

- 只在 `Assets/TYN/` 內新增或刪除檔案。
- 需要戰鬥端配合的行為（例如 `OnBattleEnded` 事件），先在對話中提出讓你去跟 Romtyui 協調，不直接改 `Assets/Romtyui/`。
- 戰鬥 prefab 一律由我方打包後包一層 wrapper（掛在 prefab **外層**空物件），不寫進他的 prefab 內容，確保下次重新打包可以直接覆蓋不遺失接線。

# ELDRITCH_MILE — 遊戲流程整合設計文件

> 目標：把 YNT 負責的「主選單 / 大地圖 / 探索 / 事件對話」流程，整合進 **單一場景 EventScene**。戰鬥由 Romtyui 以 Prefab 交付，同樣掛在此場景內 —— 專案最終只剩一個場景。
>
> 版本：**v4** · 2026-08-08 · 撰寫範圍限於 `Assets/TYN/`
>
> **v4 修訂**（依據開發者對 §10 待決事項的答覆）：
> - ✅ **Phase 0 已執行完畢**（2026-08-08）。`_Recovery` 已刪、21 個腳本已封存、`TESTING/` 已解散
> - **打牌環節的互動模型定案** → 新增 C18。原「一次投多張」的假設作廢，改為**每回合出一張、可連續出、成功率逐次衰減、玩家按結束鈕收尾**
> - **相剋倍率定案為 1× / 0.5× / 0×**（無 2×，是懲罰制而非獎勵制）→ C19
> - 「離開」確認的第一段**已存在**：`ExitTag` 掛的 `BookmarkHover` 已實作 hover 下拉，缺的只有 click 確認 → C14 更新
> - 神牌改列為**戰鬥牌**（Romtyui 領域），我方只負責在特殊事件中授予 → Phase 7 大幅簡化
> - 新手介紹由 Romtyui 製作，我方只保留位置 → Phase 6 移除該項
> - 戰敗 = 死亡進入下個輪迴，**遺產機制**列為未來規劃 → Q4 答覆
>
> **v3 修訂**（依據流程圖右半部截圖與開發者指示）：
> - **基調轉向**：從「遷移既有程式」改為「**以新規劃重建，既有程式大量封存**」。新增 §2.4 全資產去留盤點 → 36 個腳本中 **21 個封存**
> - 補完流程圖右半部：新增 C12–C16（重試迴圈、繼續探索迴圈、兩段式離開確認、商店重複購買、神牌），並修正 C2 的語意
> - 新增 §4.5 **屬性相剋系統與 hover 全選項預覽**（C17）
> - §7 改為「每階段自行從 Romtyui 的 Scene 打包」，非正式交付
> - §10 待決事項 Q1／Q3 已由截圖解答，改列屬性系統的新問題
>
> **v2 修訂**：架構修正為 Stage / MapOverlay 兩軸模型；戰鬥改 Prefab；新增 `ProbabilityCheck`。

---

## 1. 範圍與原則

**在範圍內**
- `Assets/TYN/` 底下所有流程腳本、場景、prefab
- 與戰鬥的「交接介面」設計（只定義契約，不改對方程式）

**不在範圍內**
- `Assets/Romtyui/` 內任何檔案 — 戰鬥是隊友的地盤
- 戰鬥內部邏輯、卡牌數值平衡

**四條原則**
1. **以新規劃為主，既有程式預設封存**（v3 調整）。專案改動多次、遺留嚴重，重寫的成本普遍低於改造。只有「明確有用且與新架構相容」的才保留 —— 判斷結果見 §2.4。
2. **新增優先於修改**：Core 層全部是新檔案，讓每個階段都能安全回滾。
3. **一次只做一個 Stage**：每階段結束遊戲必須能從頭跑到尾，不接受「中間三天不能玩」。
4. **封存不刪除**：所有淘汰物移到 `Assets/TYN/_Archive/`（含 `.meta`），確認穩定兩週後再真的刪。`_Archive/` 內容不得被任何場景或 prefab 引用。

---

## 2. 現況盤點

### 2.1 場景

| 場景 | 檔案 | 實際職責 | 進入方式 | Build Settings |
|---|---|---|---|---|
| MenuScene | `TYN/MenuScene.unity` | 主選單、液態背景、沸騰邊框 | 啟動場景 | ✅ index 0 |
| UIScene | `TYN/UIScene.unity` | **實際上是大地圖**（名稱誤導） | `LoadScene("UIScene")` | ✅ index 1 |
| ExploreScene | `TYN/ExploreScene.unity` | 房間探索 + 探索卡牌 + 對話/Loot 彈窗 | 地圖 additive | ✅ index 2 |
| BattleScene_v1 | `TYN/TESTING/BattleScene_v1.unity` | 戰鬥 | additive，卸載 explore | ✅ index 3 |
| BattleScene | `TYN/TESTING/BattleScene.unity` | 舊版戰鬥 | — | ⬜ 停用 |
| SampleScene | `Romtyui/scene/SampleScene.unity` | 隊友測試 | — | ⬜ 停用 |
| **EventScene** | `TYN/EventScene.unity` | **孤兒**：對話框 + 立繪 + 3 選項 + `ENCOUNTER/EXPLORE/GOD/MAP/SHOP` 測試按鈕 | 無 | ❌ 不在清單 |

EventScene 目前只掛了 `CursorManager`、`CursorInteractableObject`、`BookmarkHover` 三個腳本，沒有任何流程邏輯 — 是一張**乾淨的 UI 草稿**，正好適合升格成主場景。

### 2.2 現行流程圖

```
MenuScene
   │  START 按鈕 → SceneLoader.LoadUIScene() → LoadScene("UIScene")   ← 硬編碼字串
   ▼
UIScene (大地圖)
   │  PerspectiveMapGenerator.OnNodeClicked()
   │    → MoveAndLoadRoutine()：棋子走過去 → 自己的黑幕淡入
   │    → LoadSceneAsync(node.targetSceneName, Additive)
   │    → mapCamera.SetActive(false) / mapCanvas.SetActive(false)
   ▼
ExploreScene (additive，UIScene 仍在記憶體)
   │  ExplorationManager.Start() → 反查 PerspectiveMapGenerator.Instance 拿節點資料
   │  Instantiate(roomPrefab) → MoveGameObjectToScene(自己的場景)
   │  ├─ Door.OpenDoor() → ExplorationManager.ExitExploreScene()
   │  │     → PerspectiveMapGenerator.WakeUpMapAndUnload()  → 回地圖
   │  └─ EnemyInteractable.TriggerBattle()
   │        → PerspectiveMapGenerator.TransferToBattleScene()
   ▼
BattleScene_v1 (additive，卸載 ExploreScene)
   │  BattleToMapBridge.Update() 每幀輪詢 battleManager.gameObject.activeSelf
   │    由 true → false 時判定「戰鬥結束」
   │    → PerspectiveMapGenerator.WakeUpMapAndUnload()
   ▼
回到 UIScene 地圖
```

### 2.3 關鍵腳本職責

| 腳本 | 目前職責 | 問題 |
|---|---|---|
| `PerspectiveMapGenerator.cs` | 地圖資料(`MapData`)＋地圖 UI 生成＋場景載入卸載＋黑幕淡入淡出＋結局判定 | **一人分飾五角**，事實上的 GameManager 卻住在會被卸載的場景裡 |
| `ExplorationManager.cs` | 房間生成＋探索黑幕＋反查地圖要資料 | 反向依賴 `PerspectiveMapGenerator.Instance` |
| `UIManager.cs` | 探索用彈窗 / Loot / 排隊系統 | 沒有 `DontDestroyOnLoad`，隨場景死 |
| `BattleToMapBridge.cs` | 偵測戰鬥結束 | 每幀輪詢 `activeSelf`，最脆的一環 |
| `MapBannerUI.cs` | 標題橫幅、結局按鈕 | 內部直接 `LoadScene(menuSceneName)`，UI 元件擅自做流程決策 |
| `SceneLoader.cs` | START 按鈕 | 硬編碼 `"UIScene"` |
| `CursorManager.cs` | 游標狀態 | ✅ 已有 `DontDestroyOnLoad`，行為正確 |
| `RunStateManager.cs` (Romtyui) | 戰鬥端存檔 | ✅ 已有 `DontDestroyOnLoad`，可沿用 |

---

### 2.4 資產去留盤點（v3 新增）

`Assets/TYN/` 共 36 個腳本、3023 行。逐一判定如下。

#### ✅ 保留（6 個，直接沿用）

| 檔案 | 行數 | 保留理由 |
|---|---:|---|
| `codes/CursorManager.cs` | 105 | 狀態機設計正確、已有 `DontDestroyOnLoad`。**C8 抓取手勢與 C14 兩段式離開正好需要它** |
| `codes/CursorInteractableObject.cs` | 51 | 配合上者，無流程耦合 |
| `codes/BookmarkHover.cs` | 49 | **關鍵保留**。EventScene 的 `ExitTag` 掛的就是它，已實作 C14 的第一段（hover → 從上緣滑下）。其 `hiddenY`/`shownY` + Lerp 手法**可直接複用到 MapOverlay 的「地圖下拉」** → §4.6 |
| `TESTING/Menu_LiquidBG/BGLiquidController.cs` | 86 | 純視覺 shader 控制，無流程耦合 |
| `TESTING/Menu_BoilFrame/BoilFrameEffect.cs` | 82 | 同上 |
| `TESTING/Menu_TrickButton/TrickButton.cs` | 42 | 純 hover 文字效果 |
| `Explore/INTERACTION/images/SpriteAnimator.cs` | 53 | 純工具類 |

#### 🔧 改寫（8 個，骨架可用、邏輯重做）

| 檔案 | 行數 | 保留什麼 / 砍什麼 |
|---|---:|---|
| `Explore/MAP/.../PerspectiveMapGenerator.cs` | 592 | **保留**：地圖生成演算法、棋子移動與 head-bobbing 動畫。**砍掉**：資料層、5 個轉場協程、`lastLoadedSceneName`、結局判定 → 預計剩約 250 行 |
| `Explore/MAP/.../PerspectiveNode.cs` | 79 | 節點 UI，配合新資料層調整 |
| `Explore/MAP/.../MapBannerUI.cs` | 81 | 拿掉 `SceneManager.LoadScene` 後可用 |
| `Explore/CARD/Scripts/ExplorationDeck.cs` | 99 | 牌堆邏輯乾淨，加屬性欄位即可 |
| `TESTING/RCard/CardDataExplore.cs` | 59 | 加 `attribute` 欄位（§4.5）。註解裡「補回遺失的」字樣可清掉 |
| `TESTING/RCard/CardVisualDataExplore.cs` | 16 | 加屬性圖示 |
| `TESTING/RCard/CardViewUIExplore.cs` | 134 | 卡面顯示，需加屬性標示與 hover 觸發 |
| `Explore/CARD/Scripts/ExplorationHandUIController.cs` | 73 | 手牌排版，需接新的 hover 廣播 |

#### 📦 封存（21 個）

| 檔案 | 行數 | 封存理由 |
|---|---:|---|
| `Explore/Scripts/ExplorationManager.cs` | 159 | **開發者指名**。進場 FOV 效果在正交下失效（C10）、反查 `PerspectiveMapGenerator.Instance`、職責混亂 |
| `Explore/Scripts/RoomController.cs` | 52 | **開發者指名**。靜態計數與 C6 的 spawn slot 不相容 |
| `TESTING/EnemyInteractable.cs` | 107 | **開發者指名**。判定公式（減法懲罰）與企劃不符；⚠️ **`ICardInteractable` 介面竟定義在此檔內**，拆解需注意順序 |
| `TESTING/BattleToMapBridge.cs` | 60 | 每幀輪詢 `activeSelf` |
| `TESTING/Menu_TrickButton/SceneLoader.cs` | 10 | 硬編碼場景名，整個功能被 `GameFlowManager` 取代 |
| `DialogueOptionInteractable.cs` | 104 | 判定公式（乘法倍率）與屬性相剋系統不相容 |
| `Explore/CARD/Scripts/ExplorationInteractableTarget.cs` | 42 | ⚠️ **等同死碼**：`CanAccept()` 收 `CardData`（Romtyui 的戰鬥卡型別）而非 `CardDataExplore`，型別根本對不上，執行時被 `CardExplorationManager` 明文跳過 |
| `TESTING/RCard/CardDragUIExplore.cs` | 244 | **與 `ExplorationCardDragUI` 功能重複**，二選一（見下方說明） |
| `Explore/CARD/Scripts/ExplorationCardDragUI.cs` | 149 | 同上，二選一 |
| `TESTING/RCard/CardHoverUIExplore.cs` | 32 | 幾乎整份被註解掉的空殼；hover 預覽將全新重做（§4.5） |
| `Explore/Scripts/Door.cs` | 21 | 重寫為 `ExitTag`（兩段式確認，C14） |
| `Explore/Scripts/MapNodeExplore.cs` | 29 | `targetSceneName` 欄位作廢，改由 `nodeType` 驅動；重建為新的節點資料 |
| `Explore/INTERACTION/UIManager.cs` | 92 | 排隊系統概念可參考，但需配合屬性預覽重做 |
| `Explore/INTERACTION/InspectableObject.cs` | 61 | 由新的 `IInteractable` 體系取代 |
| `Explore/INTERACTION/ContainerObject.cs` | 50 | 同上 |
| `Explore/CARD/Scripts/CardExplorationManager.cs` | 136 | 打牌流程需整個配合屬性系統重做 |
| `Explore/CARD/Scripts/ExplorationCardResolveContext.cs` | 25 | 效果系統重做 |
| `Explore/CARD/Scripts/ExplorationCardEffectData.cs` | 6 | 同上 |
| `Explore/CARD/Scripts/ExploreInteractEffectData.cs` | 16 | 同上 |
| `Explore/CARD/Scripts/ExploreAddCardToDeckEffectData.cs` | 15 | 同上 |
| `Explore/CARD/Scripts/ExploreDrawCardsEffectData.cs` | 12 | 同上 |

**兩套卡牌拖曳系統**：`ExplorationCardDragUI`(149行，被 `ExplorationHandUIController` 使用) 與 `CardDragUIExplore`(244行，被 `CardHoverUIExplore` 使用) 是兩條獨立的平行實作，且都依賴 Romtyui 的 `HandFanLayout` / `TargetArrowUI`。因為拖曳邏輯必須配合屬性預覽重寫（hover 時要即時廣播機率），**兩套都封存，重寫一套**。

#### ⚠️ 封存的拆解順序

`ICardInteractable` 定義在 `EnemyInteractable.cs` 內，被 `DialogueOptionInteractable` 與 `CardExplorationManager` 引用。若直接封存 `EnemyInteractable` 會同時打斷另外兩個檔案。**正確順序**：

1. 先在 `Core/` 建立新的 `IInteractable`（§5.1）
2. 三個依賴者一次全部封存（它們本來就全在封存清單裡）
3. 最後封存 `EnemyInteractable.cs`

#### 其他資產

| 項目 | 處置 |
|---|---|
| `Assets/_Recovery/` | **刪除** — 43 個 `0 (N).unity`，21 MB，與遊戲無關 |
| `Assets/探索測試資料夾/` | 內含一份 docx，移入 `TYN/Docs/` |
| `TYN/TESTING/` 資料夾本身 | 解散 — 素材歸位到 `Menu/`、`Explore/`，腳本依上表處置 |
| `TYN/Explore/CARD/Card_data/v1`、`v2` | 封存，只留 `v3`（`v3` 是 0/20/40/60/80/100 完整階梯，與草圖標注一致） |
| `TYN/TESTING/BattleScene.unity`、`BattleScene_v1.unity` | 封存 — 戰鬥改由 Romtyui 端打包（§7） |

**淨效果**：36 個腳本 → 保留 7 + 改寫 8 = **15 個**，約 1400 行。砍掉約 1600 行遺留程式。

---

## 3. 問題診斷

### 病根 1 — 資料與畫面綁死，狀態隨場景死亡

`MapData`（整場 run 的節點拓撲、目前位置、走過的路徑）是 `PerspectiveMapGenerator` 的 `public` 欄位。一旦 UIScene 被卸載，整場進度歸零。

現在之所以還能玩，是因為 **UIScene 從頭到尾沒被卸載過** — 它靠 additive 疊場景硬撐。這代表所有 3D 房間、戰鬥場景都疊在地圖上面，光照、EventSystem、Audio Listener 全部有重複風險。

### 病根 2 — 黑幕控制權在兩個場景之間互搶

有兩份獨立黑幕：
- `PerspectiveMapGenerator.transitionFade`（UIScene）
- `ExplorationManager.fadeCanvasGroup`（ExploreScene）

交接靠「地圖淡到全黑 → 停手 → 探索場景載入後自己從全黑淡出」這種默契。任何一邊沒接到（例如 `ExplorationManager` 找不到節點資料而提早 return），畫面就永久黑屏。

### 病根 3 — 場景堆疊狀態只有一個字串

`PerspectiveMapGenerator.lastLoadedSceneName` 是唯一的堆疊記錄。`WakeUpMapAndUnload()` 直接卸載這個字串指向的場景。這代表：
- 只能有一層 additive
- Explore → Battle 時 `TransferRoutine()` 得手動搬移這個字串
- 任何 Explore 內開啟第三層（例如事件對話場景）都會直接壞掉 — 這正是 EventScene 一直沒能接進流程的原因

### 病根 4 — 與戰鬥的交接靠每幀輪詢

```csharp
// BattleToMapBridge.cs:36
bool isCurrentlyActive = battleManager.gameObject.activeSelf;
if (wasBattleManagerActive && !isCurrentlyActive) ReturnToMap();
```

依賴隊友腳本的「內部實作細節」（打贏後會 `SetActive(false)`）。對方任何一次重構都會無聲地打斷整條流程，而且**分不出勝、敗、逃跑**。

### 附註問題

- **Singleton 規則不一致**：`CursorManager` / `RunStateManager` 有 `DontDestroyOnLoad`；`ExplorationManager` / `UIManager` / `PerspectiveMapGenerator` 沒有。
- **場景名硬編碼散落 6 處**（見附錄 A）。
- **`MapBannerUI` 越權**：一個顯示橫幅的 UI 元件裡面寫著 `SceneManager.LoadScene()`。
- **`Assets/_Recovery/`**：43 個 `0 (N).unity` 垃圾場景，21 MB，與遊戲無關。

---

## 4. 目標架構

### 4.0 來自企劃流程圖的設計約束

以下約束直接取自 Miro 流程圖（2026-08-08 截圖），是架構的前提，不是可選項。

| # | 約束 | 出處 | 架構影響 |
|---|---|---|---|
| C1 | **「地圖下拉(自動)」出現 4 次，戰鬥線與事件線都是** | 流程圖各分支結尾 | 地圖是**從上方滑下的常駐覆蓋層**，不是要切換過去的畫面 |
| C2 | 下拉是**自動**的 —— 但**「離開」本身仍由玩家決定**（見 C14） | 同上，皆標注「(自動)」 | 精確語意：玩家確認離開後，地圖**自動**下拉，玩家不需再按一次「開地圖」。現行 `Door.OpenDoor()` 是單擊即走，缺少確認步驟 |
| C3 | 事件 → **探索** → 開寶箱/開鎖/點路人/遇到人/商店 | 「一般事件」框 | 探索是**事件節點的內容**，不是與事件平行的模式 |
| C4 | 開鎖、說話、溝通統一走 `Y/N判定 → 使用[機率]卡片 → 成功/失敗` | 中央匯流節點 | 需要**單一**判定服務，取代現有兩套不一致實作 |
| C5 | 「可以使用多張，機率不疊加」 | 判定節點旁註 | 多卡規則需明確定義（見 §10 待決 Q2） |
| C6 | 「寶箱會以各種角度**隨機**出現在**預定的位子**（室內室外皆有）」 | 開寶箱分支旁註 | 房間需要 **spawn slot** 系統，非現行的靜態擺放 |
| C7 | 「有可能會出現需要先獲得鑰匙才能開啟的狀況」 | 同上 | 需要跨節點持有的道具/鑰匙狀態 → 存在 `RunContext` |
| C8 | 「鼠標游到可獲取道具時呈現張開手勢，再次點擊變握拳抓取（確認收取）」 | 橘色註記 | 兩段式互動；`CursorManager` 已有 `HoverChest`/`HoldChest` 可對應 |
| C9 | 流程起點是 **新手介紹** → 地圖下拉 | 左側入口 | 目前專案完全沒有這段，需新增 |
| C10 | 探索視覺為**正交 2D/2.5D** | 開發者口述 2026-08-08 | 相機改 Orthographic；現行 FOV 轉場效果會失效（見 §8 風險） |
| C11 | **戰鬥由 Romtyui 打包成單一 Prefab**；他尚未完成，**每個階段要測試時我們自行從他的 Scene 打包放進來** | 開發者口述 2026-08-08 | 不再需要 additive 載入；Build Settings 收斂到 **1 個場景**。打包是我方的例行動作，非等待對方交付 |
| C12 | 判定**失敗** → 「在試一次？」→ **YES 繞回重試** / **NO 走向離開** | 流程圖右半 | 判定不是一次定生死，需要**重試迴圈**。`hasResolved` 這種一次性旗標的設計要拿掉 |
| C13 | 獲得道具後 → 「要探索其他的東西嗎？」→ **YES 繞回房間繼續** / **NO → 離開** | 流程圖右半 | 房間是**可重複互動的迴圈**，不是走完一條線就結束 |
| C14 | 「鼠標滑到 **[離開]** 標籤時，標籤會自動下拉提示，**再點擊一下確認**退出當前場景」。實作載體是 EventScene 的 **`ExitTag`** 物件，掛 `BookmarkHover` | 流程圖右半藍色註記 + 開發者補充 | 兩段式確認。**第一段已完成**（`BookmarkHover` 的 hover 下拉），**第二段缺**（`BookmarkHover` 沒有任何 `IPointerClickHandler`）→ 見 §4.6 |
| C15 | 商店 `購買商品 → 離開` 下方有回頭線 | 流程圖左下 | 商店可**重複購買**，不是買一次就走 |
| C16 | **特殊事件 → 獲得神牌** | 流程圖「特殊事件」區塊 | 新的節點類型。神牌美術與動畫已存在於 `Romtyui/Card_frame/卡面/神牌/` 與 `Romtyui/Anim/神牌動畫/` |
| C17 | **機率卡與對話選項要分屬性**；hover 卡片時**同時顯示對所有選項的影響**，依類型有些有效／無效 | 開發者口述 + 流程圖草圖「A 50 / B 50 / C 50」 | 需要屬性相剋表 + hover 全選項預覽廣播 → §4.5 |
| C18 | **打牌環節的完整互動模型**：<br>① 玩家可選定一個對話選項為**主要目標**；**未選定時** hover 手牌才顯示全選項預測<br>② **每回合出一張**手牌（非一次多張）<br>③ 成功／失敗**即時反應在選項內文與角色對話框**<br>④ 不滿意可**直接再出牌**，但該選項成功倍率**逐次衰減**<br>⑤ 衰減次數 = 手牌總數，出過的牌會消耗<br>⑥ 有**結束按鈕**，玩家滿意就結束打牌環節<br>⑦ **蓄意失敗是合法策略** | 開發者答覆 Q2／Q10 | 推翻「一次投多張」的舊假設。⑦ 特別重要：系統**不可**在成功時自動結束，必須等玩家按結束鈕 → §4.5 |
| C19 | 相剋倍率為 **1× ／ 0.5× ／ 0×**，中間層是「模糊相關」，用意是**避免抽到的卡完全不能操作** | 開發者答覆 Q8 | **沒有 2×** —— 這是懲罰制不是獎勵制，卡片基礎機率就是上限。`Effectiveness` 三級 |

### 4.1 核心概念：兩個正交的軸

初版設計把 Menu / Map / Explore / Event 當成四個互斥模式 —— **這是錯的**。C1 指出地圖會「下拉」，若地圖與探索互斥，就不會用「下拉」而會用「切換」。正確模型是兩個彼此獨立的軸：

```
軸一  Stage（舞台）── 互斥，同時只有一個
      None │ Intro │ Explore │ Battle │ Shop │ Dialogue

軸二  MapOverlay（地圖覆蓋層）── 常駐，只有「下拉 / 收起」兩態
```

整條遊戲流程因此收斂成一句話：

```
Stage 結束 → MapOverlay 下拉(自動) → 玩家選節點 → MapOverlay 收起 → 載入新 Stage
```

戰鬥線（`戰鬥 → 戰鬥完畢 → 結算 → 地圖下拉(自動)`）與事件線（`開寶箱 → 獲得道具 → 離開 → 地圖下拉(自動)`）走的是**同一個 pattern**，戰鬥只是 Stage 的一種。這讓總管的邏輯少掉一整個分支。

```
                    ┌─────────────────────────────────────────┐
                    │       EventScene（唯一場景，永不卸載）    │
                    │                                          │
   啟動 ──────────▶ │  [常駐層]                                │
                    │   GameFlowManager  ← 唯一流程總管         │
                    │   RunContext       ← 純資料，整場 run     │
                    │   ScreenFader      ← 唯一黑幕             │
                    │   ProbabilityCheck ← 唯一判定服務 (C4)    │
                    │   CursorManager / AudioManager           │
                    │   Main Camera / EventSystem              │
                    │                                          │
                    │  [MapOverlay] 常駐，下拉/收起 (C1)        │
                    │                                          │
                    │  [StageHost] 動態掛載，互斥               │
                    │   ├ Stage_Menu.prefab                    │
                    │   ├ Stage_Intro.prefab      (C9 新增)     │
                    │   ├ Stage_Explore.prefab                 │
                    │   ├ Stage_Shop.prefab                    │
                    │   └ Stage_Battle.prefab  ← Romtyui 交付   │
                    └──────────────────────────────────────────┘
```

### 4.2 EventScene Hierarchy 藍圖

```
EventScene
├── [SYSTEM]                     ← 場景不卸載，故不需 DontDestroyOnLoad
│   ├── GameFlowManager
│   ├── ScreenFader              (Canvas, order 9000)
│   ├── ProbabilityCheck
│   ├── CursorManager
│   └── AudioManager             (從 MenuScene 的 BGMManager 搬來)
├── [CAMERA]
│   └── Main Camera              ← Orthographic (C10)
├── EventSystem
├── [STAGE_HOST]                 ← 舞台，互斥
│   ├── WorldRoot                ← 2.5D 房間 / 戰鬥 prefab 生成點
│   └── StageUIRoot → Canvas_Stage
├── [MAP_OVERLAY]                ← 常駐 (C1)，Canvas order 300
│   ├── MapPanel                 ← 靠 anchoredPosition.y 下拉/收起
│   ├── MapContainer / 節點 / 連線
│   ├── PlayerAvatar
│   └── MapBanner
└── [UI_ROOT]
    ├── Canvas_Stage   (order 100) ← Stage prefab 的 UI 掛這
    ├── Canvas_Popup   (order 500) ← UIManager 彈窗 / Loot，跨 Stage 共用
    └── Canvas_Tooltip (order 800)
```

**相機策略**：只保留一台 **Orthographic** `Main Camera`（C10）。地圖、選單、UI 全部 Screen Space Overlay，不需要相機。這消滅了 UIScene 的 `MapCamera` 與 ExploreScene 的第二台 `Main Camera`。

> ⚠️ 因為改正交，`ExplorationManager` 現行的進場 zoom 效果（`mainCamera.fieldOfView` 內插）**會完全失效且不報錯** —— 正交相機不使用 FOV。必須改為 `orthographicSize`，且插值方向相反（size 越小畫面越近）。詳見 §8。

### 4.3 狀態機

```
   ┌──────┐  StartNewRun()   ┌───────┐  介紹結束   ┌═══════════════════┐
   │ Menu │ ────────────────▶│ Intro │ ──────────▶║  MapOverlay 下拉  ║◀──┐
   └──────┘       (C9)       └───────┘            ╚═════════╤═════════╝   │
       ▲                                                    │ 玩家選節點   │
       │ GoToMenu()                          MapOverlay 收起 ▼             │
       │                              ┌──────────────────────────────┐    │
       │                              │   StageHost.Load(node)       │    │
       │                              │  ┌─────────┬────────┬──────┐ │    │
       └──────────────────────────────┤  │ Explore │ Battle │ Shop │ │    │
                     結局 / 玩家離開    │  └─────────┴────────┴──────┘ │    │
                                      └──────────────┬───────────────┘    │
                                                     │ Stage 結束(自動 C2) │
                                                     └────────────────────┘
```

**兩條鐵則**
1. 任何 Stage 切換與 MapOverlay 開合，都必須經過 `GameFlowManager`。Stage prefab 內部不得出現 `SceneManager.*`。
2. Stage 結束是**自動**回報（C2），不是玩家按鈕觸發。Stage 完成時呼叫 `GameFlowManager.Instance.NotifyStageComplete()`，由總管決定下一步。

### 4.4 事件節點的內容模型（C3、C6、C7）

流程圖顯示一個「事件節點」內含多種互動物件，且寶箱位置與角度隨機。因此房間不該把物件寫死，而是：

```
RoomPrefab
└── SpawnSlots[]            ← 預先擺好的空位（室內/室外，含允許的角度範圍）
        ↓ 進場時由 RoomPopulator 隨機填入
   ┌────────────┬──────────────┬────────┬──────┬────────┐
   │ 寶箱(直開)  │ 寶箱(需鑰匙)  │  路人   │ NPC  │  商店   │
   └────────────┴──────────────┴────────┴──────┴────────┘
         全部實作 IInteractable，需判定者再實作 IProbabilityTarget
```

鑰匙/道具持有狀態存在 `RunContext.inventory`（C7），才能跨節點延續。

### 4.4b 房間內的互動迴圈（C12、C13、C14）

流程圖右半顯示房間**不是一條直線走完就結束**，而是雙層迴圈：

```
            ┌──────────────────── 繼續探索 ────────────────────┐
            ▼                                                  │
   ┌──────────────────┐                                        │
   │ 說話 / 打開箱子ING │                                        │
   └────────┬─────────┘                                        │
            ▼                                                  │
        ◇ Y/N判定 ◇                                             │
       ╱          ╲                                            │
   需判定        不需判定                                        │
     │              └────────────┐                             │
     ▼                           ▼                             │
┌─────────────────┐        ┌──────────────┐                    │
│ 使用[機率]卡片   │        │ 直接獲得道具  │                    │
│ 可用多張，不疊加 │        └──────┬───────┘                    │
└───┬─────────┬───┘               │                            │
  成功       失敗                  │                            │
    │          ▼                  │                            │
    │    ┌───────────┐            │                            │
    │    │ 在試一次？ │            │                            │
    │    └─┬───────┬─┘            │                            │
    │    YES      NO              │                            │
    │     └─(重試)─┼──────────┐   │                            │
    ▼             │           │   │                            │
┌──────────────┐  │           │   │                            │
│ 獲得卡牌或道具 │◀─┼───────────┼───┘                            │
└──────┬───────┘  │           │                                │
       ▼          │           │                                │
┌────────────────────┐        │                                │
│ 要探索其他的東西嗎？ │──YES───┼────────────────────────────────┘
└──────────┬─────────┘        │
          NO                  │
           ▼                  ▼
      ┌─────────────────────────┐
      │ 離開（hover 提示 → 再點  │  ← C14 兩段式確認
      │ 一次確認）               │
      └───────────┬─────────────┘
                  ▼
         地圖下拉(自動)  ← C1/C2
```

**兩個對實作的直接影響**

1. **重試迴圈（C12）**：現行 `DialogueOptionInteractable` 用 `hasResolved` 旗標鎖死「已結算過」，`EnemyInteractable` 用 `hasTriggered` 同理 —— 兩者都**不支援重試**，與企劃衝突。新的判定目標必須是可重複進入的狀態機，重試次數與懲罰（若有）由 `IProbabilityTarget` 自己持有。
2. **兩段式離開（C14）**：`ExitTag` 需要 `Idle → Hover 提示下拉 → Confirm` 三態。這與 C8 的抓取手勢（張開手 → 握拳）是**同一套互動語言**，應共用 `CursorManager` 的狀態；建議抽一個 `TwoStageConfirm` 元件同時服務兩者。

### 4.5 屬性相剋與 hover 全選項預覽（C17）

> 依據：開發者指示「機率卡片和對話選擇之後要分屬性，hover 時可以同時看見對所有選項的影響（根據類型有些有效/無效，像寶可夢）」，以及流程圖草圖中畫的 `A 50 / B 50 / C 50` 三列預覽。

#### 機率模型（C19 定案）

```
CardDataExplore          IProbabilityTarget（對話選項 / 寶箱 / NPC）
  ├ attribute            ├ attribute
  └ successProbability   └ decayedMultiplier  ← 每次出牌後衰減 (C18⑤)

               ▼ 查表
      AttributeChartData（相剋表 ScriptableObject）
         Match 1×  │  Partial 0.5×  │  None 0×
         （相符）      （模糊相關）      （無效）

               ▼
   最終機率 = Clamp01(卡片基礎機率 × 相剋倍率 × 目標當前衰減倍率)
```

> ⚠️ **沒有 2×**。卡片自身的 `successProbability` 就是機率上限，相剋只會往下扣。這是**懲罰制**，與寶可夢的雙向增減不同 —— 實作時別自作主張加獎勵層。
>
> `Partial 0.5×` 的存在目的是「避免抽到的卡完全不能操作」，所以 **`None 0×` 應該少用**，只保留給設計上真的要封死的組合。

#### 打牌環節的完整流程（C18）

這是 v4 最大的修正 —— 舊版假設「一次投多張牌」，實際上是**回合制、一次一張、可連續嘗試**：

```
       進入對話 / 互動
              │
              ▼
   ┌──────────────────────────┐
   │ 玩家是否已選定「主要目標」？│
   └────┬────────────────┬────┘
        │ 否              │ 是
        ▼                 ▼
  hover 手牌 →      出牌只對主要目標生效
  所有選項同時       （其他選項不再預覽）
  顯示預測 (C17)
        │                 │
        └────────┬────────┘
                 ▼
        ┌─────────────────┐
        │  出一張手牌      │◀────────────┐
        └────────┬────────┘             │
                 ▼                      │
        ProbabilityCheck.Roll(單張)      │
                 ▼                      │
     ┌───────────────────────────┐      │
     │ 即時更新：                 │      │
     │  · 選項內文（成功/失敗文字）│      │
     │  · 角色對話框              │      │
     └────────────┬──────────────┘      │
                  ▼                     │
     ┌────────────────────────────┐     │
     │ 消耗該張手牌                │     │
     │ 該選項成功倍率衰減一級       │     │
     └────────────┬───────────────┘     │
                  ▼                     │
        ┌──────────────────┐            │
        │ 還有手牌？        │──有──┐     │
        └────────┬─────────┘      │     │
                無                 │     │
                 │        玩家決定：再出一張 ──┘
                 │                 │
                 │           或按【結束】
                 ▼                 ▼
        ┌────────────────────────────┐
        │  結束打牌環節 → 結算當前結果  │
        └────────────────────────────┘
```

**四個容易做錯的地方**

1. **不可自動結束**。因為「蓄意失敗」是合法策略（C18⑦），系統即使判定成功也**必須**停下來等玩家按結束鈕。若寫成「成功即跳出」，就摧毀了這個玩法。
2. **衰減掛在「選項」上，不是掛在「卡」上**。同一個選項被反覆嘗試會越來越難；換一個選項則是各自獨立的衰減進度。
3. **衰減次數上限 = 手牌總數**（C18⑤）。手牌自然成為嘗試次數的資源，不需要另外設次數限制。
4. **hover 預覽只在「未選定主要目標」時作用**（C18①）。選定之後畫面應聚焦在該目標，不再廣播全選項。

#### hover 預覽的資料流（C17）

```
玩家 hover 手牌（且尚未選定主要目標）
   │
   ▼
CardViewUIExplore.OnPointerEnter
   │
   ▼
HoverPreviewBroadcaster.Begin(card)        ← 廣播，不是讓每個選項自己輪詢
   │
   ├──▶ 選項A.ShowPreview(50%, Match)     「A  50」
   ├──▶ 選項B.ShowPreview(25%, Partial)   「B  25」偏灰
   └──▶ 選項C.ShowPreview(0,   None)      「C  ✕」不可用
   │
   ▼
玩家移開 → Broadcaster.End() → 全部隱藏
```

**設計要點**
- 用**廣播**而非各自 `FindObjectsOfType`。
- 預覽與實際擲骰**必須共用同一個計算函式**（`ProbabilityCheck.CalculateRate`）—— 否則會出現「顯示 50% 實際卻不是」的經典 bug，玩家會覺得遊戲在騙人。§5.1 因此把 `CalculateRate`（純計算）與 `Roll`（擲骰）拆開，後者內部呼叫前者。
- **預覽要反映當前衰減值**。反覆嘗試後預覽數字必須跟著降，否則玩家看到的是過期資訊。
- `None (0×)` 顯示為明確的**不可用**（打叉／灰底），不要顯示「0%」—— 要讓玩家看得出是屬性不合，不是運氣差。

### 4.6 兩段式確認與「下拉」互動語言（C8、C14）

專案裡有三個地方用同一種動作語彙，應共用實作：

| 位置 | 第一段 | 第二段 | 現況 |
|---|---|---|---|
| **ExitTag**（離開事件場景） | hover → 標籤從上緣滑下 | 再點一次確認 | 第一段 ✅ `BookmarkHover` 已實作；**第二段 ❌ 缺** |
| **可拾取道具**（C8） | hover → 游標變張開的手 | 點擊 → 握拳抓取 | `CursorManager` 已有 `HoverChest`／`HoldChest` 狀態 |
| **MapOverlay**（C1 地圖下拉） | 事件結束 → 地圖自動滑下 | — | 待實作 |

**`BookmarkHover` 的機制可直接複用到 MapOverlay**：它用 `hiddenY`（往上藏）／`shownY` 兩個座標 + `Mathf.Lerp` 到 `targetY`，正是「地圖下拉」需要的。差別只在觸發來源 —— ExitTag 由 hover 觸發，MapOverlay 由 `GameFlowManager` 觸發。

**缺的第二段**：`BookmarkHover` 只實作 `IPointerEnterHandler` / `IPointerExitHandler`，沒有 `IPointerClickHandler`。Phase 4d 新增 `TwoStageConfirm` 處理點擊確認，**不改 `BookmarkHover`**（它的滑動職責很乾淨，保持單一職責）。

---

## 5. 新增檔案清單

全部新增於 `Assets/TYN/Core/`，不動任何現有檔案。

| 檔案 | 職責 | 行數估計 |
|---|---|---|
| `StageType.cs` | `enum StageType { None, Menu, Intro, Explore, Battle, Shop, Dialogue }` | ~10 |
| `MapData.cs` | 從 `PerspectiveMapGenerator.cs` 抽出 `RunNodeData` / `MapData` 純資料類別 | ~30 |
| `RunContext.cs` | 一整場 run 的狀態容器（地圖、HP、探索牌組、**鑰匙/道具 C7**） | ~70 |
| `GameFlowManager.cs` | 唯一流程總管：持有 `RunContext`、驅動 Stage 切換與 MapOverlay 開合 | ~180 |
| `StageController.cs` | Stage prefab 的抽象基底 | ~35 |
| `StageHost.cs` | 依 `StageType` 生成 / 銷毀 Stage prefab | ~80 |
| `MapOverlayController.cs` | **常駐地圖覆蓋層**的下拉 / 收起動畫（C1、C2） | ~90 |
| `ScreenFader.cs` | 唯一黑幕 | ~60 |
| `ProbabilityCheck.cs` | **唯一判定服務**（C4、C5、C17），預覽與擲骰共用計算 | ~80 |
| `ExploreAttribute.cs` | 屬性列舉（C17） | ~15 |
| `AttributeChartData.cs` | 相剋表 ScriptableObject，數值在 Inspector 調（C17） | ~50 |
| `IProbabilityTarget.cs` | 需判定的互動目標介面，含重試狀態（C12） | ~25 |
| `IInteractable.cs` | 互動物件介面，取代現有的 `ICardInteractable` | ~20 |
| `HoverPreviewBroadcaster.cs` | hover 卡片時廣播機率給所有選項（C17） | ~60 |
| `TwoStageConfirm.cs` | 兩段式確認元件，服務抓取手勢與離開標籤（C8、C14） | ~55 |

> `SceneNames.cs` 已從清單移除 —— 因 C11（戰鬥改 prefab 交付），整個專案只剩一個場景，不再需要場景名常數，也不再需要 `BattleGate.cs`。

### 5.1 核心 API 設計

```csharp
// ─────────── RunContext.cs ───────────
// 純資料，不繼承 MonoBehaviour。一整場 run 的所有狀態都在這裡。
[System.Serializable]
public class RunContext
{
    public MapData mapData = new();

    public int playerHp;
    public int playerMaxHp;

    public List<CardDataExplore> exploreDeck = new();

    // C7：鑰匙與道具需跨節點延續（「需要先獲得鑰匙才能開啟」）
    public List<string> inventory = new();
    public bool HasItem(string id) => inventory.Contains(id);

    // 進入節點前寫入，供 Stage 讀取
    public RunNodeData pendingNode;

    public RunNodeData CurrentNode =>
        mapData.allNodes.Find(n => n.nodeId == mapData.currentNodeId);
}


// ─────────── GameFlowManager.cs ───────────
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    public StageType  CurrentStage { get; private set; }
    public RunContext Run          { get; private set; }
    public bool       IsMapOpen    { get; private set; }

    public event System.Action<StageType, StageType> OnStageChanged;

    // ── 流程入口。Stage prefab 只呼叫這些，不得自行切換 ──
    public void GoToMenu();
    public void StartNewRun();          // 建 RunContext → Intro → 開地圖
    public void EnterNode(RunNodeData node);  // 收地圖 → 依 nodeType 載入 Stage

    /// C2：Stage 完成時「自動」回報，由總管決定下一步（通常是地圖下拉）。
    /// 注意：這不是「玩家按了回地圖」，而是流程自然結束。
    public void NotifyStageComplete(StageResult result);

    private IEnumerator SwitchStage(StageType next);
    private IEnumerator OpenMap();      // MapOverlay 下拉 (C1)
    private IEnumerator CloseMap();     // MapOverlay 收起
}

public enum StageResult { Completed, PlayerDied, RunFinished }


// ─────────── StageController.cs ───────────
// 每個 Stage prefab 的根物件都掛一個子類別。
public abstract class StageController : MonoBehaviour
{
    public abstract StageType Stage { get; }

    /// 進場（畫面仍全黑）。讀 RunContext 初始化。
    public virtual void OnStageEnter(RunContext run) { }

    /// 黑幕淡出後呼叫。播開場動畫。
    public virtual IEnumerator OnStageReady() { yield break; }

    /// 退場（畫面已全黑）。存回 RunContext、清理。
    public virtual IEnumerator OnStageExit() { yield break; }
}


// ─────────── MapOverlayController.cs ───────────
// C1：地圖是常駐覆蓋層，不是 Stage。整場 run 只存在一份，不重建。
public class MapOverlayController : MonoBehaviour
{
    public IEnumerator SlideDown();   // 從畫面上方滑入 =「地圖下拉」
    public IEnumerator SlideUp();     // 收回上方
    public bool IsOpen { get; }

    /// 依 RunContext.mapData 重畫節點狀態（走過的、可去的、目前位置）
    public void Refresh(RunContext run);
}


// ─────────── ExploreAttribute.cs / AttributeChartData.cs ───────────
// C17/C19：屬性與相剋表。屬性名稱待定（Q7），先用佔位值；倍率已定案為 1/0.5/0。
public enum ExploreAttribute { None, Intuition, Logic, Insight }  // Q7a 已定案 2026-08-15

/// C19：三級，**沒有 2×**。卡片自身機率即上限，相剋只往下扣。
public enum Effectiveness
{
    Match   = 0,   // 1.0×  相符
    Partial = 1,   // 0.5×  模糊相關 —— 存在目的是避免手牌完全不能操作
    None    = 2,   // 0.0×  無效（少用，只給設計上要封死的組合）
}

[CreateAssetMenu(menuName = "Eldritch/Attribute Chart")]
public class AttributeChartData : ScriptableObject
{
    public float matchMultiplier   = 1.0f;
    public float partialMultiplier = 0.5f;
    public float noneMultiplier    = 0.0f;

    public Effectiveness GetEffectiveness(ExploreAttribute card, ExploreAttribute target);
    public float         GetMultiplier(Effectiveness eff);
}


// ─────────── IProbabilityTarget.cs ───────────
// C12/C18：判定目標支援「反覆嘗試 + 逐次衰減」。
// ⚠️ 衰減狀態掛在【目標】上而非卡片上 —— 同一選項越試越難，換選項各自獨立。
// ⚠️ 封存的 DialogueOptionInteractable(hasResolved) 與 EnemyInteractable(hasTriggered)
//    都是一次性旗標，與此模型根本衝突，這也是它們被封存的主因。
public interface IProbabilityTarget
{
    ExploreAttribute Attribute { get; }

    /// 當前衰減倍率。初始 1.0，每次對此目標出牌後降一級（C18④⑤）。
    float CurrentDecayMultiplier { get; }
    void  ApplyDecay();

    /// 出牌結果 → 即時更新選項內文與角色對話框（C18③）
    void  OnCheckResult(bool success);

    /// C17 hover 預覽。rate 需已反映當前衰減值。
    void  ShowPreview(float rate, Effectiveness eff);
    void  HidePreview();
}


// ─────────── ProbabilityCheck.cs ───────────
// C4/C17/C18/C19：全專案唯一的判定服務。
public class ProbabilityCheck : MonoBehaviour
{
    public static ProbabilityCheck Instance { get; private set; }
    public AttributeChartData chart;

    /// 【純計算，無副作用】hover 預覽與實際擲骰都呼叫這支。
    /// ⚠️ 兩者共用同一函式是硬性要求 —— 否則會出現「顯示 50% 實際卻不是」的經典 bug。
    /// 計算式：Clamp01(card.successProbability × 相剋倍率 × target.CurrentDecayMultiplier)
    public float CalculateRate(CardDataExplore card, IProbabilityTarget target,
                               out Effectiveness eff);

    /// 【實際擲骰】C18②：一次只吃**一張**卡（不是清單）。
    /// 呼叫端負責：消耗手牌 → target.ApplyDecay() → target.OnCheckResult()
    public bool Roll(CardDataExplore card, IProbabilityTarget target, out float finalRate);
}


// ─────────── DialogueEncounterController.cs ───────────
// C18：打牌環節的回合管理。這是 v4 新增、也是最容易做錯的一塊。
public class DialogueEncounterController : MonoBehaviour
{
    /// 玩家選定的主要目標；null = 未選定，此時 hover 才廣播全選項預覽（C18①）
    public IProbabilityTarget PrimaryTarget { get; private set; }
    public void SelectPrimaryTarget(IProbabilityTarget t);

    /// 出一張牌（C18②）。內部：Roll → 消耗手牌 → ApplyDecay → OnCheckResult
    public void PlayCard(CardInstanceExplore card, IProbabilityTarget target);

    /// ⚠️ C18⑦：即使判定成功也**不得**自動結束 —— 蓄意失敗是合法策略。
    ///    只有玩家按結束鈕、或手牌耗盡，才走到這裡。
    public void EndEncounter();

    public bool HasCardsLeft { get; }
}


// ─────────── HoverPreviewBroadcaster.cs ───────────
// C17：hover 一張卡 → 所有選項同時顯示各自機率（草圖的 A 50 / B 50 / C 50）。
public class HoverPreviewBroadcaster : MonoBehaviour
{
    public static HoverPreviewBroadcaster Instance { get; private set; }

    public void Register(IProbabilityTarget target);    // 選項進場時註冊
    public void Unregister(IProbabilityTarget target);

    /// ⚠️ 已選定主要目標時不廣播（C18①）—— 呼叫端需先檢查。
    public void Begin(CardDataExplore hoveredCard);
    public void End();
}


// ─────────── TwoStageConfirm.cs ───────────
// C8/C14：兩段式確認。ExitTag 的第一段已由既有的 BookmarkHover 完成，
// 本元件只補「再點一次確認」，兩者掛在同一物件上、職責分離。
public class TwoStageConfirm : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onConfirmed;
    public float armedTimeout = 2f;   // 進入待確認狀態後多久自動解除

    public bool IsArmed { get; private set; }
}


// ─────────── ScreenFader.cs ───────────
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public IEnumerator FadeToBlack(float duration = 0.4f);
    public IEnumerator FadeFromBlack(float duration = 0.4f);
    public void SetBlackImmediate(bool black);
    public bool IsBlocking { get; }   // 轉場中，吃掉所有 raycast
}
```

---

## 6. 實作步驟

每個階段結束時，**遊戲必須能從主選單完整跑到當前已實作的最後一關**。

> v3 說明：本節基調由「遷移既有程式」改為「**重建為主**」。§2.4 判定為封存的 21 個腳本，在對應階段直接移入 `_Archive/` 並寫新的，不做逐行改造 —— 對這個程度的遺留，重寫比改造快。

### Phase 0 — 清場 ✅ **已於 2026-08-08 執行完畢**

實際執行結果：`_Recovery` 已 `git rm`（86 個追蹤檔案，可還原）、21 個腳本連同 `.meta` 移入 `_Archive/Scripts/`、`TESTING/` 資料夾解散、`Card_data` v1/v2 封存、docx 歸位。git 認列 **125 筆 rename**，檔案歷史完整保留。孤兒 `.meta` 檢查通過。

> 執行時遇到的小狀況：`git rm` 無法刪除「已無追蹤檔案的空目錄」（`RCard/`），改用 `rmdir` + 單獨 `git rm` 其 `.meta` 解決。若日後再做類似搬移，先搬檔案再處理目錄本身。

以下為原始規劃，保留供參：

| 動作 | 說明 |
|---|---|
| 刪除 `Assets/_Recovery/` | 43 個垃圾場景、21 MB，與遊戲無關 |
| 建立資料夾 | `TYN/Core/`、`TYN/Stages/`、`TYN/Map/`、`TYN/_Archive/` |
| **執行 §2.4 封存清單** | 21 個腳本連同 `.meta` 移入 `_Archive/`。**務必依 §2.4 的拆解順序**，`EnemyInteractable.cs` 最後移（`ICardInteractable` 定義在裡面） |
| 素材歸位 | `TESTING/Menu_*` → `TYN/Menu/`；`TESTING/RCard/` 保留的三個 → `TYN/Explore/CARD/Scripts/`；`TESTING/` 資料夾解散 |
| 卡牌資料 | `Card_data/v1`、`v2` → `_Archive/`，只留 `v3`（0/20/40/60/80/100 完整階梯） |
| 舊場景 | `BattleScene.unity`、`BattleScene_v1.unity` → `_Archive/` |
| docx | `Assets/探索測試資料夾/` → `TYN/Docs/` |

> ✅ **修正（v3.1）**：先前版本說「封存後會噴大量 missing script」，**這是錯的**。
>
> `Assets/TYN/_Archive/` 仍在 `Assets/` 底下，只要 `.cs` 與 `.meta` **一起搬**，GUID 就會保留 —— 場景引用完好、程式照常編譯、**零破壞**。Phase 0 的封存是**純粹的整理**：把垃圾與正式程式碼分開放，不是把它們踢出編譯。
>
> **真正踢出編譯**（移到 `Assets/` 之外或用 `~` 結尾的資料夾）必須等各 Phase 重寫完該區塊。原因見下方依賴分析 —— 現在硬踢會編譯不過。

#### Phase 0 的依賴實測（2026-08-08）

「保留／改寫」的腳本**反過來依賴**封存名單，共 5 條：

| 保留的腳本 | 依賴 | 解除時機 |
|---|---|---|
| `PerspectiveMapGenerator` | `MapNodeExplore` | Phase 3 |
| `PerspectiveNode` | `ExplorationManager` | Phase 3 |
| `CardDataExplore` | `ExplorationCardEffectData` | Phase 4b |
| `ExplorationHandUIController` | `ExplorationCardDragUI` | Phase 4c |
| `ExplorationHandUIController` | `CardExplorationManager` | Phase 4c |

傳遞閉包後，21 個封存腳本中有 **13 個必須留在編譯範圍內**：`CardExplorationManager`、`ContainerObject`、`Door`、`EnemyInteractable`、`ExplorationCardDragUI`、`ExplorationCardEffectData`、`ExplorationCardResolveContext`、`ExplorationInteractableTarget`、`ExplorationManager`、`InspectableObject`、`MapNodeExplore`、`RoomController`、`UIManager`。

> ⚠️ `EnemyInteractable` 之所以在名單內，是因為 **`ICardInteractable` 介面定義在它檔案裡**，而 `CardExplorationManager` 需要那個介面 —— 光看類別名會誤判可以移除。這正是 §2.4 標注拆解順序的原因。

剩下 8 個（`BattleToMapBridge`、`CardDragUIExplore`、`CardHoverUIExplore`、`DialogueOptionInteractable`、`SceneLoader`、三個 `Explore*EffectData`）理論上可立即踢出編譯，但為避免 Phase 0 出現兩個封存位置造成混淆，**一律先放 `Assets/TYN/_Archive/`**。

**驗收**：`_Recovery` 已刪、資料夾結構符合附錄 B、`_Archive/` 內容不被任何**新**場景引用。

---

### Phase 1 — 建立 Core 層 ✅ **程式部分已於 2026-08-08 完成**

**已完成**：`Assets/TYN/Core/` 17 個檔案、1754 行，全部置於 `namespace EldritchMile.Core`。

> **為什麼要用命名空間**：`MapData` / `RunNodeData` 與尚未改寫的 `PerspectiveMapGenerator.cs` 撞名。加上命名空間後兩者可並存，Phase 3 改寫時再移除舊的。同時也給新舊程式碼一條清楚的界線。

**編譯驗證**：以 `dotnet build` 對 Unity 6000.4.1f1 的真實組件（`UnityEngine` 模組 + `Library/ScriptAssemblies`）編譯整個 `Assets/TYN` + `Assets/Romtyui`，結果 **0 error 0 warning**。

| 檔案 | 行 | 重點 |
|---|---:|---|
| `StageType.cs` | 40 | `StageType` / `StageResult` |
| `MapData.cs` | 140 | 純資料。**修掉舊 Boss 判定 bug**（見下） |
| `MetaProgressData.cs` | 110 | **遺產機制切分點**，跨輪迴存檔 |
| `RunContext.cs` | 150 | 單場 run；`CreateNew` / `ContributeToMeta` 是與 Meta 的唯一橋樑 |
| `GameFlowManager.cs` | 300 | 流程總管，Stage × MapOverlay 兩軸 |
| `StageController.cs` | 60 | Stage 抽象基底 |
| `StageHost.cs` | 110 | Stage prefab 生成／銷毀 |
| `MapOverlayController.cs` | 150 | C1 地圖下拉，沿用 `BookmarkHover` 手法 |
| `ScreenFader.cs` | 100 | 唯一黑幕 |
| `ProbabilityCheck.cs` | 130 | C4/C19，`CalculateRate` 與 `Roll` 共用計算 |
| `ExploreAttribute.cs` | 40 | 屬性佔位值 + `Effectiveness` 三級 |
| `AttributeChartData.cs` | 90 | 相剋表 SO，Inspector 可調 |
| `IProbabilityTarget.cs` | 45 | 含衰減狀態 |
| `IInteractable.cs` | 30 | 取代 `ICardInteractable` |
| `HoverPreviewBroadcaster.cs` | 110 | C17 全選項預覽廣播 |
| `TwoStageConfirm.cs` | 105 | C8/C14 第二段確認 |
| `DialogueEncounterController.cs` | 210 | **C18 打牌回合管理**，最容易做錯的一塊 |

> **順手修掉的舊 bug**：舊 `PerspectiveMapGenerator` 判斷 Boss 節點寫的是
> `currentNode.layer == allNodes.Count - 1` —— 但 `allNodes.Count` 是**節點總數**（約 12），
> 不是**層數**（約 5），兩者量級不同，導致 Boss 判定幾乎永遠為 false。
> 新的 `MapData.MaxLayer` / `IsFinalLayer()` 已正確實作。

**尚未完成（需要在 Unity 編輯器內操作）**：
- 依 §4.2 藍圖搭 EventScene 的 `[SYSTEM]` / `[CAMERA]` / `[STAGE_HOST]` / `[MAP_OVERLAY]` / `[UI_ROOT]` 骨架
- 刪掉 `ENCOUNTER / EXPLORE / GOD / MAP / SHOP` 測試按鈕
- 建立一個 `AttributeChartData` 資產
- `EventScene` 設為 Build Settings index 0

---

以下為原始規劃，保留供參：

1. 寫出 §5.1 的 16 個檔案（含屬性系統與 hover 預覽）。
2. 開啟 `EventScene`，刪掉 `ENCOUNTER / EXPLORE / GOD / MAP / SHOP` 測試按鈕，依 §4.2 藍圖搭出 `[SYSTEM]` / `[CAMERA]` / `[UI_ROOT]` / `[MODE_HOST]` 骨架。
3. 把 `EventScene` 加進 Build Settings 並拖到 **index 0**（MenuScene 暫時降到 index 1，維持舊流程可跑）。

**驗收**：EventScene 單獨執行不報錯，`GameFlowManager` 能在 Console 印出 Stage 切換 log（此時還沒有任何 Stage prefab，屬正常）。另建一個測試用 `AttributeChartData` 資產，確認 `ProbabilityCheck.CalculateRate` 的回傳與手算相符。

---

### Phase 2 — 主選單（程式已完成，編輯器操作見 `Phase2_MenuStage.md`）

MenuScene 的 4 個腳本中，`TrickButton` / `BGLiquidController` / `BoilFrameEffect` 全部**保留**，只有 `SceneLoader` 封存 —— 是最輕的一關。

**已完成**：`Assets/TYN/Stages/MenuStageController.cs`（編譯 0 error）。

> **調查發現**：舊 MenuScene 的 **START / SETTINGS / RESTART 三顆按鈕全部綁同一個 `SceneLoader.LoadUIScene`** —— 後兩顆從未實作，只是佔位。`MenuStageController` 因此只實作 `OnStartClicked()`，另外兩支刻意留成印 log 的佔位，不擅自猜測語意。另提供 `OnClearProgressClicked()`（清除跨輪迴進度），若 RESTART 的本意是這個再改綁。
>
> **命名空間決定**：Stage 層維持**全域命名空間**。Core 之所以用 `EldritchMile.Core` 是為了避開 `MapData`/`RunNodeData` 撞名，Stage 沒有這個問題，全域反而在 Inspector 綁定時少一層阻力。

1. 把 MenuScene 的 `Canvas`（含 BG、frame_st/se/re、START/SETTINGS/RESTART 按鈕）整個拉成 `Stages/Stage_Menu.prefab`。
2. 根物件掛 `MenuStageController : StageController`。
3. **封存 `SceneLoader.cs`**，START 按鈕改接 `GameFlowManager.Instance.StartNewRun()`。
4. `BGMManager` 搬進 EventScene 的 `[SYSTEM]`。
5. MenuScene → `_Archive/`，從 Build Settings 移除。

**驗收**：從 EventScene 啟動 → 看到主選單 → 液態背景與沸騰邊框動畫正常 → 按 START 後 Console 印出 Stage 切換 log（此時地圖尚未遷移，先用 log 佔位）。

---

### Phase 3 — 地圖改為常駐覆蓋層（程式已完成，編輯器操作見 `Phase3_MapOverlay.md`）

這階段同時解掉「狀態隨場景死亡」與 v1 的架構誤判（地圖非模式而是覆蓋層，C1）。

**已完成**（編譯 0 error）：

| 檔案 | 位置 | 職責 |
|---|---|---|
| `MapGenerationSettings.cs` | `Core/` | 生成參數 SO |
| `MapGenerator.cs` | `Core/` | 純邏輯生成 `MapData` |
| `MapView.cs` | `Map/` | 繪製 + 棋子移動，繼承 `MapOverlayController` |
| `MapNodeUI.cs` | `Map/` | 單一節點 UI |

順手修掉的兩個舊隱患：
- **連線計算改用 `xPercent`**，不再讀 `transform.localPosition` 排序。舊版在設完 anchor 後立刻讀 localPosition，依賴 Unity 何時重算 RectTransform，是會隨版本或執行順序改變行為的隱患。
- **改用 `System.Random` + seed**，不用 `UnityEngine.Random`（全域狀態）。同一個 seed 必得同一張地圖，除錯容易得多。

> ⚠️ **踩到的坑（值得記住）**：新腳本一開始寫在全域命名空間並 `using EldritchMile.Core;`，結果 `MapData` / `RunNodeData` **綁到了尚未封存的舊 `PerspectiveMapGenerator.cs` 的全域型別**。
>
> 原因：檔案最上方的 `using` 註冊在「全域層」，而 C# 名稱解析在同一層「宣告永遠贏過 using 匯入」。修法是把 `using` 寫進 `namespace` **內部**，該層就會先採用 using。
>
> 這種錯誤只在型別轉換時才爆，很容易誤判成別的問題 —— 之後 Phase 4 若也遇到同名情況，先檢查 `using` 的位置。

1. **抽出資料層**：把 `RunNodeData` / `MapData` 從 `PerspectiveMapGenerator.cs` 移到 `Core/MapData.cs`。`PerspectiveMapGenerator` 改成讀寫 `GameFlowManager.Instance.Run.mapData`，不再自己持有。
2. **拆掉轉場職責**：刪除 `MoveAndLoadRoutine` / `TransitionToSceneRoutine` / `TransferRoutine` / `WakeUpRoutine` / `FadeRoutine` 五個協程與 `lastLoadedSceneName` 欄位。`OnNodeClicked` 改成：
   ```csharp
   public void OnNodeClicked(RunNodeData node)
   {
       if (ScreenFader.Instance.IsBlocking) return;
       StartCoroutine(MoveAvatarThen(node));   // 只留棋子移動動畫
   }
   private IEnumerator MoveAvatarThen(RunNodeData node)
   {
       yield return MoveAvatarAndScrollMap(Run.mapData.currentNodeId, node.nodeId);
       GameFlowManager.Instance.EnterNode(node);   // 收地圖 + 載 Stage 交給總管
   }
   ```
3. **`MapBannerUI` 去權**：拿掉 `SceneManager.LoadScene`，`backToMenuButton` 改接 `GameFlowManager.Instance.GoToMenu()`。
4. 把 UIScene 的 `MapCanvas` / `MapContainer` / `Player Avatar` / `MapBanner` 搬成 EventScene 底下的 **`[MAP_OVERLAY]` 常駐節點**（不是 Stage prefab），掛 `MapOverlayController`。
5. 地圖 Canvas 改 **Screen Space - Overlay**，刪除 `MapCamera`。
6. **實作下拉/收起**（C1）：`SlideDown()` / `SlideUp()` 以 `MapPanel.anchoredPosition.y` 從畫面外滑入滑出。收起後把 `CanvasGroup.blocksRaycasts` 設 false，避免玩家在 Stage 進行中還能點節點。
7. 地圖生成邏輯（`GenerateProceduralMap` / `GenerateDemoRoute`）移到 `GameFlowManager.StartNewRun()`，`MapOverlayController` 只負責「畫出 `RunContext` 裡已存在的地圖」。
8. UIScene → `_Archive/`。

**驗收**：主選單 → 地圖**下拉**入場 → 節點逐層彈出 → 點節點棋子走過去 → 地圖**收起**。**關鍵驗收**：反覆下拉收起多次，節點拓撲、走過的路徑、棋子位置全部保留且不重新生成。

---

### Phase 4 — 探索 Stage（4 天，v3 由「遷移」改為「重建」）

`ExplorationManager` / `RoomController` / 兩套卡牌拖曳 / 互動物件全部封存，本階段是**新寫**。可沿用的只有 `ExplorationDeck`（牌堆邏輯）與 `CardViewUIExplore`（卡面顯示）。

**4a. 骨架（1 天）**
1. 建 `Stages/Stage_Explore.prefab`，掛新的 `ExploreStageController : StageController`。
2. **相機改正交（C10）**：`Main Camera` 設 Orthographic。舊的 FOV zoom 進場效果**直接丟棄**（開發者指名封存），若之後要進場效果再用 `orthographicSize` 重做。
3. `Canvas_Popup` 的彈窗改用新寫的 `PopupService`（取代 `UIManager`）。

**4b. 互動物件與判定（1.5 天）**
4. 依 §4.4 寫 `IInteractable` 體系：`ChestInteractable`（直開／需鑰匙）、`NPCInteractable`、`PasserbyInteractable`。
5. 接上 `ProbabilityCheck` 與 `AttributeChartData`（C4、C17、C19）。**相剋倍率 1/0.5/0，不要加 2× 獎勵層**。
6. **繼續探索迴圈（C13）**：房間清空後跳「要探索其他的東西嗎？」，YES 留在房間、NO 才走離開流程。

**4c. 打牌環節 + hover 預覽（1.5 天，C17／C18 — 本階段最容易做錯）**
7. 寫 `DialogueEncounterController`：主要目標選定、回合制出牌、衰減、結束鈕。
8. 重寫卡牌拖曳（取代兩套舊的），`OnPointerEnter` → `HoverPreviewBroadcaster.Begin(card)`，**但已選定主要目標時不廣播**（C18①）。
9. 每個 `IProbabilityTarget` 實作 `ShowPreview(rate, eff)` —— 對應草圖的 `A 50 / B 50 / C 50`。預覽數字**必須反映當前衰減值**。
10. 出牌結果即時寫入**選項內文 + 角色對話框**（C18③）。
11. `None (0×)` 顯示為不可用（打叉／灰底），不要顯示 0%。

> ⚠️ **本階段三個必檢項**
> - 判定成功時**不可**自動結束環節 —— 蓄意失敗是合法玩法（C18⑦）
> - 衰減掛在**選項**上，不是掛在卡片上
> - 預覽與擲骰共用 `CalculateRate`，不可各算各的

**4d. 房間生成與離開（0.5 天）**
12. **Spawn slot（C6）**：`RoomPopulator` 依 `SpawnSlot[]` 隨機選位置與角度填入寶箱。
13. **鑰匙（C7）**：上鎖寶箱檢查 `RunContext.HasItem(keyId)`。
14. **兩段式離開（C14）**：`ExitTag` 上**保留既有的 `BookmarkHover`**（第一段已完成），**新增** `TwoStageConfirm` 補第二段 → `NotifyStageComplete()`。
15. 探索牌組存回 `RunContext.exploreDeck`。
16. ExploreScene → `_Archive/`。

**驗收**：地圖下拉 → 選節點 → 地圖收起 → 進房間（正交視角、寶箱位置每次不同）→ **hover 手牌時所有選項同時顯示各自機率** → 選定主要目標後預覽收斂 → 出一張牌 → 選項內文與對話框即時更新 → **成功了也不會自動結束** → 再出一張（機率已衰減）→ 按結束鈕 → 「要探索其他的嗎」選 NO → hover ExitTag 下拉 → 再點確認 → **地圖自動下拉**。牌組與鑰匙跨房間保留。

---

### Phase 5 — 接入戰鬥 Prefab（1 天，可在任一階段後插入）

C11：Romtyui 尚未完成，**每次要測試時由我方自行從他的 Scene 打包**。這不是等待交付，而是我方的例行動作，因此本階段可視需要提前或延後。

1. 從他的 Scene 打包（步驟見 §7.1），**檢查有無 Camera / EventSystem / AudioListener**。
2. 包一層 `BattleStageController : StageController`，掛在 prefab **外層空物件**上 —— 不動 prefab 內容，下次重新打包才能直接覆蓋。
3. 接戰鬥結束訊號 → `GameFlowManager.Instance.NotifyStageComplete(...)`。事件未就緒前先用輪詢 adapter（§7.2）。
4. 戰鬥節點入口改由 `GameFlowManager.EnterNode()` 驅動（`EnemyInteractable` 已封存）。
5. Build Settings 最終只留 **1 個場景**：`EventScene`。

**驗收**：探索遇敵 → 戰鬥 → 結算 → **地圖自動下拉**（C1/C2）→ HP/牌組正確延續。

---

### Phase 6 — 事件對話與商店（2 天）

1. **對話系統**：EventScene 現有的 `dialogbox` / `character` / `name_box` / `option_box` / `answer_1~3` 是完整的 UI 骨架，可直接沿用。新寫 `DialogueData`(SO) + `DialogueStageController`。⚠️ `DialogueOptionInteractable` 已封存，選項改實作 `IProbabilityTarget` 才能吃到屬性預覽與衰減。
2. `answer_1~3` 正好對應草圖的三列 `A / B / C`，預覽數字直接顯示在各自的選項列上。
3. **商店（C15）**：`Stage_Shop.prefab`，購買後**留在商店可繼續買**，離開走同一套 `BookmarkHover` + `TwoStageConfirm`。

> **新手介紹（C9）由 Romtyui 製作**（開發者答覆 Q5）。我方只在 `StageType` 保留 `Intro` 列舉值與 `StageHost` 的掛載位置，不實作內容。

---

### Phase 7 — 特殊事件：授予神牌（C16，範圍已大幅縮小）

開發者答覆 Q9：**神牌是純戰鬥牌**，在戰鬥中使用，取得條件是特殊事件分支。

因此這是 Romtyui 的卡牌型別（`CardData`，非 `CardDataExplore`），**我方不需要實作神牌本身**，只需要：

1. `Stage_SpecialEvent.prefab` —— 特殊事件的呈現（美術與動畫已存在於 `Romtyui/Card_frame/卡面/神牌/`、`Romtyui/Anim/神牌動畫/`）。
2. 授予動作：把指定的神牌 `CardData` 加入 `RunStateManager.savedDeck`，戰鬥開始時自然帶入。
3. 地圖節點需要新增 `SpecialEvent` 節點類型。

> 未定：神牌的具體效果與平衡屬於戰鬥設計，歸 Romtyui。我方只負責「什麼時候給、給哪一張」。

---

## 7. 與 Romtyui 的邊界契約

**C11**：戰鬥以 Prefab 形式進入 EventScene，不再 additive 載入，也不需要 `BattleGate.cs`。戰鬥變成 `StageHost` 掛載的其中一個 Stage prefab，和探索、商店走完全相同的路徑。

**重要前提（v3 修正）**：Romtyui 尚未完成，**每個階段要測試時由我方自行從他的 Scene 打包放進來**。所以這不是「等他交付」的被動流程，而是我方可隨時執行的例行動作 —— Phase 5 因此不阻擋任何其他階段。

### 7.1 自行打包步驟

每次要測試戰鬥時重跑一遍（建議寫成 Editor 腳本自動化）：

1. 開啟他的戰鬥 Scene，選取戰鬥根物件，拖成 prefab 放入 `TYN/Stages/Battle/`。
2. **檢查並移除**以下四項 —— EventScene 已各有一份，重複會出事：

| 項目 | 重複的後果 |
|---|---|
| Camera | 畫面渲染錯亂、深度衝突 |
| EventSystem | **UI 輸入完全失效**（最常見、最難查） |
| AudioListener | Unity 持續噴警告，音訊行為未定義 |
| 額外的 Directional Light | 光照疊加變亮 |

3. Canvas 改 Overlay 或設 order 100，掛到 `Canvas_Stage` 底下（避免蓋掉 Popup / Tooltip 層）。
4. 確認根物件 `SetActive(false)` 後不殘留協程與靜態狀態 —— Stage 切換靠啟停。
5. 確認內部沒有 `SceneManager.*` 呼叫（鐵則 1）。

> 我方的 `BattleStageController` 一律掛在 prefab **外層的空物件**上，不寫進他的 prefab。這樣下次重新打包可以直接覆蓋，不會蓋掉我們的接線。

### 7.2 唯一必要的程式協調：戰鬥結束事件

⚠️ **Prefab 化本身不解決這個問題。** 現在偵測戰鬥結束是輪詢 `battleManager.gameObject.activeSelf`（[BattleToMapBridge.cs:36](../TESTING/BattleToMapBridge.cs)），依賴他的內部實作細節，而且**分不出勝、敗、逃跑**。改成 prefab 後這個輪詢依然存在，只是輪詢對象換了。

**請他在打包時順手加上：**

```csharp
// Romtyui/codes/Battle/BattleManager.cs
public event System.Action<BattleOutcome> OnBattleEnded;

// 勝利結算後：OnBattleEnded?.Invoke(BattleOutcome.Victory);
// 玩家死亡時：OnBattleEnded?.Invoke(BattleOutcome.Defeat);
// 逃跑成功時：OnBattleEnded?.Invoke(BattleOutcome.Escaped);
```

**時機**：他還在開發中（C11），**現在提比完工後回頭改便宜得多**，而且我方每次重新打包都能直接受益。收益是流程從「猜」變成「知道」，且有了 `Defeat` 才做得出 Game Over —— 流程圖上戰鬥線只畫到「結算 → 地圖下拉」，打輸的分支目前空白（見 Q4）。

**在他加之前**：把輪詢包進 `BattleStageController` 當 adapter，功能不變，但爛味道關進單一檔案，日後替換只需改一個方法。**這不阻擋任何階段的進度** —— 且因為我方是自行打包（§7.1），adapter 隨時可用。

### 7.3 已有的共用資產（直接沿用，不需協調）

- `RunStateManager`：已有 `DontDestroyOnLoad`，負責跨戰鬥的 HP / 能量 / 牌組存續。`RunContext` **不重複做這件事**，只在進出戰鬥時與它同步（分工見 §8 風險表）。
- `EnemyFormationData` / `EnemyDatabase`：地圖節點指定敵人組合時直接引用。

---

## 8. 風險與對策

| 風險 | 影響 | 對策 |
|---|---|---|
| **正交相機使 FOV 轉場靜默失效**（C10） | **高 — 不報錯，最難察覺** | `ExplorationManager.cs:108,113,135` 三處 `fieldOfView` 全部改 `orthographicSize`，且插值方向相反（size 小 = 近）。Phase 4 必做 |
| Prefab 化時 Inspector 引用大量斷開 | 高 — 最容易卡住的地方 | 一次只做一個 Stage；拉 prefab 前先截圖 Inspector；prefab 化後逐欄位對照重接 |
| 戰鬥 prefab 自帶 Camera / EventSystem / AudioListener | 高 — UI 輸入會完全失效 | §7.1 打包步驟第 2 步；因為是我方自行打包，把檢查寫成 Editor 腳本可根治 |
| **hover 預覽的數字與實際擲骰不一致** | 高 — 玩家會覺得遊戲在騙人 | `CalculateRate` 與 `Roll` 硬性共用同一函式（§5.1）；寫一個測試同時比對兩者輸出 |
| **封存後 Unity 噴大量 missing script** | 中 — 看起來像壞掉，其實是預期 | Phase 0 分兩批封存（見該階段說明）；或接受紅字，Phase 1–4 補完後歸零 |
| 屬性/相剋數值未定案就寫死在程式 | 中 — 之後改要動程式 | 一律放 `AttributeChartData` ScriptableObject，Inspector 可調（Q7、Q8） |
| 重試迴圈做成無限重試 | 中 — 機率系統失去意義 | Q10 未定前，重試一律消耗手牌 |
| `RunContext` 與 `RunStateManager` 職責重疊 | 中 — 兩份 HP 不同步 | 明確劃線：**HP / 能量 / 戰鬥牌組以 `RunStateManager` 為準**；`RunContext` 只管地圖拓撲、探索牌組、鑰匙道具 |
| 判定服務統一後行為改變 | 中 — 手感可能跑掉 | `ProbabilityCheck` 先原樣搬移兩套公式並各自標注，待 Q2 確認後才合併 |
| 地圖 Canvas 改 Overlay 後排版跑掉 | 低 | Overlay 與 Camera 模式的 RectTransform 計算一致，通常只需重設 Canvas Scaler 參考解析度 |
| MapOverlay 常駐導致節點在 Stage 中仍可點 | 中 — 玩家能在探索中途跳關 | `SlideUp()` 後把 MapOverlay 的 `CanvasGroup.blocksRaycasts` 設為 false |
| `.unity` / `.prefab` 是 YAML，git 難以合併 | 中 | 遷移期間講好「EventScene 只有我改」；`.gitattributes` 加 `*.unity binary` 避免無效自動合併 |

---

## 9. 成果對照

| 指標 | 現在 | 整合後 |
|---|---|---|
| Build Settings 場景數 | 6（4 啟用） | **1** |
| TYN 腳本數 / 行數 | 36 個 / 3023 行 | **約 31 個 / 約 2600 行**（砍 1600 行遺留，加 1200 行新架構） |
| 卡牌拖曳實作份數 | **2 套平行實作** | **1 套** |
| 機率判定實作份數 | 2 套不一致公式 | **1 套**（`ProbabilityCheck`） |
| 黑幕實作份數 | 2 份互搶 | **1 份** |
| 硬編碼場景名 | 6 個檔案、9 處 | **0** |
| 呼叫 `SceneManager` 的檔案 | 6 | **0** |
| 死碼 | `ExplorationInteractableTarget`（型別對不上被跳過）、`CardHoverUIExplore`（整份註解掉） | **0** |
| run 狀態存放位置 | UI 腳本的 public 欄位 | **`RunContext` 純資料** |
| 與戰鬥的接觸面 | 3 個檔案雙向呼叫 | **1 個 wrapper 單向** |
| 判定是否支援重試（C12） | ❌ 一次性旗標鎖死 | ✅ |
| 屬性相剋與 hover 預覽（C17） | ❌ 不存在 | ✅ |

---

## 10. 待決事項

以下問題會影響實作，但**不阻擋 Phase 0–4**。標注階段為「最晚需要答案」的時間點。

### 10.1 已解答（2026-08-08）

| # | 問題 | 答覆 |
|---|---|---|
| Q1 | 判定失敗之後接什麼 | 「在試一次？」YES 重試 / NO 離開 → **C12** |
| Q2 | 「多張、機率不疊加」的語意 | **每回合出一張**、可連續出、成功率逐次衰減、玩家按結束鈕收尾。「不疊加」= 機率不會相加，非同時投多張 → **C18** |
| Q3 | 流程圖底部被截斷的分支 | 商店可重複購買（C15）、特殊事件→獲得神牌（C16） |
| Q4 | 戰鬥打輸的流程 | **死亡 → 下個輪迴**。另有**遺產機制**在規劃中（見 10.3） |
| Q5 | 新手介紹的形式 | **由 Romtyui 製作**，我方只保留位置 |
| Q6 | 地圖下拉時背後畫面 | **保留** |
| Q7 | 屬性有哪些 | 先用佔位值，待命名 |
| Q8 | 相剋倍率 | **1× / 0.5× / 0×**，中間層是「模糊相關」，用意是避免抽到的卡完全不能操作 → **C19** |
| Q9 | 神牌 | **純戰鬥牌**，戰鬥中使用，由特殊事件分支取得 → Phase 7 |
| Q10 | 重試的代價 | 成功倍率衰減，次數 = 手牌總數，出過的牌會消耗 → **C18⑤** |

### 10.2 仍待定（不阻擋 Phase 1–3）

| # | 問題 | 最晚需要 | 暫行做法 |
|---|---|---|---|
| ~~**Q7a**~~ | 屬性的**實際名稱與數量** | ~~Phase 4b 收尾~~ | ✅ **已定案 2026-08-15**：無(黑白)/直覺(紅)/邏輯(藍)/批判與創造(綠)。相剋見 `AttributeChart.asset` |
| **Q11** | **衰減級距**：每次降多少？線性（1→0.8→0.6…）還是比例（每次 ×0.7）？ | Phase 4c | 先做線性，級距 = `1 / 手牌總數`，讓最後一張剛好接近 0 |
| **Q12** | 「主要目標」選定後**能否更換**？換了之後衰減進度如何處理？ | Phase 4c | 先允許自由更換，各目標保有各自的衰減進度 |
| **Q13** | 打牌環節結束後，**未使用的手牌**如何處理？保留到下個事件還是棄掉？ | Phase 4c | 先棄掉並在下個事件重抽 |

### 10.3 未來規劃（尚未排期）

- **遺產機制**（Q4）：戰敗死亡後進入下個輪迴時，有部分內容延續。這會影響 `RunContext` 的生命週期設計 —— 目前 `RunContext` 是「一場 run 結束就整個丟棄」，若要做遺產，需要抽出一層 `MetaProgressData` 跨 run 保存。**現在不需實作，但 Phase 1 設計 `RunContext` 時預留這個切分點，日後加起來會便宜很多。**

---

## 附錄 A — 硬編碼場景名清單（全部刪除，不需替換）

> v2 更新：因戰鬥改 Prefab 交付，專案最終只有一個場景，這些呼叫**全部刪除**而非替換為常數。

| 檔案:行 | 內容 |
|---|---|
| `TESTING/Menu_TrickButton/SceneLoader.cs:9` | `LoadScene("UIScene")` |
| `Explore/MAP/map_new0621/MapBannerUI.cs:63` | `LoadScene(menuSceneName)` |
| `Explore/MAP/map_new0621/PerspectiveMapGenerator.cs:81` | `menuSceneName = "MenuScene"` |
| `Explore/MAP/map_new0621/PerspectiveMapGenerator.cs:404,473,498` | `LoadSceneAsync(..., Additive)` |
| `Explore/MAP/map_new0621/PerspectiveMapGenerator.cs:507,526` | `UnloadSceneAsync(...)` |
| `Explore/Scripts/MapNodeExplore.cs:19` | `targetSceneName = "ExploreScene"` → 改為 `nodeType` 驅動 |
| `TESTING/EnemyInteractable.cs:16,97,98` | `battleSceneName = "BattleScene"` |
| `Romtyui/codes/BattleLeaveButtonUI.cs:66` | `returnSceneName` — **隊友的檔案，需協調** |
| `Romtyui/codes/NewGameButtonUI.cs:56` | `startSceneName` — **隊友的檔案，需協調** |
| `Romtyui/codes/Units/OptionMenuUI.cs:265` | `mainMenuSceneName` — **隊友的檔案，需協調** |

> 附錄 A 最後三項屬於 Romtyui 資料夾。它們目前指向 `MenuScene`，Phase 2 之後該場景會被封存。**必須在 Phase 2 完成前告知隊友把目標改成 `EventScene`**，否則從戰鬥按「回主選單」會載入到已封存的場景。

## 附錄 B — 建議的最終資料夾結構

```
Assets/TYN/
├── Core/            ← 新增：流程總管、資料層、轉場、判定服務、屬性系統
├── Stages/          ← 新增：Stage_Menu / Stage_Intro / Stage_Explore / Stage_Shop prefab
│   └── Battle/      ←   從 Romtyui 的 Scene 打包來的戰鬥 prefab + 我方 wrapper
├── Map/             ← MapOverlay prefab 與地圖素材（原 Explore/MAP）
├── Menu/            ← 選單素材（含原 TESTING/Menu_*）
├── Explore/         ← 探索：CARD / INTERACTION / NODE / PREFAB / Scripts
├── UI/              ← 共用 UI 素材（游標、對話框、立繪）
├── Docs/            ← 本文件 + 原「探索測試資料夾」的 docx
├── EventScene.unity ← 唯一場景
└── _Archive/        ← 21 個封存腳本、舊場景、Card_data v1/v2
                       ⚠️ 不得被任何新場景或 prefab 引用
```

**`TESTING/` 資料夾解散** —— 它混雜了正式素材（`Menu_*` 的 shader 效果、`RCard/` 的卡牌資料）與實驗殘骸（`BattleToMapBridge`、`EnemyInteractable`），是這次遺留混亂的主要來源之一。日後不要再開這種「暫時放一下」的資料夾。

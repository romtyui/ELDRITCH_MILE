# Phase 3 — 地圖改為常駐覆蓋層 操作指引

> 對應 `SceneConsolidationPlan.md` §6 Phase 3。程式已完成（編譯 0 error），本文涵蓋編輯器操作。
>
> 版本：2026-08-08 · 預估 1.5–2 小時
>
> **這是整個整合裡最關鍵的一關** —— 做完之後，「狀態隨場景死亡」這個最大的病根就根治了。

---

## 這一關做了什麼

| 舊架構 | 新架構 |
|---|---|
| `PerspectiveMapGenerator` 一人分飾五角：資料層＋生成＋UI＋場景載卸＋黑幕（592 行） | 拆成四塊，各自單一職責 |
| `MapData` 是 UI 腳本的欄位 → 地圖一停用進度就沒了 | `RunContext.mapData` 純資料，地圖 UI 可自由開關 |
| 地圖是要「切換過去」的場景 | 地圖是**常駐覆蓋層**，下拉／收起 |
| 節點連線靠讀 `transform.localPosition` 排序 | 改用 `xPercent`，與畫面無關、不依賴 layout 時機 |
| `UnityEngine.Random` 全域狀態 | `System.Random` + seed，同 seed 必得同一張地圖 |

### 新增的程式

| 檔案 | 位置 | 職責 |
|---|---|---|
| `MapGenerationSettings.cs` | `Core/` | 生成參數 ScriptableObject |
| `MapGenerator.cs` | `Core/` | 純邏輯生成 `MapData`，不碰 UI |
| `MapView.cs` | `Map/` | 把 `MapData` 畫出來 + 棋子移動 |
| `MapNodeUI.cs` | `Map/` | 單一節點 UI |

`MapView` 繼承 `MapOverlayController`，所以它**同時就是**地圖覆蓋層 —— `[MAP_OVERLAY]` 上原本掛的 `MapOverlayController` 要換成 `MapView`。

---

## 步驟 1 — 建立生成參數資產

1. `Assets/TYN/Core/` 右鍵 → `Create → Eldritch → Map Generation Settings`
2. 命名 `MapGenerationSettings`

| 欄位 | 型別 | 建議值 | 說明 |
|---|---|---|---|
| `Map Layers` | int (2–12) | `5` | 總層數，含起點與 Boss |
| `Start Node Count` | int (1–5) | `2` | 第 0 層節點數 |
| `Mid Layer Min` | int | `2` | 中間層下限 |
| `Mid Layer Max` | int | `3` | 中間層上限 |
| `Combat Chance` | float (0–1) | `0.55` | 中間層是戰鬥的機率 |
| `Shop Chance` | float (0–1) | `0.15` | 商店（C15） |
| `Special Event Chance` | float (0–1) | `0.10` | 特殊事件／神牌（C16） |
| `Vertical Margin` | float | `10` | 上下留白百分比 |
| `Horizontal Margin` | float | `20` | 左右留白百分比 |
| `Horizontal Jitter` | float | `5` | 水平隨機抖動 |
| `Use Demo Route` | bool | `☐` | 勾選改用固定路線 |
| `Demo Route Kinds` | `List<MapNodeKind>` | 預設 4 筆 | DEMO 時每層的類型 |

> 剩餘機率自動歸 `Event`。起點層固定是 `Event`（避免一開場就被迫戰鬥），最後一層固定是 `Boss`。

3. 選 `[SYSTEM] → GameFlowManager`，把新欄位 **`Map Settings`** 拖上這個資產

---

## 步驟 2 — 把節點 Prefab 換成新腳本

舊的 `NodeUI_Normal` / `NodeUI_Battle` / `NodeUI_Boss` 三個 prefab 在 `Assets/TYN/Explore/MAP/map_new0621/`，上面掛的是舊的 `PerspectiveNode`。

對**每一個** prefab：

1. 雙擊進 prefab 編輯模式
2. 根物件 → **移除 `Perspective Node`**
3. `Add Component` → **`Map Node UI`**
4. 重新設定欄位：

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `Node Icon` | `Image` | 拖節點的圖示 Image（通常是自己或子物件） |
| `Color Bright` | Color | 白色 |
| `Color Dim` | Color | 深灰 `(0.3, 0.3, 0.3, 1)` |
| `Alpha Bright` | float | `1` |
| `Alpha Dim` | float | `0.4` |
| `Scale Current` | float | `1.2` |
| `Scale Selectable` | float | `1` |
| `Scale Inactive` | float | `0.8` |
| `Scale Hover` | float | `1.1` |

> 根物件需要 `CanvasGroup` —— `MapNodeUI` 有 `[RequireComponent]`，會自動補上。

---

## 步驟 3 — `[MAP_OVERLAY]` 換成 MapView

1. 開 `EventScene`，選 `[MAP_OVERLAY]`
2. **移除 `Map Overlay Controller`**
3. `Add Component` → **`Map View`**

> `MapView` 繼承 `MapOverlayController`，所以下拉／收起的欄位都還在，只是多了地圖繪製的部分。

4. 把 Phase 1 填過的滑動欄位重填（換元件會清空）：

| 欄位 | 型別 | 值 |
|---|---|---|
| `Panel` | `RectTransform` | 拖 `MapPanel` |
| `Hidden Y` | float | `1080` |
| `Shown Y` | float | `0` |
| `Slide Duration` | float | `0.5` |
| `Slide Curve` | `AnimationCurve` | 預設 EaseInOut |
| `Canvas Group` | `CanvasGroup` | 拖 `MapPanel` 上的 CanvasGroup |

5. 新增的地圖欄位：

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `Map Container` | `RectTransform` | 見步驟 4 |
| `Event Node Prefab` | `GameObject` | `NodeUI_Normal.prefab` |
| `Combat Node Prefab` | `GameObject` | `NodeUI_Battle.prefab` |
| `Boss Node Prefab` | `GameObject` | `NodeUI_Boss.prefab` |
| `Shop Node Prefab` | `GameObject` | 留空（自動沿用 Event） |
| `Special Event Node Prefab` | `GameObject` | 留空（自動沿用 Event） |
| `Line Prefab` | `GameObject` | `LineUI.prefab` 或 `ArrowUI.prefab` |
| `Line Size` | Vector2 | `7, 25` |
| `Line Display` | `LineDisplayMode` | `VisitedPlusReachable`（預設，見下） |
| `Player Avatar` | `RectTransform` | 見步驟 4 |
| `Avatar Offset` | Vector2 | `40, -20` |
| `Avatar Move Duration` | float | `0.8` |
| `Bobbing Height` | float | `25` |
| `Bobbing Frequency` | float | `2` |
| `Node Pop Duration` | float | `0.3` |
| `Layer Pop Interval` | float | `0.05` |
| `Map Banner UI` | `MapBannerUI` | 見步驟 4（可留空） |
| `Map Enter Text` | string | `<color=#FFFFFF>地圖</color>` |

### 連線顯示模式（`Line Display`）

所有連線在建圖時就全部生成好，這個欄位只控制哪些顯示 —— 切換不會重建，隨時可改。

| 模式 | 畫什麼 | 取捨 |
|---|---|---|
| `AllConnections` | 整張路網 | 能提前規劃路線（Slay the Spire 那種），但失去未知感，節點多時畫面很雜 |
| `VisitedPathOnly` | **只有走過的軌跡** | 畫面乾淨、保留未知感，符合恐怖探索調性。玩家對前方一無所知 —— 不過「可前往」的節點本來就是亮的，資訊沒有真的消失，只是不用線表達 |
| `VisitedPlusReachable` | 走過的軌跡 **+** 從當前節點通往可去節點的線 | **預設**。保留未知感，同時用線明確指出「你現在能去哪」，不必依賴玩家看得懂節點的明暗差異 |

> 建議先用預設的 `VisitedPlusReachable` 跑一次，覺得前方提示太多再切 `VisitedPathOnly`。

---

## 關於 `map_image` 不在 `MapContainer` 裡

**沒有影響，而且這樣是對的。**

`MapView.PercentToLocal()` 只讀 `mapContainer.rect` 來換算節點座標，跟背景圖放在哪無關。兩者職責分開：

- **`MapContainer`** = 節點的座標空間（決定節點撒在哪個範圍）
- **`map_image`** = 純背景美術（決定畫面看起來如何）

事實上背景圖**不該**放進 `MapContainer` —— 因為本專案的地圖是 2048×2048 正方圖旋轉 45° 成菱形，若兩者同層，節點會繼承那個旋轉而全部歪掉。

**唯一的硬性要求**：`MapContainer` 自己的 **Rotation 必須是 0、Scale 必須是 1**。背景圖要轉幾度、縮放多少都隨意，但容器不行 —— 否則 `PercentToLocal` 算出來的位置會偏。

節點若跑到菱形外面，兩個調法：
1. 縮小 `MapContainer` 的 Width / Height，讓它落在菱形內部
2. 調大 `MapGenerationSettings` 的 `Horizontal Margin` / `Vertical Margin`

---

## 步驟 4 — 從 UIScene 搬地圖內容

開 `Assets/TYN/UIScene.unity`，裡面的地圖 UI 結構大致是：

```
MapCanvas
└── MapContainer
    ├── map_image
    ├── Player Avatar
    └── (節點與連線在執行時生成)
MapBanner
```

**搬法**（一次一個，用複製貼上）：

1. 選 `MapContainer` → Ctrl+C
2. 開 `EventScene`，選 `MapPanel` → Ctrl+V
3. 同樣把 `MapBanner` 複製到 `MapPanel` 底下
4. `Player Avatar` 應該已隨 `MapContainer` 一起過來

貼完後回 `[MAP_OVERLAY]` 的 `MapView`，把三個引用補上：

| 欄位 | 拖什麼 |
|---|---|
| `Map Container` | 剛貼過來的 `MapContainer` |
| `Player Avatar` | `MapContainer` 底下的 `Player Avatar` |
| `Map Banner UI` | 剛貼過來的 `MapBanner` |

> ⚠️ **不要複製 `MapCamera`**。地圖改用 Screen Space Overlay，`[CAMERA]` 已有唯一的正交主相機。
>
> ⚠️ **不要複製 `TransitionCanvas` / `black` / `blockraycast_*`**。黑幕已由 `[SYSTEM] → ScreenFader` 統一負責。
>
> ⚠️ `MapContainer` 的 RectTransform 建議設 **stretch / stretch、offset 全 0**，`MapView` 會依它的 `rect` 換算節點座標。若它尺寸是 0，節點會全部疊在中心。

---

## 步驟 5 — 封存舊的地圖腳本

新舊兩套目前並存（新的放在 `EldritchMile.Map` 命名空間所以不衝突），但舊的已經是死碼。

在 **Unity 的 Project 視窗裡**把這兩個檔案拖到 `Assets/TYN/_Archive/Scripts/`：

- `Assets/TYN/Explore/MAP/map_new0621/PerspectiveMapGenerator.cs`
- `Assets/TYN/Explore/MAP/map_new0621/PerspectiveNode.cs`

> **一定要在 Unity 裡面拖**，不要用檔案總管 —— Unity 會連 `.meta` 一起處理，GUID 才不會斷。

順便把保留的兩個搬去 `Assets/TYN/Map/` 歸位（可選）：
- `MapBannerUI.cs`

搬完後，`MapBannerUI.cs` 裡標記 `[已棄用]` 的 `ShowEndGame(message, menuSceneName)` 就可以刪掉了 —— 它是唯一還會呼叫 `SceneManager` 的地方。

---

## 步驟 6 — 封存 UIScene

1. `File → Build Profiles` → 取消勾選 `UIScene`
2. 驗收通過後，在 Unity Project 視窗把 `UIScene.unity` 拖到 `_Archive/Scenes/`

---

## 步驟 7 — 驗收

按 Play：

- [ ] 主選單出現
- [ ] 按 START → Console 印出
      `[Flow] 開始新的一場 run（seed …）：N 個節點、5 層`
      `[地圖] 已繪製 N 個節點`
- [ ] 地圖**從畫面上方滑下來**
- [ ] 節點**逐層彈出**（由下往上，每層有回彈）
- [ ] 「地圖」橫幅淡入淡出
- [ ] 玩家棋子出現在第 0 層下方
- [ ] 第 0 層節點是亮的可點，其他是暗的
- [ ] 點一個節點 → 棋子**走過去且有上下起伏**
- [ ] 棋子到位後畫面淡黑 → 地圖收起
- [ ] Console 印出 `[StageHost] 找不到 Explore 的 prefab`（**預期**，Phase 4 才做）

**最關鍵的驗收 —— 狀態保存**：

因為 Explore Stage 還不存在，`NotifyStageComplete` 不會被呼叫，所以暫時用這招測：

- [ ] 執行中，在 Hierarchy 手動把 `MapPanel` 的 `Pos Y` 從 `1080` 改回 `0`
- [ ] 地圖重新出現時，**節點拓撲、走過的路徑、棋子位置全部保留**，而且**沒有重新生成**（Console 不會再出現 `[地圖] 已繪製 …`）

這一項通過，就代表「狀態隨場景死亡」的病根真的解決了。

---

## 常見錯誤

| 症狀 | 原因 | 解法 |
|---|---|---|
| 節點全部疊在正中央 | `MapContainer` 的 rect 寬高是 0 | 設 stretch / stretch、offset 全 0 |
| `[地圖] 沒有指定 mapContainer` | `MapView.Map Container` 沒拖 | 步驟 4 |
| `[地圖] NodeUI_xxx 缺少 MapNodeUI 元件` | prefab 還掛著舊的 `PerspectiveNode` | 步驟 2 |
| 節點出現但點不到 | `MapPanel` 的 CanvasGroup 沒拖給 `MapView`，收起時 raycast 沒被關／開 | 步驟 3 的 `Canvas Group` |
| 點節點沒反應 | 該節點不是「可前往」狀態 | 只有第 0 層（未出發時）或當前節點的 `nextNodeIds` 可點 |
| 棋子不動 | `Player Avatar` 沒拖 | 步驟 4 |
| 地圖每次下拉都重新生成 | `Refresh()` 收到的 `mapData` 每次都是新物件 | 檢查 `StartNewRun` 是否被重複呼叫 |
| 連線歪掉或長度不對 | `Line Prefab` 的 pivot 不是中心 | `MapView` 會強制設 pivot 0.5；若仍不對，檢查 prefab 的 Image 是否有 `Preserve Aspect` |

---

## 完成後

Phase 3 做完，剩下的病根只有「與戰鬥的交接靠輪詢」（Phase 5 處理）。

接著是 **Phase 4 — 探索 Stage**（4 天，最大的一關）：正交相機、spawn slot、屬性相剋、hover 全選項預覽、C18 打牌回合制。

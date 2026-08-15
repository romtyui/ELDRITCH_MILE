# Phase 1 — EventScene 骨架搭建操作指引

> 對應設計文件 `SceneConsolidationPlan.md` §4.2。程式部分已完成並通過編譯驗證，本文只涵蓋**必須在 Unity 編輯器內手動完成**的部分。
>
> 版本：2026-08-08 · 適用 Unity 6000.4.1f1

---

## 前置確認

開啟 `Assets/TYN/EventScene.unity`，確認：

- [ ] Console 沒有紅字
- [ ] `Assets/TYN/Core/` 底下有 17 個 `.cs` + 1 個 `AttributeChart.asset`
- [ ] 點一下 `AttributeChart.asset`，Inspector 能正常顯示欄位（不是 `Missing Script`）

> `AttributeChart.asset` 已預先建好，數值即 C19 定案的 `1 / 0.5 / 0`。若 Inspector 顯示異常，刪掉它，改用選單 `Assets → Create → Eldritch → Attribute Chart` 重建，再依 §3 填值。

---

## 步驟 1 — 清掉測試按鈕

EventScene 裡現有的 `ENCOUNTER` / `EXPLORE` / `GOD` / `MAP` / `SHOP` 五個測試按鈕已無用途（流程改由 `GameFlowManager` 驅動），**刪除**。

`dialogbox` / `character` / `name_box` / `option_box` / `answer_1~3` / `black_background` **保留** —— Phase 6 的對話系統會用到。可以先全部收進一個暫時的空物件 `_TEMP_DialogueUI` 並停用，避免干擾骨架搭建。

`ExitTag`（掛 `BookmarkHover`）**保留不動**，Phase 4d 會用到。

---

## 步驟 2 — 搭建骨架

目標結構：

```
EventScene
├── [SYSTEM]
│   ├── GameFlowManager
│   ├── ScreenFader
│   │   └── Black
│   └── ProbabilityCheck
├── [CAMERA]
│   └── Main Camera
├── EventSystem                （已存在，只需檢查）
├── [STAGE_HOST]
│   └── WorldRoot
├── [MAP_OVERLAY]
│   └── MapPanel
└── [UI_ROOT]
    ├── Canvas_Stage
    ├── Canvas_Popup
    └── Canvas_Tooltip
```

方括號的都是**空物件**（`GameObject → Create Empty`），Transform 歸零，只作分類用。

### 2.1 `[UI_ROOT]` — 先做這個，後面要引用

建空物件 `[UI_ROOT]`，底下建三個 Canvas（`GameObject → UI → Canvas`）。三個都掛同樣的元件，只有 **Sort Order** 不同：

| 物件 | 元件 | 欄位 | 型別 | 值 |
|---|---|---|---|---|
| `Canvas_Stage` | Canvas | Render Mode | enum | `Screen Space - Overlay` |
| | | Sort Order | int | **100** |
| | Canvas Scaler | UI Scale Mode | enum | `Scale With Screen Size` |
| | | Reference Resolution | Vector2 | `1920 × 1080` |
| | | Match | float (0–1) | `0.5` |
| | Graphic Raycaster | — | — | 預設 |
| `Canvas_Popup` | 同上 | Sort Order | int | **500** |
| `Canvas_Tooltip` | 同上 | Sort Order | int | **800** |

> 三個 Canvas 的 **Reference Resolution 必須一致**，否則同一個 UI 在不同層會有不同縮放。

### 2.2 `[SYSTEM]`

建空物件 `[SYSTEM]`，底下三個子物件。

#### `GameFlowManager`（空物件 + 腳本）

掛 `GameFlowManager`：

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `Stage Host` | `StageHost` | 拖 `[STAGE_HOST]`（步驟 2.4 建好後回來拖） |
| `Map Overlay` | `MapOverlayController` | 拖 `[MAP_OVERLAY]`（步驟 2.5 建好後回來拖） |
| `Boot Stage` | `StageType` 列舉 | Phase 1 測試先選 **`None`**；Phase 2 完成後改回 `Menu` |
| `Current Stage` | `StageType`（唯讀） | 不用填，執行時顯示 |

> **為什麼先選 `None`**：目前還沒有任何 Stage prefab，選 `Menu` 會在 Console 印一行「找不到 Menu 的 prefab」警告。那是預期行為不是錯誤，但 Phase 1 驗收時選 `None` 畫面比較乾淨。

#### `ScreenFader`（Canvas + 腳本）

這個要 **Canvas + 黑圖** 兩層：

`ScreenFader`（`GameObject → UI → Canvas`）

| 元件 | 欄位 | 型別 | 值 |
|---|---|---|---|
| Canvas | Render Mode | enum | `Screen Space - Overlay` |
| | Sort Order | int | **9000**（要蓋過所有東西） |
| Canvas Scaler | UI Scale Mode | enum | `Scale With Screen Size` |
| | Reference Resolution | Vector2 | `1920 × 1080` |
| Graphic Raycaster | — | — | 預設 |
| **Canvas Group** | — | — | 必加。`ScreenFader` 有 `[RequireComponent]`，會自動補上 |
| `ScreenFader` | `Default Duration` | float | `0.4` |

子物件 `Black`（`GameObject → UI → Image`）：

| 欄位 | 型別 | 值 |
|---|---|---|
| Rect Transform → Anchor Preset | — | **stretch / stretch**（按住 Alt 點右下角那個，讓它撐滿） |
| Left / Top / Right / Bottom | float | 全部 `0` |
| Image → Source Image | Sprite | **留空**（純色即可） |
| Image → Color | Color | `#000000`，**Alpha 設 255** |

> Alpha 一定要 255。透明度由 `CanvasGroup.alpha` 控制，Image 本身若不是全不透明，黑幕會蓋不住。

#### `ProbabilityCheck`（空物件 + 腳本）

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `Chart` | `AttributeChartData` | 拖 `Assets/TYN/Core/AttributeChart.asset` |
| `Verbose Log` | bool | `✓`（開發期建議開，每次判定會印計算過程） |

> `CursorManager` 已經在場景裡（掛在既有物件上），可以順手拖進 `[SYSTEM]` 底下歸類，但**不要新增第二個** —— 它是單例。

### 2.3 `[CAMERA]`

把場景既有的 `Main Camera` 拖進來，並改設定：

| 元件 | 欄位 | 型別 | 值 | 說明 |
|---|---|---|---|---|
| Camera | **Projection** | enum | **`Orthographic`** | C10：探索改正交 2D/2.5D |
| | Size | float | `5`（暫定，Phase 4 依實際美術調） | |
| | Environment → **Background Type** | enum | `Solid Color` | **本專案是 URP，沒有 Clear Flags**，對應欄位改叫 Background Type |
| | Environment → Background | Color | 深色即可 | |
| Audio Listener | — | — | 保留，**全場景只能有一個** | |

> ⚠️ **URP 修正**：Built-in 渲染管線的 `Clear Flags` 在 URP 改成 Camera 元件 **Environment** 區塊裡的 `Background Type`，選項是 `Skybox` / `Solid Color` / `Uninitialized`。

> ⚠️ 舊 `ExplorationManager` 用 `fieldOfView` 做進場 zoom —— 正交相機**沒有 FOV**，那段程式會靜默失效。該腳本已封存，Phase 4 會改用 `orthographicSize` 重寫。

### 2.4 `[STAGE_HOST]`

建空物件 `[STAGE_HOST]`，掛 `StageHost`：

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `World Root` | `Transform` | 拖子物件 `WorldRoot`（見下） |
| `Ui Root` | `Transform` | 拖 `Canvas_Stage`（步驟 2.1 建的） |
| `Stages` | `List<StageEntry>` | **Phase 1 留空**，Phase 2 起逐一加入 |

`Stages` 清單裡每一筆的結構：

| 子欄位 | 型別 | 說明 |
|---|---|---|
| `Type` | `StageType` 列舉 | `Menu` / `Intro` / `Explore` / `Battle` / `Shop` / `Dialogue` / `SpecialEvent` |
| `Prefab` | `GameObject` | 該 Stage 的 prefab，根物件須掛 `StageController` 子類別 |
| `Custom Parent` | `Transform` | 留空則掛在 `Ui Root` 底下。3D 內容（探索房間、戰鬥）才填 `WorldRoot` |

子物件 `WorldRoot`：**空物件**，Transform 歸零。2.5D 房間與戰鬥 prefab 會生成在這底下。

### 2.5 `[MAP_OVERLAY]`

⚠️ 這個結構容易做錯：**腳本掛在 Canvas 上，但被移動的是子物件 `MapPanel`**。Screen Space Overlay 的 Canvas 根 RectTransform 尺寸由畫面決定，移動它沒有效果。

`[MAP_OVERLAY]`（`GameObject → UI → Canvas`）

| 元件 | 欄位 | 型別 | 值 |
|---|---|---|---|
| Canvas | Render Mode | enum | `Screen Space - Overlay` |
| | Sort Order | int | **300**（在 Stage 100 之上、Popup 500 之下） |
| Canvas Scaler | Reference Resolution | Vector2 | `1920 × 1080` |
| Graphic Raycaster | — | — | 預設 |
| `MapOverlayController` | 見下表 | | |

`MapOverlayController` 欄位：

| 欄位 | 型別 | 填什麼 |
|---|---|---|
| `Panel` | `RectTransform` | **拖子物件 `MapPanel`**，不可留空 |
| `Hidden Y` | float | **`1080`**（= MapPanel 的 Height，正好推出畫面上方） |
| `Shown Y` | float | `0` |
| `Slide Duration` | float | `0.5` |
| `Slide Curve` | `AnimationCurve` | 預設 EaseInOut 即可 |
| `Canvas Group` | `CanvasGroup` | 拖 `MapPanel` 上的 CanvasGroup |

子物件 `MapPanel`（`GameObject → Create Empty` 在 Canvas 底下，會自動變 RectTransform）：

| 元件 | 欄位 | 值 |
|---|---|---|
| Rect Transform | Anchor Preset | **top / stretch**（上方那排最右邊那個：橫向撐滿、錨定頂端） |
| | Pivot | `X 0.5`、**`Y 1`** |
| | Left / Right | `0` / `0` |
| | **Pos Y** | `0`（顯示位置） |
| | **Height** | `1080`（等於 Reference Resolution 的高度） |
| **Canvas Group** | — | 手動 `Add Component` 加上 |

> ⚠️ **修正（原文寫成 stretch / stretch 是錯的）**：RectTransform 只要**上下也被拉伸**，Inspector 就會把 `Pos Y` / `Height` 換成 `Top` / `Bottom`，你就沒有 `Pos Y` 可以調。
>
> 改用 **top / stretch + Pivot Y = 1**，語意也更乾淨：
> - `Pos Y = 0` → 面板頂邊貼齊畫面頂端 = **完整顯示**
> - `Pos Y = 1080` → 整塊被推到畫面上方之外 = **完全藏住**
>
> 因此 `MapOverlayController` 的 `Hidden Y` 建議改成 **`1080`**（正好等於 Height），`Shown Y` 維持 `0`。

### 2.6 `EventSystem`

場景已有，只需確認：

| 元件 | 應為 |
|---|---|
| Event System | 存在，且**全場景只有一個** |
| Input Module | 本專案用新版 Input System → 應為 **`InputSystemUIInputModule`**。若顯示 `Standalone Input Module` 並有黃字警告，點警告裡的 **Replace with InputSystemUIInputModule** |

---

## 步驟 3 — AttributeChart 資產說明

`Assets/TYN/Core/AttributeChart.asset` 已建好，欄位如下（C19 定案值）：

| 欄位 | 型別 | 預設值 | 說明 |
|---|---|---|---|
| `Match Multiplier` | float (0–1) | `1` | 相符。**卡片自身機率即上限，沒有 2×** |
| `Partial Multiplier` | float (0–1) | `0.5` | 模糊相關。存在目的是避免手牌完全不能操作 |
| `None Multiplier` | float (0–1) | `0` | 無效。**應少用**，只給設計上要封死的組合 |
| `Rules` | `List<Rule>` | 空 | 相剋規則表，見下 |
| `Default Effectiveness` | `Effectiveness` 列舉 | `Partial` | 查不到規則時的預設 |

`Rules` 每一筆：

| 子欄位 | 型別 | 說明 |
|---|---|---|
| `Card Attribute` | `ExploreAttribute` | 卡片屬性 |
| `Target Attribute` | `ExploreAttribute` | 目標屬性 |
| `Effectiveness` | `Effectiveness` | `Match` / `Partial` / `None` |

**Phase 1 不需要填任何 Rule。** 屬性已定案（Q7a，2026-08-15）：`None`(無/黑白)、`Intuition`(直覺/紅)、`Logic`(邏輯/藍)、`Insight`(批判與創造/綠)。查表邏輯的預設行為：

- 目標屬性為 `None` → 一律 `Match`（不吃相剋）
- 卡與目標同屬性 → `Match`
- 其餘查不到 → `Default Effectiveness`（即 `Partial`）

屬性名稱定案後，只要改 `ExploreAttribute.cs` 的列舉名稱，這張表的內容不受影響。

---

## 步驟 4 — Build Settings

`File → Build Profiles`（Unity 6 改名了，舊版是 Build Settings）→ Scene List：

1. 加入 `Assets/TYN/EventScene.unity`
2. 拖到 **index 0**
3. 其餘場景（`MenuScene` / `UIScene` / `ExploreScene`）**先保留但取消勾選** —— Phase 2/3/4 抽完 prefab 才封存

---

## 步驟 5 — 驗收

按 Play，應該看到：

- [ ] 畫面從**全黑淡出**（`ScreenFader` 開場全黑 → `GameFlowManager` 淡入）
- [ ] Console **無紅字**
- [ ] `Boot Stage` 設 `None` 時無任何警告；設 `Menu` 時有一行「`[StageHost] 找不到 Menu 的 prefab`」，這是**預期行為**
- [ ] 選中 `GameFlowManager`，Inspector 的 `Current Stage` 顯示對應值

再手動測地圖下拉（暫時性）：
- [ ] 執行中選 `MapPanel`，把 `Pos Y` 從 `1080` 改成 `0`，面板應從畫面上方滑進來 —— 確認 `Hidden Y` 足夠藏住

> 若 Inspector 只看到 `Top` / `Bottom` 而沒有 `Pos Y` / `Height`，代表 Anchor Preset 選成上下也拉伸了，回 §2.5 改成 **top / stretch**。

---

## 常見錯誤檢查表

| 症狀 | 原因 | 解法 |
|---|---|---|
| 畫面永遠全黑 | `Black` 的 Image Alpha 不是 255，或 `ScreenFader` 的 `CanvasGroup` 沒掛上 | 檢查 §2.2 |
| 畫面永遠不黑 | `Black` 沒有撐滿（Anchor Preset 不是 stretch） | 按住 **Alt** 再點 Anchor Preset 右下角 |
| 點不到任何 UI | 有兩個 `EventSystem`，或 `ScreenFader` 的 CanvasGroup `Blocks Raycasts` 卡在勾選 | 場景搜尋 `t:EventSystem` 確認只有一個 |
| 地圖收不起來 | `MapOverlayController.Panel` 指到 Canvas 自己而非 `MapPanel` | 見 §2.5 的警告 |
| `NullReferenceException` on `GameFlowManager` | `Stage Host` 或 `Map Overlay` 沒拖 | 兩個欄位都必填 |
| 音訊警告 `There are 2 audio listeners` | 場景有多台相機 | 只保留 `[CAMERA]` 底下那台 |

---

## 完成後

Phase 1 收尾，接著是 **Phase 2 — 主選單**（0.5 天，最輕的一關）：把 MenuScene 的 Canvas 拉成 `Stage_Menu.prefab`、掛 `MenuStageController`、START 按鈕接 `GameFlowManager.Instance.StartNewRun()`。屆時再把 `Boot Stage` 改回 `Menu`。

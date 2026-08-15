# Phase 4c（第一批）— 打牌環節 操作指引

> 對應 `SceneConsolidationPlan.md` §6 Phase 4c。程式已完成（編譯 0 error）。
>
> **本批目標**：拖卡到目標上出牌、hover 時所有選項顯示各自成功率、判定結果即時反映、可連續嘗試且機率逐次衰減。
>
> **本批尚未包含**：主要目標選定 UI、「在試一次？」確認視窗、精緻的結束按鈕（先用陽春 Button 代替）。
>
> 版本：2026-08-08 · 預估 1.5–2 小時 · 前置：Phase 4a 已跑通

---

## 這批做了什麼

| 檔案 | 職責 |
|---|---|
| `CardDataExplore.attribute` | C17 屬性欄位（新增） |
| `Explore/CARD/Scripts/ExploreCardDrag.cs` | 卡牌拖曳 + hover 廣播預覽 |
| `Explore/CARD/Scripts/ExploreHandUI.cs` | 手牌排版 + 出牌轉交 |
| `ExploreStageController.BeginEncounter()` | 開始打牌環節、抽手牌 |
| `ChestInteractable`（`RequiresCheck`） | 點擊 → 開始打牌環節 |

**規則全部留在 Core 的 `DialogueEncounterController`**（Phase 1 就寫好了）。UI 層只負責畫出來與轉交動作，不做任何判斷 —— 所以「成功也不能自動結束」這條規則只有一個地方可能寫錯。

### 完整流程

```
點 RequiresCheck 的寶箱
   │
   ▼ ChestInteractable → stage.BeginEncounter(this)
從 RunContext.exploreDeck 抽 N 張手牌
   │
   ▼
hover 手牌 ──▶ 所有選項顯示各自成功率（C17）
   │
   ▼ 拖到目標上放開
DialogueEncounterController.PlayCard()
   │  擲骰 → 消耗手牌 → 目標衰減 → 通知結果
   ▼
選項內文即時更新，可再出一張（機率已下降）
   │
   ▼ 手牌用盡 或 按結束
環節結束，剩餘手牌棄掉
```

---

## 步驟 1 — 卡牌資料加上屬性

`CardDataExplore` 新增了 `Attribute` 欄位。

打開 `Assets/TYN/Explore/CARD/Card_data/v3/` 底下的卡牌資產，逐一設定：

| 欄位 | 說明 |
|---|---|
| `Success Probability` | 已有。這是**機率上限** —— 相剋只會往下扣，不會加成（C19） |
| **`Attribute`** | `Intuition`(直覺/紅)、`Logic`(邏輯/藍)、`Insight`(批判與創造/綠)。Q7a 已於 2026-08-15 定案 |

> 現有卡是 `explore_{Intuition,Logic,Insight}_{0..100}` 的機率階梯。
> 測試階段建議先讓它們用**不同屬性**，才看得出相剋差異；等屬性定名後再統一調整。

---

## 步驟 2 — 卡牌 prefab 加上拖曳元件

找到卡牌 prefab（`Assets/TYN/Explore/CARD/EP_cardexplore_template.prefab`），確認根物件上有：

| 元件 | 說明 |
|---|---|
| `Card View UI Explore` | 已有。負責卡面顯示 |
| `Canvas Group` | **必要**。`ExploreCardDrag` 有 `[RequireComponent]`，會自動補 |
| `Explore Card Drag` | 新增。若忘了加，`ExploreHandUI` 生成時會自動補上，但先加好比較能在 Inspector 調參數 |

`ExploreCardDrag` 欄位：

| 欄位 | 型別 | 建議 | 說明 |
|---|---|---|---|
| `Play Threshold Pixels` | float | `80` | 拖超過這距離才算出牌，避免手抖誤觸 |
| `Drag Scale` | float | `0.9` | 拖曳時縮小一點，比較看得到底下的目標 |

> 卡片上**只有這兩個**參數。上浮距離（hover / 選取）屬於排版，統一放在 `ExploreHandUI`（步驟 3）——
> 否則調一個要開卡牌 prefab、調另一個要開場景。

### 出牌有兩種操作方式

| 方式 | 操作 | 適合 |
|---|---|---|
| **拖曳** | 把卡拖到目標上放開 | 熟練、求快 |
| **點選兩段** | 點卡片（浮起待命）→ 點目標 | 求穩、避免誤放 |

**為什麼要兩軌並存**：誤放一張牌的代價很重 —— 消耗一張手牌，而且**目標會永久衰減**，收不回來。拖曳快但容易失手，尤其 Phase 6 多選項並排時。這是卡牌遊戲的標準做法（Slay the Spire 同樣兩種都支援）。

再點一次同一張卡可取消選取。選取中會持續顯示預覽，所以玩家能先比較這張打在各目標上分別是多少，再決定點哪個。

> **為什麼不直接用 `TwoStageConfirm`**：那個元件的「啟動」與「確認」都發生在同一個物件上。
> 出牌是**在卡片上啟動、在目標上確認**，跨兩個物件，形狀不合 —— 概念相同但落點不同，所以另外實作。

> ⚠️ 卡牌上若還掛著封存的 `ExplorationCardDragUI` 或 `CardDragUIExplore`，**要移除** —— 兩套拖曳同時作用會打架。

---

## 步驟 3 — 手牌區（放在 EventScene 的對話框旁）

> **手牌與對話框是同一組構圖**（企劃草圖上就是畫在一起的），所以手牌區**常駐在 EventScene**，
> 不放進 `Stage_Explore.prefab`。這樣兩者的相對位置一次調好，之後商店、對話環節也共用同一區。

在 EventScene 的 `dialogbox` 旁邊（同一層）建立：

```
EventScene
├── dialogbox            ← 既有的對話框
└── EncounterUI          ← 新增，打牌時才顯示
    ├── HandRoot         ← 手牌生成在這
    └── Btn_EndEncounter ← 「結束」按鈕
```

### `EncounterUI`

| 設定 | 值 |
|---|---|
| 位置 | 貼齊畫面下緣，與 `dialogbox` 的相對關係依構圖調 |
| 初始狀態 | **停用**（打牌時才開，由 `ExploreHandUI` 自己控制） |
| `UIPanel` 元件 | 選配。若要掛，`Kind` 設 **`Widget`** —— 它由 `ExploreHandUI` 擁有，不該讓 `UIDirector` 也來開關 |

### `HandRoot`

普通 RectTransform，卡片生成在這底下。

> ⚠️ **不要掛 Layout Group** —— `ExploreHandUI` 自己排版，兩者會互相搶。

### 在 `EncounterUI` 上掛 `Explore Hand UI`

| 欄位 | 型別 | 拖什麼 |
|---|---|---|
| `Encounter` | `DialogueEncounterController` | **留空即可**，會自動抓 `Instance` |
| `Card Prefab` | `CardViewUIExplore` | 卡牌 prefab |
| `Hand Root` | `RectTransform` | `HandRoot` |
| `Root` | `GameObject` | **留空即可**，預設用自身（`EncounterUI`） |
| `Card Spacing` | float | `140` |
| `Max Hand Width` | float | `900`（超過會自動壓縮間距，手牌多也不會超出畫面） |
| `Hover Lift` | float | `40`（滑過時上浮） |
| `Selected Lift` | float | `70`（**選取待命**時上浮。要明顯大於 Hover Lift，否則玩家分不出「滑過」與「已選取」） |

### `Btn_EndEncounter`

| 設定 | 值 |
|---|---|
| 文字 | 「結束」 |
| `OnClick` | **留空** —— 見下方說明 |

⚠️ **這顆按鈕的接法要注意**：它住在場景裡，而 `ExploreStageController` 住在 prefab 裡，**場景物件不能在 Inspector 引用 prefab 內的東西**。

兩種接法擇一：

1. **接 `ExploreHandUI`**（建議）：把 `EncounterUI` 拖進 Object 欄 → 選 `ExploreHandUI → RequestEnd ()`
2. 或在 `ExploreStageController.OnStageEnter` 用程式接（目前沒做，需要時再加）

> **這顆按鈕是必要的，不是裝飾。** C18⑦：蓄意失敗是合法策略，所以系統**即使判定成功也不會自動結束**環節。沒有它，玩家只能把手牌全部出完才離得開。

---

## 步驟 4 — 打牌環節控制器（也放 EventScene）

在 `[SYSTEM]` 底下 `Add Component` → **`Dialogue Encounter Controller`**：

| 欄位 | 型別 | 建議 | 說明 |
|---|---|---|---|
| `Decay Scaled To Hand Size` | bool | ✓ | 每次衰減 1/手牌數，最後一張剛好接近 0 |
| `Fixed Decay Step` | float | `0.2` | 上面取消勾選時才用 |

> 它是**規則引擎**，跟手牌區一樣常駐場景。`ExploreStageController` 透過 `Instance` 找到它 ——
> 因為 prefab 無法在 Inspector 引用場景物件。

---

## 步驟 5 — 牌組

在 `Stage_Explore` 根物件上 `Add Component` → **`Exploration Deck`**：

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Starting Deck` | `List<CardDataExplore>` | 起始牌組。**第一次進探索時會灌進 `RunContext.exploreDeck`** |

然後在 `ExploreStageController` 補上三個新欄位：

| 欄位 | 型別 | 拖什麼 |
|---|---|---|
| `Exploration Deck` | `ExplorationDeck` | 剛加的元件 |
| `Cards Per Encounter` | int | `5`。**這個數字同時是可嘗試的次數上限**（C18⑤） |

> `ExploreStageController` **不再有** `Encounter UI` 與 `Hand UI` 欄位 ——
> 那兩者常駐在場景裡，prefab 無法引用，改為執行時透過 `Instance` 解析。

> **牌組的真相在 `RunContext.exploreDeck`**（跨房間保存），`ExplorationDeck` 只是每個 Stage 自帶的執行期物件。
> `SyncDeckFromRun()` 會在進場時同步 —— 所以前一個房間拿到的卡帶得到下一個房間。

---

## 步驟 6 — 對象大圖與預覽標籤（C17）

> **改版（2026-08-12）**：原本是把卡片拖到**世界裡的寶箱**上、機率標籤浮在寶箱頭上。
> 實測有三個問題：
> 1. 世界物件可能被對話框或其他東西蓋住，當拖曳目標不可靠
> 2. 要讓卡片打得到世界，就得讓 raycast 穿透對話框 —— 那又會變成「打牌時還能點到背景」
> 3. 機率標籤的位置受場景擺設影響，時常被遮
>
> 改成：**進入打牌環節後，互動主體整個搬進對話框**。壓黑層擋住世界，
> 卡片打在框內的大圖上，機率顯示在大圖頭上。玩家的注意力與可點範圍一致。

### 6.1 建立 `EncounterTargetView` prefab

`Assets/TYN/Core/` 建一個 prefab（例如 `EncounterTargetView.prefab`）：

```
EncounterTargetView          ← 掛 EncounterTargetView
├── Image                    ← 對象大圖
└── PreviewLabel (TMP)       ← 機率數字，放在大圖上方
```

| 物件 | 元件 | 設定 |
|---|---|---|
| 根 | `RectTransform` | 尺寸依立繪區大小 |
| | `Image` | **`Preserve Aspect` 必勾** —— 這是「直接用 character 會變形」的解法 |
| | `EncounterTargetView` | 見下 |
| `PreviewLabel` | `TextMeshProUGUI` | 放大圖上方，字級明顯 |

> 根物件的 `Image` 也可以直接當成大圖用（不必另外開子物件），
> 只要 `EncounterTargetView.Image` 指到它就好。

`EncounterTargetView` 欄位：

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Image` | `Image` | 大圖。程式會強制設 `preserveAspect = true`，即使忘了勾也不會變形 |
| `Preview Label` | `TextMeshProUGUI` | 機率數字 |
| `Normal Color` | Color | 一般狀態的數字顏色 |
| `Immune Color` | Color | 屬性無效時的顏色（建議灰） |
| `Immune Text` | string | `✕`。**不要用「0%」** —— 要讓玩家看得出是屬性不合而非運氣差 |

### 6.2 接到 `DialogueBoxUI`

| 欄位 | 拖什麼 |
|---|---|
| `Target View Prefab` | 剛建的 prefab |
| `Portrait Root` | `character`（大圖會生成在它底下） |

程式會在打牌開始時 `Instantiate` 一個，結束時 `Destroy`。**不會動到 `character` 原本的 Image**，所以人物立繪的設定不受影響。

### 6.3 對象的特寫圖

在 `RequiresCheck` 的寶箱 prefab 上設 **`Close Up Sprite`** —— 打牌時顯示在對話框裡的那張圖。

> `ChestInteractable.Preview Label` 現在**可以留空**了。機率改由對話框裡的
> `EncounterTargetView` 顯示，世界物件不再需要自己的標籤。

---

## 步驟 7 — 建立一個 `RequiresCheck` 寶箱

複製現有寶箱 prefab，改：

| 欄位 | 值 |
|---|---|
| `Open Mode` | **`RequiresCheck`** |
| `Check Prompt Text` | 「上了鎖。也許能用點手段撬開。」 |
| `Attribute` | 挑一個屬性，測相剋用 |
| `Preview Label` | 步驟 6 的 TMP |
| `Loot Items` | 隨便填幾樣 |

加進 `RoomContentData` 的 `Entries`（**記得把 `Weight` 設成 1**，Unity 的 `+` 會零填充）。

---

## 步驟 8 — 驗收

- [ ] 進房間，點 `RequiresCheck` 的寶箱 → 顯示提示 → **手牌出現在畫面下方**
- [ ] Console 出現 `[探索] 牌組同步完成：N 張` 與 `[打牌] 開始：手牌 N 張、選項 1 個、每次衰減 0.20`
- [ ] **對話框裡出現對象大圖**，比例正常不變形
- [ ] 打牌期間**點對話框只會推進文字，不會關掉框**
- [ ] 打牌期間**點不到背景的世界物件**（壓黑層擋住）
- [ ] 滑鼠移到手牌上 → 卡片上浮，**大圖上方顯示成功率數字**
- [ ] 換不同屬性的卡 hover → **數字跟著變**（相剋生效）
- [ ] 屬性完全不合的卡 → 顯示 **`✕`**（不是 `0`）
- [ ] 把卡拖到**大圖**上放開 → Console 印出判定過程：
      `[判定] xxx → 舊木箱｜基礎 60% × 相剋 Match × 衰減 1.00 = 60%｜擲出 0.42 → 成功`
- [ ] **判定成功後環節不會自動結束**（C18⑦）—— 手牌還在，可以繼續出
- [ ] 再 hover 一次 → **預覽數字已經下降**（衰減生效，C18④）
- [ ] 出到手牌用盡 → 環節自動結束、手牌區關閉
- [ ] 或按「結束」→ 同樣結束，剩餘手牌棄掉
- [ ] 拖曳距離不夠就放開 → 卡片彈回手牌，不算出牌

**兩段式出牌**

- [ ] 點一張卡 → **卡片浮起待命**，且目標持續顯示該卡的機率
- [ ] 再點同一張 → 取消選取，卡片放下
- [ ] 選了卡之後**點目標** → 出牌，效果與拖曳完全相同
- [ ] 選了卡之後點目標，**不會**又跳出「上了鎖」的提示（點擊被出牌吃掉了）
- [ ] 沒選卡時點 `RequiresCheck` 寶箱 → 正常跳提示並開始環節

---

## 常見錯誤

| 症狀 | 原因 | 解法 |
|---|---|---|
| 點寶箱沒有手牌出現 | `ExploreStageController` 的 `Exploration Deck` / `Encounter` 沒拖 | 步驟 4、5 |
| `[探索] 牌組抽不到任何卡` | `ExplorationDeck.Starting Deck` 是空的 | 步驟 5 |
| 卡片生成但疊在一起 | `Hand Root` 沒拖，或它有 Layout Group 元件 | `ExploreHandUI` 自己排版，**HandRoot 上不要掛 Layout Group** |
| hover 沒有預覽數字 | **場上沒有 `HoverPreviewBroadcaster`**（最常見）、`EncounterTargetView.Preview Label` 沒拖、或沒有 `ProbabilityCheck` | 三者都要在 `[SYSTEM]` 底下 |
| 對話框裡沒有大圖 | `DialogueBoxUI.Target View Prefab` 或 `Portrait Root` 沒拖 | 步驟 6.2 |
| 大圖被拉伸變形 | `Image` 沒勾 `Preserve Aspect` | 程式會強制設，若仍變形檢查父物件有沒有 Layout Group 在壓尺寸 |
| 打牌時點對話框就把框關掉了 | `HoldOpen` 沒生效 —— 通常是繞過 `BeginEncounter` 自己開的框 | 一律走 `ExploreStageController.BeginEncounter()` |
| 打牌時還能點到背景的世界物件 | `black_background` 的 `Raycast Target` 被關掉了 | 要保持**勾選** —— 它是打牌期間的輸入邊界 |
| 預覽數字永遠一樣 | `AttributeChart` 沒拖給 `ProbabilityCheck`，或卡與目標的 `Attribute` 都是 `None` | 目標是 `None` 時一律視為 `Match`（不吃相剋），這是刻意的 |
| 拖曳時卡片跟著滑鼠但放開沒反應 | 拖曳距離未達 `Play Threshold Pixels`，或目標身上沒有 `IProbabilityTarget` | 只有 `RequiresCheck` 的寶箱是判定目標 |
| 兩套拖曳打架、卡片行為怪異 | prefab 上還留著封存的 `ExplorationCardDragUI` 或 `CardDragUIExplore` | 步驟 2 |
| `[探索] 場上找不到 DialogueEncounterController` | 它沒掛在 EventScene，或誤放在 Stage prefab 裡 | 步驟 4。它必須常駐場景 |
| 點卡片有浮起，但點目標沒出牌 | 場上沒有 `ExploreHandUI`，或環節沒在進行中 | 步驟 3 |
| 「結束」按鈕在 Inspector 拖不到 `ExploreStageController` | 正常 —— **場景物件不能引用 prefab 內的東西** | 改接 `ExploreHandUI → RequestEnd ()`，步驟 3 |
| 打牌開始了但卡片沒出現 | 手牌區在 `Begin()` 之後才啟用，漏訂閱了第一次事件 | 已處理：`BeginEncounter` 先 `HandUI.Show()` 再 `Begin()` |
| 拖曳放開後卡片被選取起來 | 正常已處理 —— `OnPointerClick` 會濾掉 `eventData.dragging` 的情況 | — |
| 選了卡點目標，卻又跳「上了鎖」 | 點擊沒被出牌吃掉，通常是 `Hand UI` 沒拖或環節沒在進行中 | 步驟 5 |
| 判定成功後環節就結束了 | 有人自行加了「成功即結束」的邏輯 | **不該有** —— C18⑦，只有手牌用盡或按結束才會結束 |

---

## 下一批（4c 剩餘）

| 待做 | 說明 |
|---|---|
| 主要目標選定 UI | C18①：選定後 hover 不再廣播全選項，聚焦單一目標 |
| 「在試一次？」確認 | C12：失敗後跳出來問，而不是讓玩家自己決定要不要再拖一張 |
| `EncounterUIController` | 把上面兩項與結束按鈕收攏成一個控制器 |
| 角色對話框連動 | C18③：判定結果同時反映在**選項內文**與**角色對話框** |

目前判定結果只寫進 Console 與彈窗，選項內文的即時更新（C18③）還沒接 —— 那需要選項本身有文字元件，會在多選項對話（Phase 6）一起處理。

# Phase 4a / 4b / 4d — 探索 Stage 骨架 操作指引

> 對應 `SceneConsolidationPlan.md` §6 Phase 4 的 a / b / d 三段。程式已完成（編譯 0 error）。
>
> **4c（打牌環節 UI）尚未實作**，本文不涵蓋。目標是先讓「進房間 → 點東西 → 離開 → 地圖下拉」跑通。
>
> 版本：2026-08-08 · 預估 2–3 小時

---

## ⚠️ 最容易漏的一步：Physics 2D Raycaster

探索房間裡的寶箱、可調查物是**世界空間的 Sprite**，不是 UI。Unity 的 EventSystem **預設不會**把點擊事件送給它們 —— 必須在相機上掛 Raycaster。

**沒掛的話：所有東西都點不到，而且不會有任何錯誤訊息。**

1. 選 `[CAMERA] → Main Camera`
2. `Add Component` → **`Physics 2D Raycaster`**

| 欄位 | 值 |
|---|---|
| `Event Mask` | 預設 `Everything`，或只勾互動物件所在的 Layer |

> 用 `Physics 2D` 版本（不是 `Physics Raycaster`），因為 `InteractableBase` 要求 `Collider2D`。
> 若你的房間物件用的是 3D Collider，改掛 `Physics Raycaster` 並把 `InteractableBase` 的 `[RequireComponent]` 換成 `Collider`。

---

## 步驟 1 — 訊息系統：共用對話框

> **改版（2026-08-08）**：原本用從 ExploreScene 搬來的 `Popup_Panel` / `Loot_Panel`。
> 那兩個面板搬過來後 RectTransform 是死值（Scale 0、寬高 0），而且**兩套外觀的文字視窗在同一款遊戲裡很割裂**。
> 改成**所有訊息共用場景既有的對話框**，「獲得道具」只是系統提示的一種格式。
> `Popup_Panel` / `Loot_Panel` 可以直接刪掉。

### 1.1 在對話框上掛 `DialogueBoxUI`

場景根層已有 `dialogbox`（含 `text_box` → `name_box`、`option_box`）、`character`、`black_background`，直接沿用。

在 **`dialogbox`** 上 `Add Component` → **`Dialogue Box UI`**：

| 欄位 | 型別 | 拖什麼 |
|---|---|---|
| `Root` | `GameObject` | `dialogbox` 自己 |
| `Body Text` | `TextMeshProUGUI` | `text_box` 裡的正文 TMP |
| `Name Box` | `GameObject` | `name_box`（系統提示時自動隱藏） |
| `Name Text` | `TextMeshProUGUI` | `name_box` 裡的 TMP |
| `Portrait Root` | `GameObject` | `character`（可留空） |
| `Portrait Image` | `Image` | `character` 裡的 Image（可留空） |
| `Dimmer` | `GameObject` | `black_background`（可留空） |
| `Option Box` | `GameObject` | `option_box`。**顯示純文字訊息時會自動隱藏** —— 系統提示與一般對白都沒有選項，留著會擋畫面也會誤導玩家。Phase 4c 要用時由打牌 UI 呼叫 `SetOptionsVisible(true)` |
| `Advance Button` | `Button` | 蓋住對話框的透明 Button（見下） |

**打字機**

| 欄位 | 建議 | 說明 |
|---|---|---|
| `Chars Per Second` | `40` | 設 `0` = 不用打字機，直接全部顯示 |

**系統提示公版** —— 文案在 Inspector 調，不寫死在程式裡：

| 欄位 | 預設 | 說明 |
|---|---|---|
| `System Speaker Name` | 空 | 留空則系統提示時隱藏名字框；也可填「※」之類 |
| `System Text Color` | 米白 | 系統提示的文字顏色 |
| `Speech Text Color` | 白 | 角色說話的文字顏色 |
| `Item Gained Format` | `獲得了 {0}。` | 單一道具 |
| `Items Gained Header` | `獲得了：` | 多個道具的開頭 |
| `Item Line Format` | `　· {0}` | 多個道具的每一行 |
| `Empty Container Format` | `{0} 裡面空空如也。` | 空容器 |
| `Container Opened Format` | `打開了 {0}。` | 有東西的容器 |

### 1.2 推進按鈕

對話框需要「點一下繼續」。在 `dialogbox` 底下加一個蓋住整個框的 **Button**：

| 設定 | 值 |
|---|---|
| Image → Color | Alpha `0`（完全透明） |
| Image → Raycast Target | ✓ |
| RectTransform | stretch 全滿、offset 全 0 |
| 排序 | 放在其他子物件**下方**（Hierarchy 最後），才不會擋住文字的點擊 |

拖給 `DialogueBoxUI.Advance Button`。程式會自動接上 —— **不用手動設 OnClick**。

> 行為：文字還在跑 → 點擊跳完；跑完了 → 點擊關閉並播下一則排隊訊息。

### 1.3 `PopupService`

在 `[UI_ROOT] → Canvas_Popup`（或任何常駐物件）上 `Add Component` → **`Popup Service`**：

| 欄位 | 型別 | 拖什麼 |
|---|---|---|
| `Dialogue Box` | `DialogueBoxUI` | 剛設定好的 `dialogbox` |
| `Settlement Auto Advance` | float | **`0`** —— 結算類訊息（獲得道具等）等玩家點擊 |

`Dialogue Box` 沒拖的話會噴紅字，所有訊息改印到 Console。

### ⚠️ `Settlement Auto Advance` 設 0 以外的值會自動關掉對話框

它是「結算訊息顯示完後，隔幾秒自動推進」。而**最後一則訊息的「推進」就是關閉對話框** ——
所以設成 `1.6` 之類的值時，玩家會看到「舊木箱裡面空空如也。」打完字、停一下、然後框自己消失。

**2026-08-16**：場景裡原本是 `1.6`，已改回 `0`（團隊希望一律手動點擊）。

> 這個欄位是給**長文本**用的 —— 一大串內容要玩家連點很多下才煩人，那種情況再調成 1.5~2.5 秒。
> 一般結算訊息維持 `0`。
>
> 注意這與「按『結束』等於在對話框點一下」是**兩件不同的事**，後者仍然保留
> （見 `ExploreStageController.HandleEncounterEnded` 的 `AdvanceImmediate`）。

---

## 步驟 1.5 — UI 統一開關（`UIDirector`）

> **為什麼要有**：原本每個系統各自 `SetActive`，很容易出現「換了環節但上個環節的 UI 還開著」。
> 更麻煩的是**沒有任何一個地方說得出「現在應該有哪些 UI」**，愈到後期愈難查。
>
> 改成宣告式之後，關閉不再依賴「有人記得去呼叫」，而是**環節切換的必然結果**。

### 業界的三分法

| 分類 | 判準 | 管理方式 |
|---|---|---|
| **Panel** | 「屬於某個環節，換環節就該消失」 | 狀態機：宣告 `Visible In Stages`，自動開關 |
| **Dialog** | 「疊在當前畫面上，關掉要回到原本的地方」 | 堆疊 LIFO，換環節時全部收掉 |
| **Widget** | 「不是獨立畫面，是別人的一部分」 | `UIDirector` 完全不管 |

**判斷原則：每個 UI 只能有一個擁有者。** 已經有專屬控制器的（例如 `dialogbox` 由 `DialogueBoxUI` 管、`[MAP_OVERLAY]` 由 `MapView` 管）一律標 **Widget**，否則兩邊搶著開關會打架。

### 1.5.1 掛上 `UIDirector`

`[SYSTEM]` 底下 `Add Component` → **`UI Director`**：

| 欄位 | 值 |
|---|---|
| `Verbose Log` | 開發期建議 ✓，會印出每次環節切換開了幾個、關了幾個 |

### 1.5.2 在各 UI 上掛 `UIPanel`

| 物件 | Kind | Visible In Stages | 說明 |
|---|---|---|---|
| `dialogbox` | **Widget** | — | 由 `DialogueBoxUI` + `PopupService` 佇列控制 |
| `character` / `black_background` | **Widget** | — | 同上，是對話框的一部分 |
| `[MAP_OVERLAY]` | **Widget** | — | 由 `MapView` 的下拉／收起控制 |
| `Canvas_Popup` / `Canvas_Stage` / `Canvas_Tooltip` | **不用掛** | — | 只是容器，本身不開關 |
| `ExploreUI` | **Panel** | `Explore` | 探索環節專屬 |
| `ExitTag` | **Widget** | — | `ExploreUI` 的一部分，位置由 `BookmarkHover` 控制 |
| `ContinueAskPanel` | **Dialog** | — | 掛了之後 `ExploreStageController` 會自動改走 `UIDirector` 的堆疊 |

> `UIPanel` 上若同時有 `FadePanel`，開關會自動用淡入淡出。

### 1.5.3 現在的效果有限，但 4c 之後很關鍵

老實說**目前大部分 UI 都住在 Stage prefab 裡，換環節時本來就會被 Destroy**，所以 `UIDirector` 現在沒什麼事做。

它的價值在 Phase 4c 之後才會顯現 —— 手牌區、選項列、機率預覽標籤這些會是**常駐在場景裡**的面板，那時「哪個環節該顯示哪些」就必須有人統一管。現在花五分鐘設好，之後就不必回頭重構。

### 1.5.4 已經自動處理的事

不需要你手動接：

- **換 Stage 時清空訊息佇列** —— `GameFlowManager` 統一呼叫 `PopupService.CloseAll()`，不必每個 Stage 各寫一次
- **換 Stage 時關掉所有 Dialog** —— `UIDirector.ApplyStage()` 內建
- **系統提示時關掉選項框** —— `DialogueBoxUI.Open()` 每次都會重設，所以不管上一則訊息留下什麼狀態都是乾淨的

---

## 步驟 2 — 建立兩個資產

### 2.1 `RoomContentData` — 房間裡會出現什麼

`Assets/TYN/Explore/` 右鍵 → `Create → Eldritch → Room Content`，命名 `RoomContent_Village`。

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Entries` | `List<Entry>` | 可生成的內容，見下 |
| `Min Filled` | int | 本房間**至少**填幾個位子。未達此數會忽略 slot 的 `fillChance`，確保房間不會空到沒事做 |
| `Max Filled` | int | 最多填幾個。`0` = 不限 |

每一筆 `Entry`：

| 子欄位 | 型別 | 說明 |
|---|---|---|
| `Prefab` | `GameObject` | 互動物件 prefab（步驟 3 建） |
| `Weight` | float | 權重，越大越常出現 |
| `Placement` | `Any / Indoor / Outdoor` | 適合放哪種位子 |
| `Tag` | string | 對應 `SpawnSlot.contentTag`。留空 = 不限 |
| `Max Per Room` | int | 整間最多幾個。`0` = 不限 |

### 2.2 `RoomLibrary` — 節點對應哪個房間

`Create → Eldritch → Room Library`，命名 `RoomLibrary`。

| 子欄位 | 型別 | 說明 |
|---|---|---|
| `Content Id` | string | 對應 `RunNodeData.contentId`。**留空 = 此類型的通用房間** |
| `Node Kind` | `MapNodeKind` | `Event` / `Combat` / `Boss` / `Shop` / `SpecialEvent` |
| `Room Prefab` | `GameObject` | 房間 prefab（步驟 4 建） |
| `Weight` | float | 同類型有多個時的權重 |

> Phase 4 只需要先加一筆：`Node Kind = Event`、`Content Id` 留空、指向你的測試房間。
> 地圖生成目前不會寫 `contentId`，所以一律走「同類型通用房間」這條路。

---

## 步驟 3 — 建立互動物件 prefab

先做兩個最基本的。放在 `Assets/TYN/Explore/PREFAB/Interactables/`（自行新建）。

### 3.1 寶箱

新建 GameObject → 加以下元件：

| 元件 | 設定 |
|---|---|
| `Sprite Renderer` | Sprite 用 `Explore/INTERACTION/images/chest_closed.png` |
| `Box Collider 2D` | **必要**，點擊判定用。調整成蓋住圖 |
| `Chest Interactable` | 見下表 |

`ChestInteractable` 欄位：

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Display Name` | string | 例如「舊木箱」 |
| `Single Use` | bool | ✓ 開過就不能再開 |
| **`After Interact`** | enum | **`Auto`**（預設）／`KeepVisible`／`Disappear`。見下 |
| `Target Renderer` | `SpriteRenderer` | 留空會自動抓同物件上的 |
| `Interacted Sprite` | `Sprite` | `chest_open.png` |
| `Show Grab Cursor` | bool | ✓（C8 抓取手勢） |
| **`Open Mode`** | enum | **`Direct`** = 直接開<br>**`RequiresKey`** = 需鑰匙(C7)<br>**`RequiresCheck`** = 需判定(C18) |
| `Required Key Id` | string | `RequiresKey` 時填，例如 `key_warehouse` |
| `Locked Text` | string | 沒鑰匙時的提示 |
| `Consume Key` | bool | 開啟後是否消耗鑰匙 |
| `Loot Items` | `List<string>` | 顯示在 Loot 彈窗的道具名稱 |
| `Granted Item Ids` | `List<string>` | 實際加入背包的 id（例如開出另一把鑰匙） |
| `Attribute` | `ExploreAttribute` | 僅 `RequiresCheck` 用（C17） |
| `Preview Label` | `TextMeshPro` | hover 顯示成功率用。**4c 才會用到，先留空** |
| **`Visual Variants`** | `List<VisualVariant>` | **不同轉向／樣式的圖，生成時隨機挑一組**。見下 |

### 互動完之後物件怎麼處理（`After Interact`）

| 值 | 行為 |
|---|---|
| **`Auto`**（預設） | 有 `Interacted Sprite` → 換圖留著；**沒有 → 從畫面消失** |
| `KeepVisible` | 一律留著 |
| `Disappear` | 一律消失 |

`Auto` 對應直覺：**撿走的道具不該還躺在原地**，開過的箱子則應該留著（換成開啟的圖）。

所以：
- 可拾取道具（`InspectableInteractable` 的 `Pickup`）→ 不填 `Interacted Sprite`，撿完自動消失
- 沒有做開啟圖的寶箱 → 同樣自動消失
- 有做開啟圖的寶箱 → 填了就會留著換圖

### 隨機轉向的圖（C6）

若同一個寶箱備有多張不同轉向的素材，填 `Visual Variants`，每一筆兩個欄位：

| 子欄位 | 型別 | 說明 |
|---|---|---|
| `Normal` | `Sprite` | 未開啟的圖 |
| `Interacted` | `Sprite` | 開啟後的圖。留空則沿用上方固定的 `Interacted Sprite` |

生成時會用房間的 seed 隨機挑一組 —— 同一場 run 進同一個房間看到的是同一組，換節點或換一局才會變。

> ⚠️ **圖本身已有不同轉向時，把該 `SpawnSlot` 的 `Rotation Range` 設成 `0, 0`**，
> 否則會在已經轉過向的圖上再疊一次隨機旋轉，看起來會歪掉。
>
> 兩種隨機化擇一即可：**用圖換轉向**（適合像素或有明確透視的美術）或**用 Transform 旋轉**（適合俯視、對稱的物件）。

> 先做兩個變體：一個 `Direct`、一個 `RequiresKey`（配一個 `Direct` 寶箱開出鑰匙），就能測 C7。

### 3.2 可調查物件

| 元件 | 設定 |
|---|---|
| `Sprite Renderer` | 例如 `document.png` |
| `Box Collider 2D` | 必要 |
| `Inspectable Interactable` | 見下 |

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Kind` | `Information` / `Pickup` | 前者只顯示文字，後者會進背包 |
| `Content Text` | string | Information：描述；Pickup：道具名稱 |
| `Granted Item Id` | string | Pickup 時實際加入背包的 id |
| `Interacted Sprite` | `Sprite` | 例如 `documentopen.png` |

---

## 步驟 4 — 建立房間 prefab

**建議先做一個全新的，不要改舊的 `TEST_*`**（原因見文末）。

```
Room_Village_01                    ← 根物件，掛 RoomController
├── Background                     ← SpriteRenderer，房間底圖
├── Slot_01                        ← 空物件，掛 SpawnSlot
├── Slot_02
├── Slot_03
└── Slot_04
```

### 根物件的 `RoomController`

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Content Data` | `RoomContentData` | 步驟 2.1 的資產 |
| `Slots` | `List<SpawnSlot>` | **留空即可**，會自動抓子物件裡所有的 |
| `Entry Text` | string | 進房間時的敘述。留空則不顯示 |
| `Clear Text` | string | 全部互動完的總結。留空則不顯示 |

### 每個 `Slot_XX` 的 `SpawnSlot`

| 欄位 | 型別 | 說明 |
|---|---|---|
| `Placement` | `Any / Indoor / Outdoor` | 與 `Entry.placement` 比對（C6 的「室內室外皆有」） |
| `Content Tag` | string | 只接受特定 tag 的內容。留空 = 不限 |
| `Rotation Range` | Vector2 | **C6 的「各種角度」**，例如 `-12, 12` |
| `Scale Range` | Vector2 | `1, 1` = 不縮放 |
| `Fill Chance` | float 0–1 | 這個位子有東西的機率 |

> Scene 視圖裡 `SpawnSlot` 有 Gizmo（黃框 = 空、綠框 = 已填），擺位時看得到。

---

## 步驟 5 — 建立 `Stage_Explore.prefab`

放在 `Assets/TYN/Stages/`。

### ⚠️ 這個 Stage 同時有「世界空間」和「UI」兩種東西

- **房間**是 `SpriteRenderer`，活在世界座標
- **ExitTag / ContinueAskPanel** 是 UI，需要 Canvas

一個 prefab 只能掛在一個 parent 底下，所以不能整包丟給 `Canvas_Stage`（房間會看不見）也不能整包丟給 `WorldRoot`（UI 會沒有 Canvas）。

**解法：根物件放世界空間，UI 用一個「自己的 Canvas」包起來。**

```
Stage_Explore                      ← 普通 GameObject（Transform，不是 RectTransform）
│                                     掛 ExploreStageController
├── RoomRoot                       ← 普通 GameObject，房間 Sprite 生成在這
└── ExploreUI                      ← Canvas（Screen Space - Overlay）
    ├── ExitTag                    ← Image + BookmarkHover + TwoStageConfirm
    │   └── Text (TMP)             "離開"
    └── ContinueAskPanel           ← CanvasGroup + FadePanel
        ├── Text (TMP)
        ├── Btn_Yes
        └── Btn_No
```

`ExploreUI` 的 Canvas 設定：

| 元件 | 欄位 | 值 |
|---|---|---|
| Canvas | Render Mode | `Screen Space - Overlay` |
| | Sort Order | **`100`**（與 `Canvas_Stage` 同層） |
| Canvas Scaler | UI Scale Mode | `Scale With Screen Size` |
| | Reference Resolution | `1920 × 1080`（與其他 Canvas 一致） |
| Graphic Raycaster | — | 預設 |

> **這裡的 Canvas 是合法的，跟 Phase 2 那個坑不一樣。**
> Phase 2 的問題是 Canvas **巢狀在另一個 Canvas 裡面**，導致 RectTransform 從「被驅動」變成死值 0。
> 這裡 `ExploreUI` 的父物件是普通 Transform，它自己就是 root canvas，RectTransform 會被正常驅動。

### `StageHost` 註冊時要指定 parent

因為根物件是世界空間，註冊時 `Custom Parent` 要填 **`WorldRoot`**（見步驟 8）。

### `ExploreStageController` 欄位

| 欄位 | 型別 | 拖什麼 |
|---|---|---|
| `Room Root` | `Transform` | `RoomRoot` |
| `Room Library` | `RoomLibrary` | 步驟 2.2 的資產 |
| `Exit Tag` | `Button` | `ExitTag` 上的 Button（見步驟 6） |
| `Continue Ask Panel` | `GameObject` | `ContinueAskPanel` |
| `Encounter` | `DialogueEncounterController` | **4c 才會用，先留空** |

---

## 步驟 6 — ExitTag：離開入口（C14）

把 EventScene 裡現有的 `ExitTag`（已掛 `BookmarkHover`）**移進 `Stage_Explore.prefab`**。

> 為什麼放進 prefab 而不是常駐：Stage 結束時離開標籤本來就該跟著消失；商店之後會有自己的一份。
> 而且 prefab 不能引用場景物件，`ExploreStageController.exitTag` 需要指到 prefab 內部。

### 兩段式確認由「滑下 + 面板」構成，標籤本身只要單擊

```
hover ──▶ 標籤從上緣滑下（提示）──▶ 點一下 ──▶ 「要探索其他的東西嗎？」──▶ 選離開
          ↑ 第一段：BookmarkHover              ↑ 第二段：確認面板
```

企劃的 C14 要的是「先提示、再確認」，滑下就是提示、面板就是確認 —— **標籤上不需要再做一次點兩下**，否則會變成三段（hover → 點 → 再點 → 面板 → 選離開），太囉嗦。

`ExitTag` 需要的元件：

| 元件 | 職責 | 設定 |
|---|---|---|
| `Image` | 標籤圖像 | `Raycast Target` ✓ |
| `Bookmark Hover` | hover 時從上緣滑下（已存在，不用改） | `Hidden Y` 正數往上藏、`Shown Y` = 0、`Move Speed` = 10 |
| **`Button`** | 接收點擊 | `OnClick` **留空** —— `ExploreStageController` 會自動接上 |

> `Button` 與 `BookmarkHover` 掛在同一個物件上不會衝突，Unity 會把 pointer 事件送給所有 handler。
>
> `Button` 的 `Transition` 建議設 `None` —— 顏色變化會跟 `BookmarkHover` 的滑動搶視覺焦點。

### 離開流程是單一出口

> ⚠️ **2026-08-16 變更**：房間清空**不再自動跳面板**。離開一律由玩家主動點 ExitTag。
> 面板文字也因此從「要探索其他的東西嗎？」改成「**確定要離開嗎？**」，
> **兩顆按鈕的 YES/NO 語意跟著反過來** —— 見步驟 7。

```
玩家點 ExitTag（C14）──▶ 「確定要離開嗎？」─┬─ YES → 真正離開 → 地圖下拉
                                          └─ NO  → 留在房間

房間全部互動完（C13）──▶ 只播 clearText，不跳面板
```

「不小心點到離開有一次挽回機會」這個好處保留著 —— 確認面板還在，只是問法反過來。

要改回舊行為：`ExploreStageController` 勾選 **`Ask On Room Cleared`**，
並把面板文字與兩顆按鈕改回去。

---

## 步驟 7 — 「確定要離開嗎？」面板

`ContinueAskPanel` 底下兩顆按鈕的 `OnClick`：

| 按鈕 | Object | 方法 |
|---|---|---|
| `Btn_Yes`（**離開**） | `Stage_Explore` 根物件 | `ExploreStageController → OnChooseLeave ()` |
| `Btn_No`（**留下**） | `Stage_Explore` 根物件 | `ExploreStageController → OnContinueExploring ()` |

> ⚠️ **這張表在 2026-08-16 對調過。** 方法本身的語意沒變
> （`OnContinueExploring` ＝ 留下、`OnChooseLeave` ＝ 離開），
> 變的是問句 —— 「確定要離開嗎？」的 YES 當然是離開。
>
> **接反了不會有任何錯誤訊息**，只會變成玩家按「是」卻留在原地。
> 改完務必實際點一次。

面板初始狀態設為**停用**（程式也會在 `OnStageEnter` 關掉它，但先關比較不會在編輯時擋畫面）。

在 `ContinueAskPanel` 上加 **`UIPanel`**，`Kind` 設 **`Dialog`** —— `ExploreStageController` 會自動改走 `UIDirector` 的堆疊，換環節時就會被 `CloseAllDialogs()` 一併收掉。

### 讓它和 MapBanner 外觀一致

`MapBanner` 是靠 `CanvasGroup` 淡入淡出的，直接 `SetActive` 會硬切，兩者放在一起會很突兀。

在 `ContinueAskPanel` 上加：

| 元件 | 欄位 | 值 |
|---|---|---|
| `Canvas Group` | — | 必要（`FadePanel` 有 `[RequireComponent]`，會自動補） |
| **`Fade Panel`** | `Fade In Duration` | `0.2`（與 `MapBannerUI` 的 `fadeInDuration` 對齊） |
| | `Fade Out Duration` | `0.2` |
| | `Hidden On Awake` | ✓ |

`ExploreStageController` 會自動偵測：**面板上有 `FadePanel` 就用淡入，沒有就退回 `SetActive`** —— 不必改任何程式或設定。

> 視覺本體直接複製 `MapBanner` 的子物件（外框、底色）過來即可，只要換掉文字並加兩顆按鈕。
> `MapBannerUI` 腳本**不要**一起複製，那是橫幅專用的邏輯。

---

## 步驟 8 — 註冊到 StageHost

`EventScene` → `[STAGE_HOST]` → `StageHost.Stages` 新增一筆：

| 子欄位 | 值 |
|---|---|
| `Type` | **`Explore`** |
| `Prefab` | `Stage_Explore.prefab` |
| `Custom Parent` | **`[STAGE_HOST] → WorldRoot`** |

> 必須指定 `WorldRoot`，因為 `Stage_Explore` 的根物件是世界空間（房間是 SpriteRenderer）。
> 它內部的 `ExploreUI` 自己帶 Canvas，所以 UI 照樣正常顯示 —— 見步驟 5。
>
> 對照：`Stage_Menu` 是純 UI，`Custom Parent` 留空即可（自動掛到 `Ui Root`）。

---

## 步驟 9 — 驗收

按 Play：

- [ ] 主選單 → START → 地圖下拉、節點逐層淡入
- [ ] 點一個節點 → 棋子走過去 → 地圖收起 → **進入房間**
- [ ] Console 出現 `[房間] Room_xxx 填入 N / M 個位子`
- [ ] 房間裡的物件**位置與角度每次不同**（回主選單重開一局比較，C6）
- [ ] 滑鼠移到寶箱上 → **游標變成張開的手**（C8）
- [ ] 點寶箱 → **對話框**顯示「打開了 X。」並列出道具、圖換成打開的箱子
- [ ] 訊息有**打字機效果**，點一下跳完，再點一下關閉
- [ ] 訊息顯示時**選項框是關閉的**
- [ ] 點可調查物（Pickup 類型、沒填 Interacted Sprite）→ 顯示訊息後**物件從畫面消失**
- [ ] **鑰匙測試（C7）**：先點沒鑰匙的箱子 → 出現「鎖住了」；開出鑰匙後再點 → 開得了
- [ ] 全部點完 → 只播 `clearText`，**不會**跳出面板（2026-08-16 起）
- [ ] 點 ExitTag → 跳出「確定要離開嗎？」→ **YES 真的離開、NO 留在房間**
- [ ] 選「繼續探索」→ 面板關閉，留在房間
- [ ] 滑鼠移到 `ExitTag` → **標籤從上緣滑下**
- [ ] 點一下 → **跳出「要探索其他的東西嗎？」**（不是直接離開，也不用點兩下）
- [ ] 選「離開」→ 畫面淡黑 → **地圖自動下拉**（C1/C2）
- [ ] 回到地圖後，剛才那個節點變成已走過，棋子在上面
- [ ] 若 `UIDirector` 的 `Verbose Log` 開著，每次環節切換會印出開關了幾個面板

---

## 常見錯誤

| 症狀 | 原因 | 解法 |
|---|---|---|
| **房間物件完全點不到，也沒有錯誤訊息** | `Main Camera` 沒掛 `Physics 2D Raycaster` | 見文首 |
| 點得到但沒反應 | 物件沒有 `Collider2D`，或 Collider 沒蓋住圖 | `InteractableBase` 有 `[RequireComponent]`，但 Collider 大小要自己調 |
| `NullReferenceException` on `UIManager` | 房間 prefab 還掛著封存的 `InspectableObject` / `ContainerObject` | 見下方「舊房間 prefab」 |
| `[訊息] PopupService 沒有指定 Dialogue Box` | `PopupService.Dialogue Box` 沒拖 | 步驟 1.3 |
| 對話框出現但點不掉 | 沒有 `Advance Button`，或它被其他子物件擋住 | 步驟 1.2。透明 Button 要放在 Hierarchy 最後 |
| **UI 有 SetActive 但畫面上看不見** | RectTransform 是死值：`Scale 0` 或寬高 0。從別的場景複製 UI 過來最常見 | Scale 改回 `1,1,1`，重設 Anchor Preset 與 Width/Height。`PopupService` 已加自動偵測會噴紅字 |
| `[房間] 沒有內容表或沒有 SpawnSlot` | `RoomController.Content Data` 沒指定，或房間裡沒有 `SpawnSlot` | 步驟 4 |
| **`[房間庫] … Weight 是 0 而被跳過`** | ⚠️ **最常踩**：Unity 用 Inspector 的 `+` 新增 List 元素時會**零填充**，不套用程式裡的 `weight = 1f` 預設值 | 手動把 `Weight` 改成 `1`。`RoomContentData` 的 `Entries` 也一樣 |
| `[房間庫] 沒有任何 Node Kind = Event 的條目` | `RoomLibrary` 沒加對應類型 | 步驟 2.2 |
| `[房間庫] … Room Prefab 沒指定` | 條目建了但沒拖 prefab | 步驟 2.2 |
| 房間出現但 UI（ExitTag / 詢問面板）看不見 | `ExploreUI` 沒有自己的 Canvas，或 Sort Order 太低 | 步驟 5 |
| UI 正常但房間看不見 | `StageHost` 的 `Custom Parent` 沒指到 `WorldRoot` | 步驟 8 |
| 房間看不見 | 房間是世界空間物件卻掛在 Overlay Canvas 底下 | 步驟 8 的註記 |
| 每次進同一節點擺設都不一樣 | 正常 —— 種子是 `runSeed ^ nodeId`，**同一場 run 內**同一節點才會一致 |  |
| ExitTag 點一下就直接離開，沒跳確認面板 | `ExploreStageController.Continue Ask Panel` 沒拖 | 步驟 5 |
| ExitTag 點了沒反應 | `ExitTag` 上沒有 `Button`，或 Image 的 `Raycast Target` 沒勾 | 步驟 6 |
| 換環節後上一個環節的 UI 還開著 | 該面板的 `UIPanel.Kind` 設成 `Widget`，或 `Visible In Stages` 沒填 | 步驟 1.5.2 |
| 面板被兩邊搶著開關、閃爍 | 同一個 UI 有兩個擁有者 | 已有專屬控制器的一律標 `Widget` |
| 撿完道具物件還留在原地 | `After Interact` 設成 `KeepVisible`，或填了 `Interacted Sprite` | 步驟 3.1 |
| 系統提示時選項框還開著 | `DialogueBoxUI.Option Box` 沒拖 | 步驟 1.1 |

---

## 舊房間 prefab（`TEST_*`）怎麼辦

`Room.prefab` / `TEST_HOUSE` / `TEST_PATH` / `TEST_ROOM` / `TEST_seawalk` 五個都掛著**已封存**的 `InspectableObject`、`ContainerObject`、`Door`、`RoomController`。

它們仍會編譯（`_Archive/` 在 `Assets/` 底下），但執行時會 `NullReference` —— 因為那些腳本呼叫的 `UIManager.Instance` 已經不在場景裡了。

**建議先不要動它們**，用步驟 4 全新做一個房間把流程跑通。等新的管線確認可用，再決定：

| 選項 | 適用 |
|---|---|
| 逐個轉換：換掉 4 個腳本、加 SpawnSlot | 房間的美術擺位有價值、想留 |
| 重做：只保留背景圖，重新擺 slot | 舊擺位本來就是測試用的 |

轉換對照：

| 舊 | 新 |
|---|---|
| `RoomController`（全域） | `EldritchMile.Explore.RoomController` |
| `InspectableObject` | `InspectableInteractable` |
| `ContainerObject` | `ChestInteractable`（`Open Mode = Direct`） |
| `Door` | 刪除，改用 `Stage_Explore` 裡的 `ExitTag` |

---

## 完成後

接著是 **Phase 4c — 打牌環節 UI**：卡牌拖曳重寫、hover 全選項預覽廣播（草圖的 `A 50 / B 50 / C 50`）、主要目標選定、「在試一次？」確認。

`DialogueEncounterController`（回合、衰減、結束鈕的邏輯）在 Phase 1 就寫好了，4c 只補畫面層。

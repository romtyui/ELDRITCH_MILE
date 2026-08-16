# ELDRITCH_MILE — 系統盤點（做完的／待做的／劇情需要什麼）

> 最後更新：2026-08-15 · 涵蓋 `Assets/TYN/`（探索、地圖、流程、對話、道具）
>
> **這份文件在補一個缺口**：工程文件（`RoadmapNext.md`、各 `Phase*.md`）只談程式，
> 劇情文件（`克蘇魯劇情大綱.docx`）只談內容，中間沒有橋。
> 結果是「劇情裡寫了的東西需要哪些系統」這件事沒有任何地方記錄。本文負責這一段。
>
> 讀這份文件之前建議先看 [HANDOFF.md](HANDOFF.md)（慣例與踩坑）。
> 「接下來做什麼」的細節仍以 [RoadmapNext.md](RoadmapNext.md) 為準，本文只給全景。

---

## 0. 一頁總覽

| 系統 | 狀態 | 歸屬 | 劇情裡誰依賴它 |
|---|---|---|---|
| 流程總管（Stage／地圖覆蓋層） | ✅ 可用 | TYN | 全部 |
| 地圖生成與節點 | ✅ 可用 | TYN | 「關聯場景：港口、漁村」 |
| 主選單 | ✅ 可用 | TYN | — |
| 地圖動效（節點 tooltip／棋子滑行） | 🔶 程式完成，**tooltip 面板待建** | TYN | — |
| 探索房間（隨機填充、互動物件） | ✅ 可用 | TYN | 全部 |
| **打牌判定（C17/C18/C19）** | ✅ **已驗收** | TYN | 探索的核心迴圈 |
| 屬性與相剋 | ✅ 已定案 | TYN | 尚未接上世界觀 |
| 道具資料層 | 🔶 資料層完成，**無 UI** | TYN | 漁夫的漁獲、商店 |
| 道具數量 | ✅ `ItemStack`（可容納 per-instance 狀態） | TYN | 漁獲、商店 |
| 對話系統 | 🔶 **已實作，待驗收**（選項＝判定目標，可用機率卡打） | TYN | **五名角色全部** |
| 商店 | 🔶 **已實作，待驗收**（挑一件；**無貨幣**） | TYN | — |
| 事件效果（陷阱／逆轉／複製卡牌） | ❌ 未做 | TYN | **畫家/卡羅麗** |
| 神牌 | 🔶 事件端已實作（挑一張牌）；真正的神牌仍待戰鬥端 | TYN + Romtyui | **小說家/克拉夫特** |
| HP／SAN 的探索端讀寫 | ❌ **卡跨隊** | Romtyui 持有 | **漁夫、貴婦** |
| 世界污染進度 | ❌ **不存在，歸屬未定** | ？ | **五名角色的倒戈條件** |
| 戰鬥 | 由 Romtyui 負責 | Romtyui | 全部 |
| 戰鬥接入探索流程 | ❌ **卡跨隊** | TYN | — |

---

## 1. 已完成（可用、且大多已在 Play Mode 驗收）

### 1.1 流程骨架

單一 `EventScene`，Build Settings 只有一個場景。

| 元件 | 職責 |
|---|---|
| `GameFlowManager` | 唯一的流程總管。Stage 切換與地圖開合都必須經過它。也持有 `RunContext` 與 `MetaProgressData` |
| `StageHost` / `StageController` | Stage 的生成與生命週期（`OnStageEnter` → `OnStageReady` → `OnStageExit`） |
| `UIDirector` / `UIPanel` | UI 開關總管。`Panel` 走狀態機、`Dialog` 走堆疊、`Widget` 不管 |
| `MapOverlayController` / `MapView` | 地圖覆蓋層（不是獨立場景，是覆蓋層） |
| `ScreenFader` / `FadePanel` / `PanelToggle` | 轉場與面板顯示 |
| `PopupService` / `DialogueBoxUI` | 全專案共用的訊息與對話框（含打字機、排隊、公版格式） |

> 文件：[Phase1_EditorSetup.md](Phase1_EditorSetup.md)、[Phase2_MenuStage.md](Phase2_MenuStage.md)、[Phase3_MapOverlay.md](Phase3_MapOverlay.md)

### 1.2 探索房間

`RoomLibrary` → `RoomContentData` → `SpawnSlot` 隨機填充，用 run seed + nodeId 保證可重現。

互動物件收斂成 `InteractableBase`：
- `ChestInteractable`（`Direct` / `RequiresKey` / `RequiresCheck` 三態）
- `InspectableInteractable`

C13 的迴圈：房間清空**不會**自動離開，而是問「要探索其他的東西嗎？」。

> 文件：[Phase4a_ExploreStage.md](Phase4a_ExploreStage.md)

### 1.3 打牌判定 — 本階段的主要成果

四批做完，全部在 Play Mode 驗收過：

| 批次 | 內容 | 文件 |
|---|---|---|
| 一 | 拖曳／兩段式出牌、hover 全選項預覽、即時判定、逐次衰減、對話框內對象大圖 | [Phase4c_CardPlay.md](Phase4c_CardPlay.md) |
| 二 | C12 回合感（次數提示，**取代**了原本的確認視窗） | [Phase4c2_RetryAsk.md](Phase4c2_RetryAsk.md) |
| 三 | 徹底失敗結案、房間清空修復、付代價重試（道具） | [Phase4c3_RetryCost.md](Phase4c3_RetryCost.md) |
| 四 | Q7a 屬性定案、相剋表、手牌變暗 | [Phase4c4_Attributes.md](Phase4c4_Attributes.md) |

**規則全部在 `DialogueEncounterController`（Core）**，UI 層只畫畫面與轉交動作，不做任何判斷。

屬性（Q7a 已定案）：

| ID | 顯示名 | 顏色 | 值 |
|---|---|---|---|
| `None` | 無 | 黑白 | 0 |
| `Intuition` | 直覺 | 紅 | 1 |
| `Logic` | 邏輯 | 藍 | 2 |
| `Insight` | 批判與創造 | 綠 | 3 |

相剋：**直覺 ↔ 邏輯互斥（0×）**，批判與創造居中（0.5×），同屬性相符（1.0×）。
這不是元素環 —— 目標的屬性代表「這個難題吃哪一種腦袋」。

### 1.4 道具資料層（剛完成）

| 元件 | 職責 |
|---|---|
| `ItemData`（SO） | `id` / `displayName` / `icon` / `description` |
| `ItemDatabase`（SO） | id → ItemData 查表 |
| `GameFlowManager.ItemName(id)` | 靜態便利方法 |

**`RunContext.inventory` 是 `List<ItemStack>`** —— id 是真相，`ItemData` 只負責「長什麼樣」。
數量與日後的 per-instance 狀態都掛在 `ItemStack` 上，見 §4。

---

### 1.5 地圖 UI 動效（2026-08-16）

| 項目 | 狀態 |
|---|---|
| 棋子移動：彈跳 → 磨砂石桌上的滑行 | ✅ 有預設值，**不用設定就生效** |
| 節點 hover：縮放 → tooltip | 🔶 程式完成，**面板要在編輯器裡建**（10 分鐘） |
| 游標變化 | ❌ 等美術素材。機制現成，插入點已確認 |

⚠️ **tooltip 面板建起來之前，地圖 hover 完全沒有回饋。**
趕時間可把 `MapNodeUI.Scale Hover` 設 `1.1` 暫時頂著（預設 `1` ＝ 關閉）。

操作與調參見 [MapPolish.md](MapPolish.md)。

---

## 2. 待做，依「被什麼擋住」分類

### 2.1 🔴 卡跨隊（需要 Romtyui 配合 — 請一次談完）

| # | 需求 | 為了什麼 | 出處 |
|---|---|---|---|
| A | `BattleManager.OnBattleEnded(BattleOutcome)` 事件 | Phase 5 戰鬥接入探索流程 | 設計文件 §7.2 |
| B | **run 開始時就初始化 HP／SAN**（不要等第一場戰鬥打完） | 探索端要能扣 HP／SAN | [RoadmapNext](RoadmapNext.md) §1.2b |
| C | **世界污染進度歸誰管** | 五名角色的倒戈條件（見 §3） | 劇情大綱 |

> **B 的細節**：`RunStateManager.savedPlayerCurrentHp` 只有在 `SaveFromBattle()` 之後才有值。
> 玩家若在任何戰鬥之前先進探索房間，HP 是 `0`。
>
> 順帶可提：`RunStateManager.cs` 的中文註解疑似不是存成 UTF-8（讀出來是亂碼）。

### 2.2 🟡 設計未定（等企劃決定，不是工程問題）

| 項目 | 卡在哪 |
|---|---|
| 一般物件 vs 特殊物件的分類機制 | 「特殊效果」目前不存在於資料模型。物件數量還太少，硬做分類會變空想 |
| 大類型的固有倍率清單 | 暫用 Tier1~4 + TacticalDeath（×1/2/3/5/20），等物件多了重整 |
| 屬性接上世界觀 | 直覺／邏輯／批判與創造目前是**純機制概念**，劇情大綱裡完全沒提到 |
| 綠屬性的平衡 | 綠永遠沒有 0，期望值 0.67 > 紅藍 0.50。建議用資料補平（降基礎機率或降牌組比例） |

### 2.3 🟢 純工程待做（沒有阻塞，隨時可以開）

| 項目 | 份量 | 備註 |
|---|---|---|
| **地圖 tooltip 面板** | 小 | 10 分鐘。做完 hover 才有回饋，見 [MapPolish.md](MapPolish.md) |
| 背包 UI | 中 | 玩家目前看不到自己身上有什麼 |
| Phase 4d 收尾驗證 | 小 | 鑰匙寶箱、兩段式離開（C14）。⚠️ `chest_RequiresKey` 權重目前是 0（測試腳手架） |
| Phase 5 我方準備 | 中 | 從 Romtyui 的 Scene 打包 prefab、包 `BattleStageController`、檢查 Camera/EventSystem/AudioListener 重複 |
| Phase 6 對話系統 | 大 | 見 §3，**劇情最依賴這個** |
| Phase 6 商店（C15） | 中 | 需要道具數量與價格 |
| C18① 主要目標選定 | 小 | 併入 Phase 6。手牌變暗已經給了它明確用途 |
| C18③ 選項內文即時更新 | 小 | 需要選項有文字元件，Phase 6 一起 |
| Phase 7 神牌 | 小 | 見 §3 |
| repo 加 `.editorconfig` | 極小 | [RoadmapNext](RoadmapNext.md) §1.7，已評估後刻意延後 |

---

## 3. 劇情大綱 → 系統需求對照

> 依據 `克蘇魯劇情大綱.docx`（角色設定集，五名角色）。
> **這一節是本文件的重點** —— 它是唯一記錄「劇情裡寫的東西需要哪些系統」的地方。

### 3.1 角色的三重身分

劇情設定：角色是「千面的奇術師」收集情報時混入的**情報碎片**，被設置為**對應侵略者的錨點**，
散落世界各處。他們**可以接觸與對話，但無法干涉玩家的行為，也不能直接參戰**；
不過被賦予了**作為卡牌在戰鬥中提供幫助**的能力。

這代表每個角色同時是三種東西，而且**分屬不同的系統與不同的負責方**：

| 身分 | 需要什麼 | 歸屬 | 現況 |
|---|---|---|---|
| **可對話的 NPC** | 對話系統（`DialogueData` + `DialogueStageController` + 多選項） | **TYN** | ❌ 未做（Phase 6） |
| **戰鬥中的卡牌** | 卡牌效果系統 | Romtyui | 由對方負責 |
| **可能倒戈的敵人** | 敵人資料 + **觸發條件** | Romtyui + **觸發條件見 §3.3** | ❌ |

> 對話框已經支援立繪（`DialogueBoxUI.portraitRoot` / `EncounterTargetView`），
> 所以角色的「人類／非人」兩種外貌在技術上已經有地方擺。

### 3.2 各角色的能力 → 對應的系統缺口

| 角色 | 劇情寫的能力 | 需要的系統 | 歸屬 | 現況 |
|---|---|---|---|---|
| **魔術師/坎貝爾**<br>（奈亞拉托提普） | 抽取兩張手牌 | 卡牌效果 | Romtyui | — |
| **小說家/克拉夫特** | 杜撰故事 — 抽取 1 張**神牌** | **神牌系統**（Phase 7） | TYN 給牌 + Romtyui 定效果 | ❌ |
| **畫家/卡羅麗**<br>（無定形的色彩） | **觸發事件**可複製一張現有的卡牌 | **事件效果系統** | **TYN** | ❌ **完全不存在** |
| **漁夫/時藏**<br>（克蘇魯／深潛者） | 隨機獲取漁獲<br>（使用後 **+HP −SAN**，**只能在戰鬥外使用**） | **背包 + 可使用道具 + HP／SAN 橋** | **TYN** + 跨隊 B | 🔶 資料層剛做，UI 與使用動作未做 |
| **貴婦/伊麗沙白** | 吸血、**消耗 HP 獲得收益** | HP 橋 | 跨隊 B | ❌ |

#### 三個從劇情反推出來、工程文件裡原本沒有的需求

**① 漁夫的漁獲＝可主動使用的消耗品。**
「只能在戰鬥外使用」明確要求探索階段能：看到道具 → 點它 → 使用 → 改變 HP／SAN。
這把背包從「Phase 6 商店才需要」提前成「設計上已經存在」。
它同時要**跨隊需求 B**（HP／SAN 初始化）才能真的動。

**② 畫家的能力是「觸發事件」，那是 TYN 的範圍。**
「觸發事件可複製一張現有的卡牌」不是戰鬥中的卡牌效果，是**探索／事件階段**的效果。
但 TYN 目前沒有任何「事件效果」的表達方式 —— `ChestInteractable` 只有
`openMode` / `attribute` / `lootItems` / `grantedItemIds`。
這與 §2.2 的「一般物件 vs 特殊物件」是同一個缺口，**陷阱與逆轉也會落在這裡**。

**③ HP 與 SAN 是互相交換的關係。**
漁獲是 `+HP −SAN`、貴婦是「消耗 HP 獲得收益」。所以這兩個數值不是各自獨立的血條，
而是一組可以互相轉換的資源 —— 這會直接影響重試代價要扣哪一個、扣多少。

### 3.3 世界污染進度 — 一個不存在的變數

> 劇情大綱：「依照世界被污染的進度。他們有概率會提前遭到侵略者污染，
> 成為強力的眷屬與主角敵對。」

這是決定**盟友何時倒戈**的全域進度值。目前：

- `RunContext` 沒有（只有地圖、探索牌組、背包）
- `RunStateManager`（Romtyui）沒有（只有 HP／能量／戰鬥牌組）

**需要企劃決定兩件事**：

1. 它是不是就是 **SAN**？（理智下降＝世界看起來更污染，在克蘇魯題材裡說得通）
   還是獨立的第三個值？
2. 若獨立，**歸誰管**？影響戰鬥（敵人組成）也影響探索（誰還能對話），兩邊都要讀

> 這是跨隊需求 **C**，建議跟 A、B 一起提。

### 3.4 場景需求

漁夫的設定寫了「**關聯場景：港口、漁村**」。目前 `RoomContentData` 只有
`RoomContent_Village` 一份，`RoomLibrary` 也只對應到兩個 prefab。

角色若要綁定特定場景出現，需要：節點類型 → 場景類型 → 內容表的對應鏈。
骨架（`RoomLibrary.Pick(node, rng)`）已經在了，缺的是內容。

---

## 4. 道具數量：`ItemStack`（2026-08-15 定案並實作）

```csharp
[Serializable]
public class ItemStack { public string id; public int count = 1; }

// RunContext
public List<ItemStack> inventory;
public int  CountOf(string id);                       // 跨疊加總
public void AddItem(string id, int count = 1);        // 同 id 併疊
public bool ConsumeItem(string id, int count = 1);    // 全有或全無
```

### 為什麼是這個結構（查證過業界做法後的結論）

業界的分界線只有一條：**這個道具有沒有「每一份各自不同」的狀態。**

- **可堆疊**（三根撬棍完全一樣）→ 用 `count`
- **不可堆疊**（耐久度、詞綴、隨機屬性）→ 每份都要是獨立實例

通用寫法是 `IsStackable()` + `Count()`，不可堆疊的 count 恆為 1。
另外，「重複清單 + 顯示時才 group」也是被認可的做法（在 GUI 層堆疊而非 model 層），
**不是偷懶** —— 多數 Unity 教學展示 `{item, quantity}` 是因為它們在做格子背包，
槽位本身就是資料模型，那個理由不適用於本專案。

### 決定性的因素：漁獲

劇情大綱裡漁夫的能力是「**隨機**獲取漁獲」。
若每條魚的 `+HP／−SAN` 數值不同，漁獲就不是可互換的東西 ——
`List<string>` 與單純的 count 都裝不下它。

遊戲是肉鴿，這類「同名但數值不同」的東西只會變多。
**`ItemStack` 能表達「三根一樣的撬棍」，反過來不行**，所以選它。

換的時機也對：`RunContext` **目前完全沒有持久化**（只有 `MetaProgressData` 用 PlayerPrefs），
所以沒有舊存檔要顧；等商店與漁獲都接上去之後呼叫點會多很多。

### 幾條實作上的約束

**① `ConsumeItem` 必須全有或全無。** 需要 3 個但只有 2 個時，不可以扣掉那 2 個再回報失敗
—— 那是這類 API 最典型的 bug，而且因為資源真的少了，重試也修不回來。
程式是先 `CountOf` 確認付得起才動手扣。

**② 允許同一個 id 出現在多疊裡。** 這是刻意的，per-instance 狀態就靠它表達。
所以 `CountOf` 與 `ConsumeItem` 都跨疊處理，不可以假設「一個 id 只有一疊」。

**③ 加 per-instance 狀態時，`AddItem` 的合併規則要跟著改。**
目前是「同 id 就併」（道具還都可互換，所以正確）；加了狀態之後必須是
「同 id **且狀態相同**才併」，否則兩條不同的魚會被併成一疊。
這條寫在 `ItemStack` 的類別註解裡。

**④ 空掉的疊要移除**，否則 `inventory` 會慢慢長滿 `count == 0` 的殘骸。

### `maxStack` 還沒做

今天多拿一把倉庫鑰匙只是浪費，不會壞任何東西。要加的時候：
欄位放 `ItemData.maxStack`，但**強制邏輯不要寫進 `RunContext`** ——
它是純資料、不該認識 `ItemDatabase`。包一層 `GameFlowManager.GrantItem(id, count)`，那裡才有資料庫。

### 已接上的顯示

重試詢問現在可以顯示剩餘數量：`{0}` 道具名、`{1}` 第幾次重試、`{2}` 還剩幾個。

> ⚠️ **既有 prefab 的文案不會自動更新** —— 改 C# 的預設值不會動到已序列化的字串。
> `chest_RequiresCheck` 的 `Retry Prompt Format` 目前是客製過的兩個佔位符版本，
> 要顯示剩餘數量的話自己把 `{2}` 加進去。少傳參數才會爆，多傳會被忽略，所以不加也不會壞。

## 5. 技術債與測試腳手架

### 5.1 測試腳手架 — 正式配內容前要還原

⚠️ **這些看起來像 bug，其實是刻意的。不要順手「修好」。**

| 項目 | 現況 | 還原成 |
|---|---|---|
| `RoomContent_Village` 的 `chest_RequiresKey` / `chest_Document` | `weight: 0`（讓測試寶箱必定出現） | 補回 1，否則這兩者永遠不生成 |
| `MapGenerationSettings.Use Demo Route` | **✓**（5 個全 Event 的直線路線） | 戰鬥接上後取消勾選。見 [DemoRoute.md](DemoRoute.md) |
| `MapNodeUI.Scale Hover`（若為了 demo 開了） | `1.1` | tooltip 上線後調回 `1` |

### 5.2 技術債

| 項目 | 影響 |
|---|---|
| **背包 UI 不存在** | 玩家看不到自己身上有什麼 |
| `lootItems`（顯示文字）與 `grantedItemIds`（id）是兩份要手動同步的清單 | 有了 `ItemData` 之後可以合併，但要重配現有寶箱資料 |
| `MetaProgressData.AddLegacyItem` 會去重，`RunContext.AddItem` 不會 | 語意上說得通（遺產＝解鎖、背包＝物資），但行為不一致 |
| 兩處玩家可見的中文寫死在程式裡 | `InspectableInteractable.cs:46`、`RoomController.cs:148`。在地化時的唯一障礙 |
| repo 沒有 `.editorconfig` | TYN 是 UTF-8、Romtyui 有檔案是 cp950，兩邊都沒 BOM |
| `_Archive/` 仍在編譯範圍 | 舊型別佔用全域命名空間（所以 `using` 要寫在 `namespace` 內） |
| `ExplorationDeck.OnCardPlayed` 沒有被呼叫 | 出過與沒出的牌都走 `DiscardHand`，所以 `CardDataExplore.exhaust` 目前無效 |

---

## 6. 建議順序

1. **commit**（目前這批已驗收）
2. **跟 Romtyui 談一次，A／B／C 三件一起提**（§2.1）—— 不擋任何事，但越早越好
3. **Phase 4d 收尾驗證**（小）—— 記得先把 `chest_RequiresKey` 的權重補回 1
4. **Phase 6 對話系統**（大）—— **劇情最依賴這個**，五名角色全部卡在這裡。
   C18①、C18③、商店都會併進來
5. Phase 5 戰鬥接入（等 A）、Phase 7 神牌、事件效果系統（等 §2.2 的分類決定）

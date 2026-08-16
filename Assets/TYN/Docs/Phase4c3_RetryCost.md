# Phase 4c（第三批）— 徹底失敗結案 + 付代價重試 操作指引

> 對應 `RoadmapNext.md` §1.2a／§1.2b。程式已完成（編譯 0 error 0 warning）。
>
> 版本：2026-08-15 · 預估 30–40 分鐘 · 前置：Phase 4c 第一、二批已跑通

---

## 這批解了什麼

**回報的症狀**：手牌 5 張全部失敗後，寶箱仍可再次互動、再抽一手牌。

**根因**：`MarkDone()` 只在成功時呼叫，判定失敗**沒有任何結案路徑**。連帶兩個更嚴重的後果：

| 後果 | 說明 |
|---|---|
| **房間永遠清不掉** | `ReportInteracted` 只有 `MarkDone()` 會呼叫 → 計數到不了總數 → **C13「要探索其他的東西嗎？」永遠不會自動跳** |
| **保證 0% 的假迴圈** | 重進去時衰減已歸零，每張牌必定失敗；棄牌堆又會循環回抽牌堆，所以永遠跑不完 |

**改法**：手牌用盡時分兩條路 ——

```
手牌用盡
   │
   ├─ 目標可重試且付得起 ──▶ 詢問「要付代價再來一輪嗎？」
   │                             ├─ 是 ─▶ 付款 → 衰減重置 → 手牌重抽 → 繼續（次數累加）
   │                             └─ 否 ─▶ 結案
   └─ 不可重試 ────────────────────────▶ 結案
```

**結案** = 換 `Failed Sprite`、**絕不消失**、回報房間、之後點擊給提示而不是沉默。

> 失敗刻意**不共用** `Interacted Sprite`（那是「打開的寶箱」）——
> 沒撬開的箱子長成打開的樣子會直接誤導玩家以為自己成功了。

---

## 代價公式

```
總代價 = 大類型固有倍率 × 基礎代價的遞增結果
```

`Assets/TYN/Core/RetryCost.asset`（已建好，預設值即你定的暫定值）：

| 欄位 | 預設 | 說明 |
|---|---|---|
| `Tier1 Multiplier` | `1` | 一般物件。**測試用的普通寶箱是這一級** |
| `Tier2 / 3 / 4` | `2` / `3` / `5` | |
| `Tactical Death Multiplier` | `20` | 代價高到「要不要再試」本身就是重大決定 |
| `Base Cost` | `5` | 第一次重試的基礎代價 |
| `Increment Mode` | `Fixed` | 固定遞增。另有 `Multiply`（每次乘）與 `None`（不遞增） |
| `Increment Amount` | `5` | Fixed 時是「每次加多少」；Multiply 時是「每次乘多少」 |

所以 Tier1 的重試代價是 **5 → 10 → 15 → 20…**，Tier3 則是 **15 → 30 → 45…**。

> 大類型目前是寫死的五個欄位而不是 List —— 一來避開 Unity 的 `+` 零填充（HANDOFF §4.3），
> 二來你說了物件多了要重整分類，到時整個 enum 會被取代，現在不值得先蓋架構。

---

## 步驟 1 — 詢問面板（若第二批還沒建）

在 **`_TEMP_DialogueUI`** 底下、**最後一個**子物件：

```
_TEMP_DialogueUI
├── dialogbox
├── EncounterUI ␣        ← 加 Canvas Group（目前沒有）
└── RetryAskPanel        ← 初始停用
    ├── Blocker          ← 全螢幕 Image、alpha 0、Raycast Target 勾選
    ├── Label (TMP)      ← 代價文字會寫進這裡
    ├── Btn_Yes
    └── Btn_No
```

`[SYSTEM]` → `Add Component` → **`Encounter UI Controller`**：

| 欄位 | 拖什麼 |
|---|---|
| `Ask Mode` | **`Never`** —— 這是每次失敗都問的舊模式，維持關閉 |
| `Retry Ask Panel` | `RetryAskPanel` |
| `Retry Ask Label` | 裡面的 `Label` |
| `Hand Interaction Group` | `EncounterUI` 上的 `Canvas Group` |

按鈕 `OnClick` 接 `[SYSTEM]` → `EncounterUIController → OnRetryYes ()` / `OnRetryNo ()`。

> `Ask Mode` 只管「每次失敗都問」那個舊模式。**手牌用盡的重試詢問跟它無關**，
> 只要目標可重試且面板存在就會跳。

---

## 步驟 2 — 設定寶箱

`chest_RequiresCheck.prefab`：

| 欄位 | 值 |
|---|---|
| `Retry Policy` | **`RequiresItem`** |
| `Tier` | `Tier1` |
| `Retry Cost` | `Assets/TYN/Core/RetryCost.asset` |
| `Retry Item Id` | `lockpick`（自訂） |
| `Failed Sprite` | 撬壞的鎖。**留空則維持原圖**（仍可測，只是看不出差別） |

> `CostsHealth` / `CostsSanity` 選了會在 Console 說明「尚未接上」並直接結案 —— 見文末。

---

## 步驟 3 — 讓玩家拿得到道具（否則測不到重試）

`RunContext.inventory` 開局是空的，所以**不做這步就永遠付不起**，重試詢問不會出現。

`chest_Direct.prefab` → `Granted Item Ids` 加入 `lockpick`。

這樣測試迴圈是：**開普通木箱拿到撬棍 → 撬鎖失敗 5 次 → 被問要不要用撬棍再來一輪**。
`chest_Direct` 的權重是 1，會生成。

### ⚠️ 新道具一定要登記進 `ItemDatabase`

背包存的是**字串 id**（`RunContext.inventory` 是 `List<string>`），玩家看到的名字要靠
`Assets/TYN/Core/ItemDatabase.asset` 翻譯。沒登記的話詢問文字會直接印出 id ——
玩家會看到「要用掉一個 **lockpick**」而不是「一個**撬棍**」。

新增道具的三步：

1. `Assets/TYN/Core/Items/` 建 `ItemData`（右鍵 → Create → Eldritch → Item）
2. 填 `Id`（程式認的，**定了不要改**）與 `Display Name`（玩家看的，隨時可改）
3. 把它加進 `ItemDatabase.asset` 的 `Items` 清單

> `GameFlowManager` 的 `Item Database` 欄位要指到那個資產（已設好）。
> 留空不會壞，只是所有道具名都會退回顯示 id。

目前已登記：`lockpick`（撬棍）、`key_warehouse`（倉庫鑰匙）。

---

## 步驟 4 — 驗收

### 不可重試（先把 `Retry Policy` 設回 `None` 測這段）

- [ ] 5 張全失敗 → 「鎖芯已經被撬爛了。這箱子開不了了。」
- [ ] 箱子點不動了，再點 → 「已經沒救了，別再費工夫。」（不是沉默）
- [ ] `Failed Sprite` 有設的話 → 圖換掉了，而且**箱子沒有消失**
- [ ] **把房間其他東西也互動完 → 「要探索其他的東西嗎？」會自己跳出來**

> 最後一項是這批的核心 —— 它之前根本不會發生。

### 可重試（`Retry Policy` = `RequiresItem`，且身上有 `lockpick`）

- [ ] 5 張全失敗 → 跳出「還能再撬一次，但要用掉一個「lockpick」。要試嗎？（第 1 次重試）」
- [ ] 詢問期間**手牌拖不動**
- [ ] 按「是」→ 道具被消耗、**手牌重抽 5 張**、Console 印出 `衰減已重置回 1.00`
- [ ] hover 手牌 → 機率**回到初始值**（不是 0）
- [ ] 次數提示**繼續累加** —— 第 6 張牌顯示「第 6 次」，**不是**回到「第 1 次」
- [ ] Console 的重試那行印出 `Tier1 若改用數值資源，本次代價為 5`
- [ ] 再全失敗一次 → 第二次詢問寫「第 2 次重試」，Console 代價變成 `10`（+5 遞增）
- [ ] 身上沒有 `lockpick` 時 → **不會跳詢問**，直接結案
- [ ] 按「否」→ 結案，與不可重試的結果相同

---

## 常見錯誤

| 症狀 | 原因 | 解法 |
|---|---|---|
| 手牌用盡後沒跳詢問 | `Retry Policy` 是 `None`、或身上沒有道具、或 `Retry Cost` 沒拖 | 後兩者 Console 都會說明原因 |
| 詢問跳了但文字沒有代價 | `Retry Ask Label` 沒拖 | 面板會維持你在 Inspector 打的固定文字 |
| 重試後機率還是 0 | `ResetForRetry` 沒跑到 | Console 應有 `衰減已重置回 1.00`；沒有的話是付款失敗走了 Decline |
| 重試後次數回到「第 1 次」 | 有人把 `RefillHand` 換成了 `Begin()` | **不可以** —— `Begin()` 會把 `cardsPlayed` 歸零，玩家會看到「第 1 次」卻被收第 N 次的錢 |
| 換環節後手牌用盡會噴錯 | `HandExhaustedInterceptor` 沒清 | 已處理：`HandleEncounterEnded` 與 `OnStageExit` 都會清掉 |
| 房間還有東西沒互動就宣告清空 | 有物件回報兩次 | 已修：`ReportInteracted` 改用 `HashSet` 去重 |
| **手牌打完時看不到成功／失敗的文字，直接跳物品結算** | 見下方「訊息被吃掉的三個來源」 | 已修 |

---

## 訊息被吃掉的三個來源（踩過，會再踩）

打牌收尾時有三個地方會把「剛顯示的判定結果」弄不見，症狀都一樣 ——
**玩家看不到最後一張牌的成功／失敗，畫面直接變成下一則**。三個都修好了，但改這一帶時要記得：

### ① `ShowInstant` 會蓋掉正文

`PopupService.ShowInstant()` 會 `pending.Clear()` 並**直接替換**對話框內容。
它是給「連續出牌時即時更新判定結果」用的（C18③），**不適合收尾訊息** ——
收尾時對話框上正顯示著最後一張牌的結果，蓋掉就等於玩家沒看到。

> 收尾類訊息一律用 `ShowText()`（排隊），讓玩家點過去。

### ② `AdvanceImmediate()` 只有在玩家主動結束時才成立

它的用意是「按結束＝相當於在對話框點一下」，讓玩家不必再手動點。但**手牌用盡是自動結束**，
玩家什麼都沒按 —— 這時候推進會在同一個呼叫堆疊裡把結果文字
`SkipTyping → Hide → Drain` 一路推掉，那一則存在 **0 幀**。

> 已改為 `if (Encounter.EndedByPlayer) box?.AdvanceImmediate();`
> `EndedByPlayer` 由 `EndEncounter(bool playerInitiated)` 設定，只有
> `PlayCard` 的手牌用盡那條傳 `false`。

### ③ 詢問面板立刻彈出會蓋住還在打的字

重試詢問若在判定結果打字打到一半就彈出來，玩家還沒讀到自己為什麼失敗，
就要決定付不付錢重來 —— 那是個沒有依據的決定。

> 已改為等 `DialogueBoxUI.IsTyping` 結束再彈（`ShowRetryOfferAfterText`），
> 與 `AskMode.OnFailure` 走同一套等待。用 `while (IsTyping)` 而不是固定秒數，
> 因為文案長度是企劃在調的。

---

## HP／SAN 為什麼還沒接（段 B）

**不是因為難，是因為要先跟 Romtyui 談一件事。**

| 需要的 | 現況 |
|---|---|
| HP／SAN 數值 | ✅ `RunStateManager`（Romtyui）已有，跨戰鬥保存。**Romtyui 已經把 Energy 就叫 SAN** |
| 歸屬 | ✅ 設計文件 §8 早已定案，以 `RunStateManager` 為準 |
| 「探索中死亡」結局 | ✅ `StageResult.PlayerDied` + `GameFlowManager` + `ContributeToMeta` 都在 |
| TYN 讀寫的橋 | ❌ 唯一缺的 |

### ⚠️ 要談的那一件事

`savedPlayerCurrentHp` 只有在 `SaveFromBattle()` 之後才有值 —— **第一場戰鬥打完才存在**。
玩家若在任何戰鬥之前先進探索房間，HP 是 `0`。探索要扣值，就必須要求
「**run 開始時就初始化 HP／SAN**」。

與 §7.2 的 `OnBattleEnded` 是同一類跨隊需求，現在提比事後補便宜。

### 談定之後要做什麼

只有 `ChestInteractable.TryPayForRetry()` 裡的一段 —— 把「檢查／扣除」換成讀寫
`RunStateManager`，並在扣到 0 時 `ReportComplete(StageResult.PlayerDied)`。
`CanOfferRetry` 裡擋住 `CostsHealth` / `CostsSanity` 的那段警告一併移除。

**已決定的規則**：歸零預設**直接死亡**；另做一個「保底留 1、探索不會死」的**可啟用狀態**
（既方便測試，日後也能當成特殊道具的效果）。

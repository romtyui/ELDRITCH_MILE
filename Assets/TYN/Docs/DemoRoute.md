# 最小可跑流程（給組員看的 demo）

> 建立：2026-08-16 · **目標：一條從主選單走到最後、不會斷的路線**
>
> 這不是正式的遊戲配置，是**刻意調成全部可跑**的展示設定。
> 要恢復正常請看文末「跑完 demo 之後怎麼還原」。

---

## 為什麼需要這份設定

正常的地圖生成會產出：

| 節點類型 | 對應 Stage | 現況 |
|---|---|---|
| `Event` | `Explore` | ✅ **完整實作** |
| `Dialogue` | `Dialogue` | 🔶 **替身**（播對白，無選項） |
| `Shop` | `Shop` | 🔶 **替身**（無介面、無貨幣） |
| `SpecialEvent` | `SpecialEvent` | 🔶 **替身**（給探索牌代替神牌） |
| `Combat` / `Boss` | `Battle` | ❌ **完全沒有**，戰鬥還沒接（跨隊需求 A） |

而預設的 `combatChance` 是 **0.55** —— 中間層有一半以上是戰鬥節點，最後一層固定是 Boss。

點到沒有 prefab 的節點不會當掉（`StageHost` 有 null 防護），但**畫面會停在一個空的 Stage 上走不下去**，
Console 只會有一行 `[StageHost] 找不到 Battle 的 prefab`。

所以 demo 路線避開戰鬥，把其餘四種都走一遍。

---

## 已經幫你設好的（不用再動）

`Assets/TYN/Core/MapGenerationSettings.asset`：

| 欄位 | 值 |
|---|---|
| `Use Demo Route` | **✓**（改用固定直線路線，忽略隨機生成） |
| `Demo Route Kinds` | **探索 → 對話 → 商店 → 特殊事件 → 探索** |

`StageHost` 也已註冊五個 Stage（Menu / Explore / Dialogue / Shop / SpecialEvent）。

---

## ⚠️ 三個 Stage 是替身，不是實作

`Dialogue` / `Shop` / `SpecialEvent` 用的是 `StubStageController` ——
**進場 → 用現有的對話框播幾句話 → 自動回地圖**。刻意不做任何新 UI，
因為真正的介面是各自 Phase 的工作，現在做等於預先猜錯。

每個替身的最後一句話都會明講「這一段還沒實作、之後會有什麼」，
所以組員看 demo 時不會誤以為那就是成品。

| Stage | 演了什麼 | 真正還缺什麼 |
|---|---|---|
| `Dialogue` | 魔術師／坎貝爾的幾句對白 | **選項** —— 多選項 + 每個選項是 `IProbabilityTarget`，用機率卡打通。C18①③ 會一起落地 |
| `Shop` | 空店、留字條、直接拿走一根撬棍 | 商品 UI、**貨幣**（`RunContext` 目前沒有任何貨幣欄位）、C15「買完留在店裡繼續買」 |
| `SpecialEvent` | 小說家／克拉夫特給你一張牌 | 真正的神牌是**戰鬥卡**，歸 `RunStateManager.savedDeck`（Romtyui）。我方只負責「事件把牌給出去」 |

> 替身要換成真貨時，把子類別整個換掉即可 —— `StageHost` 的註冊與節點對應都不用動。

---

## 這條路線會經過什麼

```
主選單
  └─▶ 地圖下拉（5 個節點的直線）

  ① 探索 ─ 進房間
      ├─ 點普通木箱      → 直接開，拿到道具（撬棍／倉庫鑰匙）
      ├─ 點上鎖的箱子    → 打牌環節
      │     ├─ 拖曳或點兩下出牌
      │     ├─ hover 看各張牌的成功率（直覺牌對它是 ✕，會變暗）
      │     ├─ 判定結果即時顯示，第二次起附「這是你嘗試的第 N 次」
      │     ├─ 失敗五次 → 問要不要用撬棍再來一輪（衰減重置、重抽手牌）
      │     └─ 不重試 → 箱子結案（換圖、不再可點）
      └─ 點 ExitTag → 「確定要離開嗎？」→ 是 → 回地圖

  ② 對話 ─ 魔術師／坎貝爾的幾句對白（替身，無選項）

  ③ 商店 ─ 空店留字條，直接拿走一根撬棍（替身，無介面無貨幣）

  ④ 特殊事件 ─ 小說家／克拉夫特給你一張牌（替身，探索牌代替神牌）

  ⑤ 探索 ─ 再走一次房間。**這次手牌裡會多出 ④ 給的那張牌**
```

第 ⑤ 站是刻意安排的 —— 它證明 ④ 給的牌真的進了牌組，流程是**通的**而不是各自獨立的畫面。

**沒有涵蓋的**：戰鬥（跨隊需求 A）。見 [SystemsStatus.md](SystemsStatus.md)。

---

## 跑之前建議確認的三件事

1. **Play 之前存過場景**（Ctrl+S）—— 今天有幾項設定是透過工具改的
2. **Console 清空** —— 方便看 `[判定]` `[打牌]` `[探索]` 這些流程 log
3. **地圖 hover 目前沒有視覺回饋** —— 見下

### 地圖節點 hover 現在沒反應（今天的變更）

原本 hover 會放大，2026-08-16 改成顯示 tooltip，但**tooltip 面板還沒在編輯器裡建**。
所以現在滑過節點沒有任何回饋。兩個選擇：

| 選項 | 做法 | 花費 |
|---|---|---|
| **A. 暫時把縮放開回來**（demo 前建議） | `MapNodeUI`（三個 NodeUI prefab）的 `Scale Hover` 設 **`1.1`** | 1 分鐘 |
| **B. 把 tooltip 面板建起來** | 見 [MapPolish.md](MapPolish.md) | 10 分鐘 |

A 和 B 不衝突，之後 tooltip 上線再把 `Scale Hover` 調回 `1` 即可。

---

## 跑完 demo 之後怎麼還原

| 項目 | demo 值 | 正式值 |
|---|---|---|
| `MapGenerationSettings.Use Demo Route` | ✓ | **取消勾選** |
| `MapNodeUI.Scale Hover`（若用了選項 A） | 1.1 | **1** |
| `RoomContent_Village` 的 `chest_RequiresKey` / `chest_Document` 權重 | 0 | **1** |

> 前兩項等戰鬥接上（跨隊需求 A）就該還原。
> 第三項是更早的測試腳手架，見 [Phase4c4_Attributes.md](Phase4c4_Attributes.md)。

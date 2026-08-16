# Phase 6（第一批）— 對話／商店／特殊事件 三個 Stage

> 建立：2026-08-16 · 程式已完成（編譯 0 error 0 warning）
>
> **狀態：實作完成，尚未完整驗收。** 對話節點已測到「選項可點、可拖曳出牌」，
> 商店與特殊事件尚未逐項確認。

---

## 這批做了什麼

事件流程從「只有探索」擴充成**四種可操作的節點**：

```
主選單 → 地圖
 ① 探索       撬鎖打牌（既有）
 ② 對話       三個選項，每個都是判定目標，用機率卡打
 ③ 商店       列商品、挑一件
 ④ 特殊事件   攤幾張牌、挑一張（進牌組）
 ⑤ 探索       再走一次，手牌裡會有 ④ 給的牌
```

**戰鬥完全避開** —— `Battle` Stage 沒有 prefab，卡在跨隊需求（見 [SystemsStatus.md](SystemsStatus.md) §2.1）。

### 新檔案

| 檔案 | 職責 |
|---|---|
| `Core/DialogueOptionUI.cs` | 一個選項。**同時是 `IProbabilityTarget`** |
| `Core/DialogueOptionsPanel.cs` | 管理 `answer_1/2/3` 三個槽，三種用途共用 |
| `Stages/ChoiceStageController.cs` | 「開場白 → 選項 → 結果 → 回地圖」的共用基底 |
| `Stages/DialogueStageController.cs` | 對話：選項要用機率卡打通 |
| `Stages/ShopStageController.cs` | 商店：`PlainChoice`，挑一件 |
| `Stages/SpecialEventStageController.cs` | 特殊事件：`PlainChoice`，挑一張牌 |
| `UI/Scripts/HoverRaiseLayer.cs` | 滑鼠進入時把 UI 提到同層最上面 |
| `Stages/Stage_{Dialogue,Shop,SpecialEvent}.prefab` | 三個 Stage，已註冊進 `StageHost` |

### 一併落地的三條 C18 約束

- **C18①** 主要目標選定 —— **刻意沒做入口**（見下方「操作方式」）
- **C18③** 判定結果反映在選項內文 —— 選項有文字元件了，`AppendResultText()`
- **C18⑦** 蓄意失敗合法 —— 判定成功不自動結束

---

## 操作方式（定案）

**兩條路並存，語意一致，與探索房間完全相同：**

| 方式 | 操作 |
|---|---|
| 拖曳 | 把卡拖到選項／特寫圖上放開 |
| 點選 | 先點卡（標記）→ 再點目標（＝出牌） |

**沒有做「點選項＝選定主要目標」** —— 那會讓同一個點擊有兩種意思，玩家分不出自己觸發了哪一種。副作用是 C18① 目前沒有入口，多選項時手牌不會變暗（「無效」沒有唯一答案，硬壓暗會騙人）。要做得另找不搶點擊的手勢。

### 瞄準回饋

`IProbabilityTarget.SetTargeted(bool)` —— 卡片瞄準的目標會**稍微變暗**。拖曳中每幀更新、兩段式選卡後滑過去也會。

狀態放在 `DialogueEncounterController.AimedTarget`（Core），因為拖曳由手牌區驅動、hover 由目標自己驅動，兩條路要改同一份狀態。

---

## ⚠️ 還沒清掉的暫時診斷

Console 會印四種訊息，確認流程穩定後要一起移除（都標了 `TODO 暫時診斷`）：

| 訊息 | 位置 |
|---|---|
| `[手牌] 選取切換` | `ExploreHandUI.ToggleSelect` |
| `[選項] 收到點擊` | `DialogueOptionUI.OnPointerClick` |
| `[拖曳] 開始` | `ExploreCardDrag.OnBeginDrag` |
| `[拖放] 放開於…命中 N 個` | `ExploreCardDrag.OnEndDrag` |

> ⚠️ `[拖放]` 那個**必須留在 `OnEndDrag`**，不可以搬回 `FindTargetUnder` ——
> 那支現在每幀都會被呼叫（瞄準回饋），寫在裡面 Console 會被洗爆。

---

## 尚未驗收的項目

- [ ] 商店：開場白 → 兩件商品 → 挑一件 → 收尾 → 回地圖
- [ ] 特殊事件：挑一張牌 → **下一站探索的手牌裡真的有它**
- [ ] 對話：三個選項各自顯示不同機率（邏輯／直覺／批判與創造）
- [ ] 對話：判定結果接在選項內文後（`→ 成功` / `→ 失敗`）
- [ ] 對話：第三個選項成功會給撬棍
- [ ] 手牌用盡 → 收尾 → 回地圖

---

## 這一批踩過、已修好的坑

全部寫進 [HANDOFF.md](HANDOFF.md) §4.6，這裡只列索引：

1. `HoldOpen` 設定時機太晚 → 選項是對話框的子物件，框關了選項就看不見
2. Unity 指標事件只往**祖先**傳，不傳兄弟 → `HoverRaiseLayer` 掛錯層級
3. 地圖覆蓋層是**滑出畫面**不是停用 → 靠 `OnDisable` 收尾一律失效
4. 遲到的 `OnPointerExit` 會把剛關掉的東西復活 → 需要總開關
5. hover 改變被 hover 的東西 → 上浮把卡片從游標底下抽走，瘋狂閃爍
6. `SetParent` 是附加到最後 → 倒著迭代會把圖層順序整個翻過來
7. `RaycastAll` 會回傳被遮住的東西 → 投放判定只能認最上層
8. 兩個東西都在搶 `SetAsLastSibling` → 拖曳要用專用圖層
9. `Advance Button` 蓋在選項上 → 透明的全幅按鈕會吃掉所有點擊

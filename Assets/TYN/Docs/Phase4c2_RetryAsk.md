# Phase 4c（第二批）— C12 回合感 操作指引

> 對應 `SceneConsolidationPlan.md` C12、`RoadmapNext.md` §1。程式已完成（編譯 0 error 0 warning）。
>
> **本批目標**：讓「失敗之後還能再試」變成玩家看得懂的回合，而不是靠他自己想到還能再拖一張。
>
> 版本：2026-08-15 · 預估 **2 分鐘**（採用建議做法）· 前置：Phase 4c 第一批已跑通

---

## 結論先講：不用做確認視窗

C12 原本的想法是「判定失敗後跳出詢問」。實作完之後回頭檢討，**每次失敗都跳確認是錯的**：

| 問題 | 說明 |
|---|---|
| 資訊只有第一次是新的 | 「你還可以再試」玩家看一次就懂了。第二次之後純粹是摩擦 |
| 前提就不成立 | 當初的理由是「失敗沒有出口」。但**出口一直都在** —— 「結束」按鈕整個環節都在畫面上 |
| 代價早就顯示了 | 「再試要付什麼代價」hover 的預覽數字已經答了，玩家看得到機率下降。彈窗是重複 |
| 打斷核心迴圈 | C18 的核心是**連續**嘗試。5 張手牌失敗 4 次 = 點掉 4 個彈窗，把設計成流暢的迴圈切成四段 |

**改用**：回合感寫進判定結果文字，零打斷。

```
第 1 次失敗 →「沒能撬開。鎖紋風不動。」
第 2 次失敗 →「沒能撬開。鎖紋風不動。
              這是你嘗試的第 2 次。」
```

搭配 hover 時**下降中的預覽數字**（「第 3 次」＋「機率剩 20」），那才是玩家真正需要的回合感。

---

## 步驟 1 — 設定文案（這就是全部了）

選 `[SYSTEM]` → `Dialogue Encounter Controller`：

| 欄位 | 建議值 | 說明 |
|---|---|---|
| `Attempt Suffix Format` | `\n這是你嘗試的第 {0} 次。` | `{0}` = 第幾次。**留空則完全不附加** |

**第一次不會附加** —— 第一次沒有「又」的語氣，寫「第 1 次」反而像系統在報數。

> 成功時也會附加。這是**回合計數**不是失敗計數，而且判定成功後環節不會自動結束（C18⑦），
> 玩家可能還會繼續打。第 3 次才成功時顯示「這是你嘗試的第 3 次」讀起來是對的。

> 少打了 `{0}` 不會爆 —— 程式會直接把整串接在後面，而不是讓 `string.Format`
> 把判定結果文字換成一個例外。

### 涵蓋範圍

目前只有 `ChestInteractable` 接上（探索的寶箱）。Phase 6 的對話選項要一起吃這個效果的話，
在它的 `OnCheckResult` 裡照樣呼叫 `DialogueEncounterController.WithAttemptLine(body)` 即可 ——
一行。

---

## 測試腳手架 — 兩個「看起來像 bug，其實是故意的」

⚠️ **這兩項是為了測試方便而刻意設成這樣的，不要順手「修好」它。**

### `RoomContent_Village` 有兩筆 `weight: 0`

`chest_RequiresKey` 與 `document` 的權重是 **0**，所以它們**永遠不會生成**。

**這是刻意的** —— 把其他條目關掉，`chest_RequiresCheck`（打牌用的那個）才不會被稀釋掉、
每次進房間都抓得到。HANDOFF §4.3 講的「Inspector 的 `+` 會零填充導致條目被靜默跳過」
症狀一模一樣，所以很容易被誤判成那個坑。**它不是。**

> 正式內容配置前要記得把權重補回來，否則鑰匙寶箱與文件永遠不出現。

### ~~`AttributeChart` 有一條測試規則~~ → 已被正式相剋表取代

> **2026-08-15 更新**：Q7a 屬性命名定案，測試用的那條規則已換成正式的兩條。
> 這一段留作歷史記錄，現行相剋表見 [Phase4c4_Attributes.md](Phase4c4_Attributes.md)。

當時為了**測得到 `✕` 顯示**而加了一條 `AttrA → AttrC = None`，因為相剋表原本
`rules` 是空的、`defaultEffectiveness` 是 `Partial`，沒有任何組合會算成 `None`。

---

## 步驟 2 — 驗收

- [ ] 出一張牌**第 1 次失敗** → 只有「沒能撬開。鎖紋風不動。」，**沒有**次數那行
- [ ] **第 2 次失敗** → 多出「這是你嘗試的第 2 次。」
- [ ] **第 3 次** → 數字跟著變成 3
- [ ] 同時 hover 手牌 → 預覽數字**已經下降**（衰減有生效，C18④）
- [ ] **判定成功** → 一樣會附加次數，且環節**不會**自動結束（C18⑦）
- [ ] 中途按「結束」再點同一個寶箱 → 接續剩下的手牌，次數**接著算**（不是從 1 重來）
- [ ] hover **A 屬性**的卡 → 大圖上顯示 **`✕`**（不是 `0`）

### 預期數字（測試寶箱是 `Logic`、手牌 5 張 → 每張衰減 0.2）

| 卡 | 第 1 張 | 第 3 張（衰減 0.6） |
|---|---|---|
| `explore_Logic_60` | **60**（`Match`） | **36** |
| `explore_Insight_60` | **30**（`Partial` 0.5×） | **18** |
| `explore_Intuition_60` | **`✕`**（`None`） | **`✕`** |

對不上就是有東西沒接好。Console 的 `[判定]` 那行會印出完整算式可以對照。

> 最後一項是刻意的：衰減記在目標身上、不會因為重進而重置，次數也該一致。

---

## 附錄 — 確認視窗（預設關閉，留著比較手感用）

程式裡仍有一個可運作的確認視窗實作，預設**不啟用**。想實際比較兩種手感再打開。

`[SYSTEM]` → `Add Component` → **`Encounter UI Controller`**：

| 欄位 | 說明 |
|---|---|
| `Ask Mode` | **`Never`（預設・建議）** ＝ 不跳確認<br>`OnFailure` ＝ 每次失敗且還有手牌時跳 |
| `Encounter` | 留空即可，自動抓 `Instance` |
| `Retry Ask Panel` | 詢問面板。`Ask Mode` 不是 `Never` 時才需要 |
| `Hand Interaction Group` | 詢問期間要停掉互動的 `CanvasGroup` |
| `Ask Delay` | `0.35`。失敗那句**打完之後**再等這麼久才跳 |

### 若要啟用 `OnFailure`，UI 這樣建

場景現況（Unity MCP 讀出來的）：

```
[SYSTEM]                      Transform / UIDirector / DialogueEncounterController
_TEMP_DialogueUI              Canvas / CanvasScaler / GraphicRaycaster / DialogueBoxUI / UIPanel
├── dialogbox
└── EncounterUI ␣             RectTransform / ExploreHandUI
    ├── HandRoot
    └── Btn_EndEncounter
```

1. `EncounterUI` 上 `Add Component` → **`Canvas Group`**（目前沒有），拖給 `Hand Interaction Group`
2. `_TEMP_DialogueUI` 底下**最後一個**子物件建 `RetryAskPanel`（初始停用），內含全螢幕 `Blocker`、
   `Label`、`Btn_Yes`、`Btn_No`
3. 按鈕 `OnClick` 接 `[SYSTEM]` → `EncounterUIController → OnRetryYes ()` / `OnRetryNo ()`
   （兩者都住在場景裡，拖得到）

**NO ＝ 結束打牌環節，不是離開房間** —— 走跟「結束」按鈕同一條 `EndEncounter()`。

### 這幾個坑

| 症狀 | 原因 | 解法 |
|---|---|---|
| 面板跳出來但看不見 | RectTransform 死值：`Scale (0,0,0)` 或寬高 0 | HANDOFF §4.1 |
| 面板被對話框蓋住 | 不是最後一個子物件 | 同 Canvas 內用 sibling 順序決定疊放 |
| 詢問期間還能拖手牌出牌 | `Hand Interaction Group` 沒拖 | 那張牌會繞過整個詢問，還會再吃一次衰減 |
| `Blocker` 擋不住底下的東西 | `Image` 被停用了 | 要用 **alpha 0**，不是取消勾選元件（停用的 Graphic 收不到 raycast，且不報錯） |
| 下一次遭遇手牌整排點不動 | `CanvasGroup` 沒還原 | 程式三處都會還原；若仍發生，檢查有沒有別的東西也在改同一個 `CanvasGroup` |

---

## 還沒做：手牌用盡時的「逆轉」

構想是：手牌用盡時給一個特殊狀態，問玩家要不要逆轉。**這個現在寫出來會是假選擇**：

`DecayStep = 1 / 手牌數`（`Decay Scaled To Hand Size` 勾選時）。出完 N 張後衰減倍率是
`1 − N×(1/N)` = **正好 0**。而 `ProbabilityCheck` 算的是 `base × 相剋 × 衰減`，
又對 `finalRate <= 0` 直接短路成失敗 —— 所以玩家點「逆轉」得到的是**保證 0% 的嘗試**。

> `Fixed Decay Step` 模式下不一定歸零（手牌 3 張 × 0.2 只扣到 0.4），但那不是目前的建議設定。

要做這個模式，得先決定兩件事：

| 待決 | 為什麼卡住 |
|---|---|
| **逆轉如何處理衰減** | 重置回 1.0？回到某個比例？還是逆轉用的牌直接無視衰減？不解決這條，功能等於沒有 |
| **逆轉要付什麼代價** | 免費的額外嘗試會直接廢掉 C18⑤（手牌數 ＝ 嘗試次數上限）。候選：消耗道具／消耗 HP 或理智／一次遭遇限一次／消耗 Phase 7 的神牌 |

決定之後，`AskMode` 加第三個值 `OnHandExhausted`，並在
`DialogueEncounterController.PlayCard` 的「手牌用盡自動結束」那裡加一個攔截點（約 5 行）。

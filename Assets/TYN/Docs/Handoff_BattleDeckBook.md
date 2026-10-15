# 交接：戰鬥中開卡冊時，遺物／食物 UI 沒被模糊蓋住

2026-10-14。用量快用完，先整理方向，換帳號後照這份做。

## 問題
Romtyui 回報：戰鬥裡打開卡冊（左下角的書）時，我們的遺物／食物 UI 浮在模糊背景**上面**。
在商店開卡冊時是對的（被模糊蓋住）。

**原因**：
- 商店開的是我們的 `DeckBook.prefab`（Overlay 畫布，sortingOrder **7000**）→ 比 HUD（400）高，HUD 會被截進模糊背景、再被卡冊蓋住。
- 戰鬥裡開的是**他自己的** DeckViewer（在 `BattleUICanvas(No Rendering)` 底下，由 FinalUICamera 畫，排序 FinalUICanvas/207）。
  我們的 HUD 是 Overlay 畫布 → **Overlay 永遠畫在所有相機之後**，所以一定在他的卡冊上面。這不是調排序數字能修的。

## 兩個方向

### 方向 2（建議）：戰鬥裡點書也開我們的 DeckBook，資料直接用戰鬥的牌堆
- 工作量小、不改他的檔、不改 prefab，只在 runtime 換掉那顆按鈕的行為。
- 遺物／食物 UI 不動，戰鬥中照樣能吃東西（他那邊的道具「使用」流程還沒做，見方向 1）。
- 已查過：他的 DeckViewerUI / DeckViewerBlur **沒有**跟教學或其他系統綁在一起，全專案也沒有別人引用 DeckViewerUI，換掉安全。

**步驟**
1. `Assets/TYN/UI/System/DeckBookUI.cs`：加一個多載
   `public void Open(BattleDeck liveDeck)` ——
   - `liveDeck != null`：`viewer.battleDeck = liveDeck`，**不要**呼叫 `RestoreDrawPileOrderOnly`（那會清空戰鬥的牌堆！），四個分頁都顯示（`SetBattleOnlyTabsVisible(true)`）。
   - `liveDeck == null`：照現在的戰鬥外行為（灌 savedDeck、藏三個分頁）。
   - 關閉時把 `viewer.battleDeck` 指回自己的 `bookDeck`，避免下次戰鬥外打開時還指著已銷毀的戰鬥牌堆。
2. `Assets/TYN/Stages/BattleStageController.cs` 的 `BindToHostScene()` 加一支 `RerouteDeckButton()`：
   - 找 Stage 底下的書按鈕：`BattleContentRoot /BattleUICanvas(No Rendering)/Inventory/bag_Panel/root`（Button）。
     它的 persistent listeners 是 `DeckViewerUI.OpenDrawPile` 與 `DeckViewerBlur.Open`（v3 已確認）。
   - `for i in GetPersistentEventCount()`：目標是 DeckViewerUI 或 DeckViewerBlur 的，`SetPersistentListenerState(i, UnityEventCallState.Off)`。
   - `onClick.AddListener(() => DeckBookUI.Instance?.Open(battleDeckOfThisStage))`，`battleDeck` 用 `GetComponentInChildren<BattleDeck>(true)`。
   - ⚠️ 只在 runtime 改，**不要存回 prefab**（下次重打包會被覆蓋，而且會讓他的 SampleScene 行為跟我們不同步）。
3. 測試（Play Mode，背景要推幀，見 memory「MCP Play Mode 測試技巧」）：
   - 進戰鬥 → 點書 → 遺物／食物條在模糊背景裡、被卡冊蓋住。
   - 四個分頁的張數跟 BattleDeck 一致（抽牌堆／棄牌堆／消耗區／手牌）；出一張牌後再開，棄牌堆 +1。
   - 關掉後戰鬥照常；離開戰鬥、到商店開書 → 只剩牌組區、張數 = savedDeck。

**缺點**：他之後改了戰鬥裡那本書的樣子，`DeckBook.prefab` 不會跟著變（重打包時要一起重拷，memory 已記）。

### 方向 1：遺物／食物改用他戰鬥裡自己的面板，戰鬥中關掉我們的 HUD
- 他的 `Inventory/Props_Panel`（ItemInventory）與 `Relics_Panel`（RelicsInventory）在 v3 裡是關著的。
  遺物資料其實已經灌進去了（`BindRelicsFromInventory`），食物也有灌（`BindItemsFromInventory`）。
- **擋點**：他那邊的道具「使用」流程還沒寫（`ItemEffectData` 有兩個子類別，但沒有程式去執行它們），
  而且食物要有 `battleItemEffect` 資產才會進他的面板。改這條路，戰鬥中就**吃不了東西**，直到他補上使用流程。
- 適合等他把道具使用做完再一起做。

## 建議
先做方向 2（今天就能好、不影響吃東西），方向 1 等 Romtyui 的道具使用流程做完再評估要不要整合成他那一套。

## 目前狀態（給接手的人）
- 本機 main 領先 origin 15 個 commit，**還沒推**（祭壇、合併 Romtyui、Stage_Battle_v3、遺物修正、商店、解析度鎖）。
- 工作區只剩 Unity 自動產生的雜訊（EventScene Layout、packages-lock 3.2.0↔3.3.0），可以不管。
- 要轉告 Romtyui 的事見最近幾個 commit 訊息：BattleRewardUI 加了 `ShowOutsideBattle()`、他 SampleScene 的四塊畫布要改等比縮放、獎勵列遺物圖示是白色、獎勵會抽到 NoEffect 遺物。

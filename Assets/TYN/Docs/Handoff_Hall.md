# 交接：把大廳接進遊戲流程（給協助 Romtyui 的 AI）

2026-11-04，TYN 這邊整理。依據是 `origin/damege_test`（d04d8a7）的 `Assets/Romtyui_hall`，以及 TYN 本機 `main` 的流程程式。

**這是提案，還沒實作。** Romtyui 的指示優先於這份文件。「待決定」一節列出的事請問 Romtyui，不要自己定。

## 你要做什麼

把 `Assets/Romtyui_hall/test_Scene.unity` 裡的大廳畫面做成 `Assets/Romtyui_hall/Prefab/Stage_Hall.prefab`，讓 TYN 能掛進遊戲流程。

目標流程：

```
主選單 ─開始鍵→ 大廳 ─出發鍵→ StartNewRun(選擇) → 新手介紹（有 prefab 才演）→ 地圖
                 ↑                                                      │
                 └──────── 一輪結束（回大廳或主選單，待決定）────────────┘
```

## 範圍

| 誰 | 做什麼 | 位置 |
|---|---|---|
| Romtyui 這邊（你） | `Stage_Hall.prefab`、紀錄畫面、倉庫腳本的調整 | `Assets/Romtyui_hall/` |
| TYN | `StageType.Hall`、`GameFlowManager`、`MenuStageController`、`HallStageController`、StageHost 註冊、`MetaProgressData` 新欄位 | `Assets/TYN/` |

**不要做的事：**

- 不要改 `Assets/TYN/` 底下的任何檔案，也不要在裡面建檔。兩邊分工如此，`EventScene.unity` 尤其容易衝突。
- 不要自己寫 `HallStageController` 或新增 `StageType.Hall`，TYN 會寫。下面「TYN 會提供的介面」只是讓你知道資料怎麼走，**這些東西目前還不存在**，在你的分支上呼叫會編譯失敗。
- 不要用 `SceneManager.LoadScene` 切到大廳。整個遊戲只有 `EventScene` 一個場景，流程總管和 HUD 都在裡面，換場景會把它們一起銷毀。
- 不要合併 `main`，也不要重新打包戰鬥。那些由 TYN 處理。

## 分支現況

- `Romtyui_hall` 只在 `damege_test`，`main` 還沒有。
- TYN 的 `main` 比 `origin/main` 多 12 個 commit，都還沒推。所以你的分支**沒有** `MetaProgressData.lobby` 和隊伍系統，寫紀錄畫面時要等 TYN 推上去並同步之後才能讀大廳成員。其他紀錄欄位你的分支已經有。

## 步驟一：做 Stage_Hall.prefab

現在的 `test_Scene` 根物件：

| 根物件 | 處理 |
|---|---|
| `Canvas/Image`（背景） | 搬進 prefab |
| `Canvas/StartSelectionRoot`（`StartSelectionPageUI`、`FoodSelectionUI`、`RelicSelectionUI`） | 搬進 prefab |
| `FoodWarehouse`、`RelicWarehouse`、`StartingFoodLoadout`、`StartingRelicLoadout` | 搬進 prefab，當根物件的子物件 |
| `RatioTestController`（`RatioFlipTestUI`） | 搬進 prefab。它的滑條和方塊在 `MainPanel/牌組選擇` 底下，少了它滑條就沒反應 |
| `Canvas` 本身（Canvas、CanvasScaler、GraphicRaycaster） | **不要帶** |
| `Main Camera`、`EventSystem`、`Global Light 2D`、`Freeform Light 2D` | **不要帶**，EventScene 都有自己的 |
| `GameObject`（`PSBMonsterLightReveal`） | **不要帶**，測試用 |

要求：

1. **根物件是普通的 RectTransform**，全螢幕 stretch（anchor 0,0 到 1,1，offset 全 0），Scale (1,1,1)，上面不能有 Canvas。它會被生成在 `Canvas_Stage` 底下，`StageHost.ValidateInstance` 看到根物件帶 Canvas 或 Scale 為 0 會報錯。
2. **先把上表的物件都放到同一個根物件底下，再存成 prefab。** `FoodSelectionUI` 等腳本的 Inspector 引用指著這些物件，分開存會斷。
3. **拿掉四個倉庫／配置腳本 `Awake` 裡的 `DontDestroyOnLoad(gameObject)`**（`FoodWarehouse`、`RelicWarehouse`、`StartingFoodLoadout`、`StartingRelicLoadout`）。它們現在是子物件，Unity 只會噴警告，而且遊戲不換場景，本來就不需要。`Instance` 單例照留。
4. **排版改用 1920×1080 確認。** `test_Scene` 的 CanvasScaler 是 ConstantPixelSize，接進來後由 `Canvas_Stage` 縮放（ScaleWithScreenSize、1920×1080、match 0.5）。
5. **出發鍵與返回鍵的 `OnClick` 留空。** `MainPanel/Button` 目前沒有接任何事件，看起來就是出發鍵，請向 Romtyui 確認，並改名成 `DepartButton`。需要返回主選單的話，加一顆 `BackButton`。TYN 整合時會接上。

獨立測試方法：`test_Scene` 留著當測試場景，在裡面放一個帶 CanvasScaler（ScaleWithScreenSize、1920×1080）的 Canvas，把 prefab 拖進去跑。這個場景沒有 `GameFlowManager`，所以 `GameFlowManager.Instance` 是 null，用到它的地方都要判空。

完成的檢查標準：

- [ ] 在上述測試場景能開關食物和遺物面板、選格子、確認，滑條能翻方塊。
- [ ] Console 沒有 `DontDestroyOnLoad` 警告，沒有 Missing Reference。
- [ ] prefab 根物件沒有 Canvas，Scale 是 1。
- [ ] 沒有動到 `Assets/TYN/` 底下的檔案。

## 步驟二：紀錄畫面

讀 `GameFlowManager.Instance.Meta`（型別 `EldritchMile.Core.MetaProgressData`），`GameFlowManager` 會在 `Awake` 載入它，進大廳時一定有值。**唯讀使用，不要從大廳寫回去。**

| 欄位 | 內容 | 狀態 |
|---|---|---|
| `totalRuns`、`deaths` | 輪迴次數、死亡數 | 已在寫 |
| `bestLayerReached` | 最深層數 | 已在寫 |
| `runHistory` | `List<RunSummary>`：`runId`、`seed`、`result`、`startedAt`、`endedAt`、`steps`、`deepestLayer`、`gameVersion` | 已在寫 |
| `lobby` | 大廳成員的角色 id，用 `GameFlowManager.Character(id)` 取得 `CharacterData`（`.Label`、`.portrait`） | 只在 TYN 的 `main`，同步後才有 |
| `unlockedCardIds` | 解鎖的卡 | 欄位有，目前沒有東西會寫入 |
| `legacyItemIds` | 跨輪保留的道具 | 欄位有，目前沒有東西會寫入 |

大廳在開新一輪之前，所以這些時候不能假設有值：

- `GameFlowManager.Instance.Run` 是 **null**。
- `PlayerVitals`（HP、SAN、牌組）還沒初始化，要到按出發之後才有。

## 步驟三：倉庫存檔（等 TYN 加好欄位再做）

現在倉庫自己存 PlayerPrefs（`StartSelection.FoodWarehouse.v1`、`StartSelection.RelicWarehouse.v1`）。建議改存進 `MetaProgressData`，**公開 API 不變，只改 `Load`／`Save`**。原因：

1. 一輪結束時要把東西存進倉庫，但那時大廳 Stage 已經卸載，`FoodWarehouse.Instance` 是 null，寫不進去。Meta 隨時都寫得到，結算點 `RunContext.ContributeToMeta` 也在那裡。
2. `MetaProgressData.ClearSave()`（清除進度）只清 Meta，分開存的話倉庫會留著。

預計的欄位（名稱以 TYN 實際加的為準）：食物 `Meta.foodStock`（`List<ItemStack>`，`ItemStack` 有 `id`、`count`），遺物用現成的 `Meta.legacyItemIds`。

## TYN 會提供的介面（目前不存在）

```csharp
// Assets/TYN/Core/StageType.cs：加在 enum 最後。數字會存進 prefab 與場景，不能插在中間
Hall,

// Assets/TYN/Core/GameFlowManager.cs
public void EnterHall();                         // 主選單開始鍵呼叫。還沒有 Stage_Hall 時直接開新一輪
public void StartNewRun(List<string> picked);    // 地圖生成前把 picked 逐一 Run.AddItem

// Assets/TYN/Stages/HallStageController.cs：掛在 Stage_Hall 根物件上（TYN 整合時掛）
public class HallStageController : StageController
{
    public override StageType Stage => StageType.Hall;

    public void OnDepartClicked()   // DepartButton 接這裡
    {
        var picked = new List<string>();
        picked.AddRange(StartingFoodLoadout.Instance.CreateFoodIdSnapshot());
        picked.AddRange(StartingRelicLoadout.Instance.CreateRelicIdSnapshot());
        picked.RemoveAll(string.IsNullOrEmpty);
        GameFlowManager.Instance.StartNewRun(picked);
    }

    public void OnBackClicked()     // BackButton 接這裡
    {
        GameFlowManager.Instance.GoToMenu();
    }
}
```

**你的腳本會被這樣用，所以請保留：** `StartingFoodLoadout.CreateFoodIdSnapshot()`、`StartingRelicLoadout.CreateRelicIdSnapshot()`（回傳道具 id 清單，空格是空字串）、兩個 `Instance`。要改名或改回傳內容請先告訴 TYN。

大廳**不要呼叫** `StageController.ReportComplete()`：那支是「這一站結束，地圖下拉」，而這時候還沒有 Run，也沒有地圖。

## 待決定（請問 Romtyui，不要自己定）

1. **倉庫是否取代舊決定？** 2026-09-15 定案過「遺物與道具不能帶到新一輪」（`GameFlowManager.inheritLegacyItems = false`）。TYN 的建議解讀：自動繼承仍然關著，只多一條玩家手動選、有上限的路。
2. **倉庫怎麼進貨？** 目前沒有程式呼叫 `FoodWarehouse.AddFood`、`RelicWarehouse.AddRelic`，場景上的 `initialFoods`、`initialRelics` 也是空的，所以倉庫永遠是空的。通關才存、死亡也存、全存或挑一件，都還沒定。
3. **帶出去要不要從倉庫扣？** `TryAssign` 現在只檢查數量、不扣。如果要扣，死了之後回不回來？
4. **一輪結束回哪裡？** 現在死亡和通關都回主選單。TYN 建議回大廳。
5. **比例滑條要影響什麼？** `RatioFlipTestUI` 目前只換圖，沒有輸出數值，也沒有對外的讀取方法。如果要決定起始牌組比例，需要加一個讀取值的 API，開局的牌組來自 `GameFlowManager.startingDeck`。
6. **第一輪要不要進大廳？** 第一輪倉庫和紀錄都是空的。

## 容易踩的坑

- **格數要對得上。** `StartingFoodLoadout.slotCount`（3）要等於 `GameFlowManager.foodSlots`（3）。多帶的話，`RunContext.AddItem` 會在地圖打開時跳「食物欄滿了」。`StartingRelicLoadout.slotCount` 目前是 1。
- **道具要在資料庫裡。** `RunContext.AddItem` 靠 id 查 `ItemDatabase`。倉庫的 `database` 指的是同一份，維持這樣。
- **HUD 預設不顯示。** `UIDirector` 換站時會關掉 `visibleInStages` 沒列到該站的面板。大廳需要顯示哪個 HUD 面板，請告訴 TYN。
- **`Canvas_Stage` 的排序是 100。** 比它高的有對話框（101）和 HUD（400）。大廳不要自己再開高排序的 Canvas，要的話先跟 TYN 講。
- **Play Mode 中不要改 `.cs`。** Unity 會當場重新編譯，靜態欄位清空、協程全部停掉，畫面卡在黑屏，看起來很像流程壞了。

## 交回給 TYN 時

推上 `damege_test`，並告訴 TYN：

1. prefab 路徑，以及出發鍵、返回鍵的物件名稱。
2. 上面「待決定」各項 Romtyui 的答案。
3. 動過哪些公開 API。

整合時 TYN 會接著做：合併分支（`EventScene.unity` 的版面值雜訊以 `main` 為準）、戰鬥重新打包成 v8（這次 `SampleScene` 有動）、把 Stage_Hall 註冊進 StageHost、掛 `HallStageController`、接按鈕。

# 從主選單到地圖：流程怎麼連起來

2026-11-08，給 Romtyui（也可以直接給他的 AI 看）。大廳要插在哪裡、怎麼接，另見 `Handoff_Hall.md`。

## 一句話版

整個遊戲只有 **`EventScene`** 一個場景。主選單、探索、戰鬥、商店……每一個畫面都是一個 **Stage prefab**，由 `StageHost` 生成、換站時銷毀；**地圖不是 Stage**，是一直存在的覆蓋層（`MapOverlayController`），只是收起來或打開。總指揮是 `GameFlowManager`（`Assets/TYN/Core/GameFlowManager.cs`）。

```
開遊戲
  └─ GameFlowManager.Start() ─ SwitchStage(bootStage = Menu)
        └─ 生成 Stage_Menu（MenuStageController）

主選單按「開始」
  └─ MenuStageController.OnStartClicked()
        └─ GameFlowManager.StartNewRun()
              ├─ 清掉上一輪的狀態（ResetRunLevelState）
              ├─ 建立這一輪的資料 RunContext（金錢、隊伍、HP／SAN、牌組）
              ├─ 生成這一區的地圖（BuildRegionMap）
              ├─ 黑幕淡出 → 卸載主選單（有新手介紹 Intro 的話先演它）
              └─ 打開地圖（RevealMapRoutine）

地圖上點節點
  └─ MapView3D.OnNodeClicked → GameFlowManager.EnterNode(node)
        ├─ 收起地圖、記下走到哪
        ├─ （有插播事件的話先演事件 Stage）
        └─ 換到這個節點的 Stage（Battle／Explore／Shop…）

Stage 結束
  └─ StageController.ReportComplete() → GameFlowManager.NotifyStageComplete(result)
        ├─ Completed：卸載 Stage → 打開地圖
        └─ PlayerDied／RunFinished：寫入跨輪資料（Meta）→ 結局事件 → 回主選單
```

## 關鍵檔案

| 檔案 | 做什麼 |
|---|---|
| `Core/GameFlowManager.cs` | 全部的換站流程。`StartNewRun`（約 340 行）、`EnterNode`（349）、`NotifyStageComplete`（358）、`SwitchStageInternal`（1021）、`StageTypeForNode`（1192，節點種類 → Stage） |
| `Core/StageHost.cs` | 依 `StageType` 生成／銷毀 Stage prefab。清單在 EventScene 的 `[STAGE_HOST]` 上 |
| `Core/StageType.cs` | Stage 的種類（Menu、Explore、Battle、Shop…）。**新增要加在最後面**，數字會存進 prefab 與場景 |
| `Core/StageController.cs` | 所有 Stage 的基底：`OnStageEnter(run)`、`OnStageReady()`、`OnStageExit()`、`ReportComplete()` |
| `Stages/MenuStageController.cs` | 主選單。開始鍵接 `OnStartClicked()` |
| `Core/MapOverlayController.cs` ＋ `Map/Map3D/MapView3D.cs` | 地圖的開關與畫面；點節點從 `MapView3D.OnNodeClicked` 進來 |
| `Core/RunContext.cs` | 這一輪的資料。**`StartNewRun` 之前不存在**（主選單時 `GameFlowManager.Instance.Run` 是 null） |
| `Core/MetaProgressData.cs` | 跨輪資料（輪迴次數、死亡數、最深層數、遊玩紀錄、大廳成員），`GameFlowManager.Instance.Meta` |

## 換站時固定會發生的事（`SwitchStageInternal`）

不必每個 Stage 自己處理，總指揮會統一做：

1. 收掉對話框、手牌區、氣泡
2. 呼叫舊 Stage 的 `OnStageExit()`，然後銷毀它
3. 生成新 Stage，呼叫 `OnStageEnter(run)`
4. `UIDirector.ApplyStage(...)`：HUD 上沒列這一站的面板會關掉
5. 黑幕淡入後呼叫 `OnStageReady()`

## 戰鬥這一站怎麼接

`Stage_Battle_vN.prefab` 是你 `SampleScene` 裡 `BattleSystemPrefab` 的快照（打包清單見 TYN 的記錄）。`BattleStageController` 在進場時把旅程資料（HP／SAN、牌組、遺物、食物）灌進你的系統，戰鬥結束時讀回來，再 `ReportComplete`。

## 想加新畫面（例如大廳）

1. `StageType` 最後面加一個值
2. 做 Stage prefab，根物件掛一個繼承 `StageController` 的元件，**根物件不要帶 Canvas**（會掛在 `Canvas_Stage` 底下）
3. 在 EventScene 的 `StageHost.stages` 加一格
4. 在 `GameFlowManager` 決定什麼時候 `SwitchStage` 過去

不要用 `SceneManager.LoadScene` 換場景，`GameFlowManager`、HUD、地圖都在 EventScene 裡，換場景會把它們一起銷毀。

# 設計研究：岔路通往其他地區 ＆ 戰鬥跟著地區換背景與怪物

2026-10-18。研究結論與建議做法。**第一階段已實作**（見第五節），第四節是團隊的決定。

---

## 一、現況（查證過的）

### 地圖
- 一場 run 開始時 `GameFlowManager.StartNewRun` **生成一次地圖**（`MapGenerator.Generate(mapSettings, runSeed)`），整場 run 用同一份。
- 擺法是 **Terrain**（`MapGenerationSettings.layout = 2`）：讀一張地形圖放節點，水域不放、海岸邊加權。
- **所有路最後都匯進唯一一個首領節點**（`MapGenerator`「Boss 層只有一個，倒數第二層全部連過去」）。
- 打完首領 = `MapData.IsFinalLayer` → `RunFinished`，整場 run 結束。
- 節點種類 `MapNodeKind` 有 6 種（Event/Combat/Boss/Shop/SpecialEvent/Dialogue）。⚠️ 存檔存整數，**只能往後加**。
- **資料模型裡還沒有「地區」**。商店的地區貨表是借 `RunNodeData.contentId`（例如 `village`）湊的，程式註解寫明是暫時的接縫。

### 戰鬥（Romtyui）
- **怪物組合**：`EnemyFormationSpawner.encounterPool`（一個 `EnemyEncounterPoolData` 資產）決定。欄位是 public。
  我方另有一層：`EncounterPlanner` 在地圖生成時替每個戰鬥節點排好 `enemyId`，進戰鬥時由 `BattleStageController.ReserveEnemies` 預約給他。
- **背景**：`BattleContentRoot/SceneCanvas/SceneRoot/Background` 上一張固定的圖（漁村室外1），prefab 裡寫死。
- 戰鬥是我們打包的 `Stage_Battle_v3`，`BattleStageController`（我方）在戰鬥開始**之前**就有機會改東西（`OnStageEnter` → `BindToHostScene` → 才打開 BattleManager）。

---

## 二、建議做法

### 1. 地區資料 `RegionData`（新，我方，ScriptableObject）
一個地區一份，把現在散在各處的「這一區用什麼」收在一起：

| 欄位 | 用途 |
|---|---|
| `regionId` / `displayName` | 識別、進場標題（「III - IV 區域名稱」那塊） |
| `mapSettings` | 這一區的地圖生成設定（地形圖、層數、節點機率） |
| `groundTexture` | 地圖底圖 |
| `encounterPool` | 我方的敵人安排（每個戰鬥節點打誰） |
| `shopStock` | 商店貨表（取代現在借 contentId 的 regionalStock） |
| `battleBackground` | 戰鬥背景圖 → 交給 Romtyui |
| `battleEncounterPool` | 他的 `EnemyEncounterPoolData` → 交給 Romtyui |
| `exits` | 這一區的岔路能通往哪些地區（加權清單） |

`RunContext` 加 `currentRegionId`（＋去過哪些區，給紀錄用）。

### 2. 地圖：新節點種類「通道」`Gate`
- `MapNodeKind.Gate = 6`（**往後加**），`RunNodeData` 加 `targetRegionId`。
- 生成時：倒數第二層的部分節點**不連首領**，改成連到一個 Gate（終點）。
  - 保證：**至少一條路一定通到首領**（不然整區沒有首領可打）。
  - Gate 數量、通往哪一區由 `RegionData.exits` 決定，用 run 的種子抽 → 同一場 run 重進結果一樣。
- 地圖上 Gate 要一張新圖示（美術）。

### 3. 走到 Gate：穿越到別的地區
`GameFlowManager.TravelToRegion(regionId)`：
1. 黑幕 → 換 `currentRegionId`
2. 用新地區的 `mapSettings` **重新生成一張地圖**（種子錯開，例如 `runSeed ^ 地區序號`）、`EncounterPlanner` 用新地區的池重排敵人
3. HP／SAN／牌組／背包**全部保留**（那是 run 的，不是地圖的）
4. 地區標題橫幅 → 地圖下拉、播開場縱覽

不需要新的 Stage —— Gate 本身沒有內容，進去就是轉場。之後要「抵達時先播一段對話」也只是在第 3 步前插一段。

### 4. 通知戰鬥換背景與怪物（跟 Romtyui 的接口）
進戰鬥時 `BattleStageController`（我方）讀 `run.currentRegionId` → 拿到 `RegionData` → 在打開 BattleManager **之前**套上去。

兩種接法：

- **A. 我們直接改他的欄位**：`spawner.encounterPool = region.battleEncounterPool`、找到背景那張 Image 換圖。
  - 優點：他什麼都不用做。
  - 缺點：背景要靠路徑找（`SceneCanvas/SceneRoot/Background`），他改階層就斷，而且是**安靜地斷**。
- **B.（建議）請他在 BattleSystemPrefab 上放一個小接口元件**，例如：
  ```csharp
  // Romtyui 的檔
  public class BattleRegionHook : MonoBehaviour {
      public Image background;                 // 他自己指
      public EnemyFormationSpawner spawner;    // 他自己指
      public void Apply(Sprite bg, EnemyEncounterPoolData pool) { ... }
  }
  ```
  我們只呼叫 `Apply()`。欄位由他維護，他改階層也只要重指，不會靜默壞掉。
  之後他要加「地區專屬的燈光／音樂／故障強度」也都塞在這一支，我們這邊不用動。

⚠️ 兩層敵人安排要對齊：我方 `EncounterPool`（排 enemyId）和他的 `EnemyEncounterPoolData`（抽組合）**都要換成同一區的**。只換一邊的話，可能出現「漁村的節點排到深海的怪」。

### 5. 順手要改的
- 商店的地區貨表改讀 `currentRegionId`（拿掉借 contentId 的暫時做法）。
- 遊玩紀錄（RunRecorder）記下穿越了哪幾區。

---

## 三、分階段
1. **資料與穿越**（我方）：`RegionData`、`Gate` 節點、生成規則、`TravelToRegion`。先用兩個測試地區（都借漁村的圖），驗證「走到 Gate → 換一張地圖、狀態保留」。
2. **戰鬥接口**（我方＋Romtyui）：接法 B 的接口元件、兩層敵人池對齊。
3. **美術**：Gate 圖示、各地區的地形圖／底圖／戰鬥背景、穿越演出。

---

## 四、團隊的決定（2026-10-18）
1. **打完首領 = 結束整場 run**（未來可能改成只結束這一區）。走通道 = 跳過這一區的首領，進入下一區的**對應位置**（不是直接到那一區的首領）。
2. **地區之間怎麼連：先有準備，細節再討論。** 我方建議用「物理位置不變」的想法（見下）。
3. **不能回頭。**
4. **走進通道：播一段系統提示**（名字框是系統符號 ※），告知通道還沒完成。
5. **戰鬥接口用元件（接法 B），改 prefab。**

### 「物理位置不變」怎麼落實
- 每一區有一個世界格子座標 `RegionData.worldCell`（x 往東、y 往北）。漁村 (0,0)、B 區 (-1,0)、C 區 (1,0)。
- 通道出現在**面向目標的那一邊**：B 在西 → 通道在漁村地圖的西緣；C 在東 → 東緣；目標在北 → 最深幾層。方向由座標差算，不用另外填。
- 穿過去落在**反方向那一緣、差不多同一個南北位置**：往西走 → 出現在 B 地圖的東緣。不會落在首領或首領前一層。
- 正南方的地區不放通道（不能回頭）。

---

## 五、已實作（第一階段）
| 項目 | 位置 |
|---|---|
| 地區資料 | `Core/RegionData.cs`；資產在 `Core/Regions/`（Region_Village、Region_B、Region_C） |
| 通道節點 | `MapNodeKind.Gate = 6`、`RunNodeData.targetRegionId` |
| 放通道／落點 | `Core/MapGeneratorGates.cs`（`PlaceGates`、`FindArrival`） |
| 流程 | `GameFlowManager`：`startingRegion`、`Region()`、`CurrentRegion`、`GateRoutine`、`TravelRoutine`、`gateUnfinishedLines` |
| run 狀態 | `RunContext.currentRegionId`、`visitedRegionIds` |
| 地圖顯示 | `MapView3D.spriteGate`（還沒有圖 → 借事件圖染 `gateTint` 藍）、換區時換底圖 `groundTexture` |
| 戰鬥接口 | `Stages/BattleRegionHook.cs`，掛在 `Stage_Battle_v3` 根物件，`BattleStageController.ApplyRegion()` 在預約敵人前呼叫 |
| 商店 | 地區有填 `shopStock` 就用它，沒填照舊 |

規則重點：
- 通道是把一個中段（35%~80% 層）的**探索節點**改類型，**連線不動** —— 每條路照樣通首領；通道沒做好時播完提示可以從它繼續往下走，不會卡死。
- `RegionData.travelReady = false`（B、C 目前都是）→ 只播提示。改成 true 就真的穿越：重新生成新地區的地圖（沒填的欄位借漁村的），HP／SAN／牌組／背包保留，黑幕打出地區名。
- 漁村的兩條出口機率目前都是 1（每場都有兩個通道），在 `Region_Village` 的 exits 調。

實測（Play Mode）：200 個種子平均每張圖 1.98 個通道，B 全在西緣、C 全在東緣、首領 200/200 都在；走進 B 通道播兩句 ※ 提示 → 回地圖、可前往下一站；把 C 暫時設成 ready → 換了一張 38 節點的新地圖、落在西緣第 6/8 層；在 C 區進戰鬥，接口換掉了背景。

還沒做：通道的圖示（美術）、各區的地形圖／底圖／戰鬥背景／怪物池、遊玩紀錄記穿越、存檔續玩時的地區。

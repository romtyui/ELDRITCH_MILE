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

---

## 六、接上 Romtyui 的怪物池（2026-10-18 第二輪，依《怪物組合.xlsx》）
- **港口 = 起始地區**（`Region_Harbor`，原本的 Region_Village 改名，guid 不變）→ 怪物池 `深淵1`。現有的怪（半魚人、祭司、海膽、珊瑚礁、貪吃鬼、海坊主）都在表的港口那一欄。
- **漁村**（`Region_Village`）→ `深淵2`。表上的漁村怪（磯撫、鬼切、吞舟…）還沒做，池子目前是測試內容。**還沒接到任何通道上**（放在 GameFlowManager.extraRegions），位置要再定。
- **權重規則**（數字在 RegionData，可每區不同）：這一區前 3 場一般戰 = 1、第 4 場起 = 2、菁英 = 3、首領 = 5。抽 **weight 剛好相等** 的組合，這場 run 出過的先避開；那個 weight 沒有組合就往下找最接近的，再沒有往上。
  - 實作在 `BattleRegionHook.ReserveFromPool`：抽到後預約給 spawner（他本來就先用預約的），所以**他的抽怪程式沒改**。
  - 沒用他的「模式二」：那是「≤ 上限」，菁英戰會抽到前期雜魚組；而且靠 isBoss 分一般／首領。
- **地圖只排級別**：地區有怪物池時，`EncounterPlanner` 只決定這站是一般／菁英／首領（地圖要顯示），不排 enemyId。保證出現的半魚人祭司（《螺湮的祝福》要用）照舊指定。
- **菁英不太早出現**：`EncounterPool.eliteMinLayer = 3` —— 第 0~2 層不排菁英（保證項也挪到第 3 層以後）。200 張圖實測前三層菁英 0 個。
- **背景分一般／首領**：`RegionData.battleBackground`、`bossBattleBackground`（沒填首領的就用一般的）。
- 戰場計數 `RunContext.regionBattleCount`：進戰鬥就 +1，換區歸零。

### 2026-10-19 怪物與池子盤點、重新打包 v4
**待確認（先保留 Romtyui 的設定）**：兩個池子的 `isBoss` 幾乎每組都打勾（深淵1 只有 Paguroidea、深淵2 只有 Bosss 2 沒勾）。
我們的挑選只看 weight，不受影響；但他的 spawner 用 isBoss 決定要不要顯示**首領的大血條**，所以目前一般戰也會出現大血條。等他確認意圖再改。

怪物（EnemyData）8 隻，視覺 prefab（一般／黑化）都有指：雜魚、祭司、珊瑚寄居蟹、貪吃鬼（tuā-khoo-tai）、Boss（無臉人魚）、**新：海膽、猥裸（nude clam）、海坊主（Umibozu）**。
- 新的 3 隻原本**不在 EnemyDatabase** → 已補進去（`漁村/data/New Enemy Database.asset`）。少了的話，指定對手（事件、保證項）與他的中斷續戰都找不到牠們。

港口（深淵1）對照表：
| 表 | 池子 |
|---|---|
| 前三場 1 半魚人x2 | 雜魚2 w1 ✓ |
| 前三場 2 怪海膽x3 | 海膽3 w1 ✓ |
| 前三場 3 半魚人x1、怪海膽x2 | 海膽2雜魚1 w1 ✓ |
| 前三場 4 人手x2 | **沒有怪**（人手未做） |
| 前三場 5 半魚人x1、怪海星x1 | **沒有怪**（怪海星未做） |
| 第四場起 1 半魚人x3 | 雜魚3 w2 ✓ |
| 第四場起 2 祭司x1、半魚人x2 | 雜魚2祭司1 w2 ✓ |
| 第四場起 3 祭司x1、人手x2 | **沒有怪**（人手未做） |
| 第四場起 4 祭司x1、怪海膽x2 | 海膽2祭司1 w2 ✓ **（我們改的：原本放的是重複的海膽2雜魚1，海膽2祭司1 有做但沒進池）** |
| 第四場起 5 珊瑚礁x1 | Paguroidea w2 ✓ |
| 菁英 1 祭司x1、珊瑚礁x1 | 祭司1蟹1 w3 ✓ |
| 菁英 2 祭司x2、半魚人x1 | 雜魚1祭司2 w3 ✓ |
| 菁英 3 猥裸x1 | nude clam w3 ✓ |
| 菁英 4 貪吃鬼x1 | tuā-khoo-tai w3 ✓ |
| 首領 海坊主x1 | Bosss 2 w5 ✓ |

漁村（深淵2）：表上的怪（磯撫、怪海膽群、鬼切、吞舟、針千本、八百比丘尼）都還沒做，池子目前是港口的組合擺不同 weight，**沒有 w2、沒有 w5**（缺的會往下找最接近的）。照「傳多少做多少」先不動。
沒進任何池的組合：`test 1`（測試用）。

**戰鬥重新打包成 `Stage_Battle_v4`**（從 10/19 的 SampleScene，945 物件、遺失腳本 0），處理跟 v3 一樣：拿掉重複系統與齒輪、BattleManager 關著、watcher 停用、固定像素畫布改 1920x1080 等比、掛 BattleRegionHook。StageHost 改指 v4，v3 留著（退回只要改那一格）。
卡冊副本 `UI/System/DeckBook.prefab` 跟著換成 v4 的版本（他的卡冊有改）；祭壇的獎勵畫面副本比對過沒變，不用動。
實測：首領戰抽到海坊主、指定猥裸也打得起來，他新的格擋跳字（popupVariants）在，0 錯誤。

### 2026-10-19（早）Romtyui 更新 → Stage_Battle_v5
- **他把池子的首領勾全部拿掉**（深淵1 連海坊主也沒勾 → 首領戰不會有首領的大血條，待他確認）。
- **深淵1 第四場起的「海膽2祭司1」被他換回「海膽2雜魚1」**（重複）。照他的版本，不再改。
- 新怪：怪海星（starfish）有資料與外觀，**已補進 EnemyDatabase**；但還沒有任何組合用到牠（表上「半魚人x1、怪海星x1」）。
  人手、二口、磯撫、針千本、鬼切 只有外觀 prefab，還沒有 EnemyData。
- 山羊神牌（goat_god）接上新的出場動畫（Goat.asset / Goatg group.prefab）。
- **他的 SampleScene 把 `BattleUICanvas/Inventory` 整組關掉了**（抽牌袋、結束回合燈、HP/SAN 都在裡面），沒有程式會再打開。
  v5 打包時**保持開著**，否則戰鬥裡沒有結束回合鍵。請他確認是不是忘了打開。
- v5 其餘處理同 v4；StageHost 改指 v5，v4 留著。卡冊與獎勵畫面這次沒動，副本不用重拷。

### 2026-10-19 通道位置的硬條件（試玩：可選的兩站都是通道）
舊規則只挑「中段層數裡最靠邊的探索房」。500 張圖實測：332 張有「下一步只能走通道」的節點、56 張通道落在錯的那半邊、58 張兩個通道距離不到地圖寬的 40%、7 張同一站可以同時選到兩個通道。
新規則（`MapGenerator.PlaceGates`），三條全部要成立，否則這一場不放那條通道：
1. 落在那一側最外面 30% 的帶子裡（`GateEdgeBand`）—— 西的在西、東的在東，兩個通道自然隔開
2. 不跟別的通道互連、不共用父節點 —— 同一站不會同時看到兩個通道
3. 每一個通往它的節點都還有一條不是通道的路 —— 不會「只能走通道」
候選：探索房優先，不夠才用一般戰（商店、對話、特殊不動）。
同樣 500 張圖：以上五種情況全部 0；421 張有兩個通道、73 張一個、6 張沒有；通道約 1/3 是由一般戰改來的；首領 500/500 都在。

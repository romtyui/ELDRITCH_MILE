# 8/29 交付進度

> 建立：2026-08-27 · 這一份是**這一輪衝刺的現況與待辦**。
> 全景看 [SystemsStatus.md](SystemsStatus.md)，日常推進看 [Next.md](Next.md)。

---

## 這一輪要做的四件事

| | 狀態 |
|---|---|
| ① 場景美術嵌入 ＋ 家具隨機 | ✅ 完成 |
| ② 遺物／食物快捷欄 | ✅ **只做顯示**（刻意） |
| ③ 怪物難度分級 | ✅ 完成 |
| ④ 對話打牌（Romtyui 規格書） | ✅ 邏輯＋UI＋接進流程 |

---

## ① 場景美術嵌入

美術的 `Assets/rafterYi/altar.unity` 有三組背景，抽成共用的
`Assets/TYN/Rooms/Art/`：`Art_Village_MiddleRoom` / `Art_Altar` / `Art_Village_Outdoor`。

三個原本會出事的地方，都處理了：

- **縮放**：美術是 38.4×21.6 units，相機可見高度只有 10 —— 不處理會爆框 2 倍。
  兩者都是 16:9，所以 Art 根縮 0.5 → 19.2×10.8，**跟原本佔位圖完全一致**，
  既有 slot 座標不用重調。縮放放根物件、不改 PPU，美術更新可以整組換掉。
- **排序**：美術道具用了 `sortingOrder` 1~3，互動物件是 0，而 sortingOrder
  優先於 z —— 寶箱會被道具蓋住。Art 根加 `SortingGroup(-100)`。
- **`Stage_SpecialEvent` 原本掛在 Canvas 底下**：它以前沒有 SpriteRenderer 所以沒事，
  現在有了會被 CanvasScaler 縮到爆。已改掛 `WorldRoot`。

### 家具隨機（`SceneDressing`）

每件家具各自有出現機率（預設 0.65），**在地圖生成時就決定並存進節點**
（`RunNodeData.dressingSeed`）。兩個理由，缺一不可：

1. **同一個節點重進要長一樣** —— 現場擲的話玩家離開再回來整間房就變了
2. **探索與戰鬥讀同一個種子** → 組員要的「從探索到戰鬥背後是同一個地方」

`SceneDressing` 有右鍵選單「自動收集子物件」，美術更新後跑一次就好。

**⛔ 還沒做**：戰鬥要真的顯示那組美術，需要有人把 `Art_*` 放進 `Stage_Battle`
的背景層。資料通路接好了，但那會蓋掉 Romtyui 的 `SceneCanvas`，是視覺決定。

---

## ② 快捷欄

`Canvas_HUD`（sortingOrder 400）右側兩條：遺物（Curio）在上、食物（Food）在下。
收合成一個圖示，**滑鼠移到畫面右側 16% 才淡入**（`EdgeRevealUI`），
hover 單格會往外推並顯示說明。

### ⚠️ 只做顯示是刻意的

食物要能注射需要「使用道具」這個動作 —— **那個動作不存在**，
回多少 HP／扣多少 SAN 也沒有數值。硬做只會做出一個按了會亂跳的假功能。

`ShortcutBarUI.OnItemUsed` 留著當接口，等「使用道具」做好接上就會動，UI 不用重做。
在那之前點下去會印一行 Log 說明「功能還沒接」。

### 踩過的三個坑（都修了）

- `Refresh()` 只在 `OnEnable` 呼叫 → 場景載入時背包是空的 → **永遠 0 個格子**。
  改成展開前重讀。
- `Canvas_HUD` 原本 300，**跟 `[MAP_OVERLAY]` 撞號**。地圖底下有全寬的
  `blockraycast_*` 攔截層，同分靠階層順序太脆弱。改 400（仍低於 Popup 500）。
- 兩條欄的框重疊，重疊區的 hover 會被其中一條吃掉。已排開。

---

## ③ 怪物難度分級

`EncounterPool.Tier`：`Minion` / `Elite` / `Boss`。地圖 tooltip 看得到，
**排在說明之前** ——「這站硬不硬」是玩家在分岔口最想知道的事。

**分級掛在我方的池子、不掛 `EnemyData`** —— 同一隻怪在不同區可以是不同級別，
而且 `EnemyData` 在 `Assets/Romtyui/`，難度是關卡設計不是戰鬥資料。

⚠️ **一般雜魚刻意不標**。每站都掛「普通」的話「菁英」就淹沒了。

驗證：300 張隨機地圖，每張固定 1 個菁英、Boss 標對 300/300。

---

## ④ 對話打牌（機率卡牌）

Romtyui 規格書《對話機率卡牌功能》。**與現有的探索打牌是兩套，刻意並存。**

| | 探索打牌（已驗收） | 機率對話（新） |
|---|---|---|
| 卡打在哪 | 目標上，出牌即判定 | **選項**上，只改機率 |
| 判定時機 | 每出一張牌 | 出完牌後才選一個選項 |
| 卡牌關係 | 屬性相剋決定倍率 | colorId 命中就 +value |
| 衰減 | 逐次衰減 | 沒有 |
| 失敗 | 手牌用盡才結案 | 移掉那個選項，其餘繼續 |

**改現有的 `DialogueEncounterController` 會連帶弄壞探索打牌**（兩者共用），
所以另開 `StageType.ProbabilityDialogue`，舊的 `Dialogue` 原封不動。
萬一新的來不及，舊的還能跑 —— 這是保命繩。

### 分層

- `Core/ProbabilityDialogue/` —— 資料 ＋ 規則引擎（**純 C#，不碰 UI**）
- `UI/ProbabilityDialogue/` —— 回答／卡牌／View（**只接結果，不做判斷**）
- `Stages/ProbabilityDialogueStageController` —— 很薄，只管進場與結束

規則引擎不是 MonoBehaviour，所以**規格書的 T01~T16 可以完全離線跑**，
不用開場景擺 UI。目前 **16/16 全過**。

### 範例資料

`PDialogue_Gatekeeper`（坎貝爾守門），三個回答其中一個是雙色（橘＋藍），
正好驗規格 §3.1 的多色回答與兩個色點。六張卡（橘／藍 各 10/25/40）。

---

## 怎麼測：F1 除錯面板

按 **F1** 開面板，最上面有一排「直接跳到」按鈕，可以跳過地圖直接進任何 Stage。

⚠️ 那個跳轉用 `[Conditional("UNITY_EDITOR")]` 包起來，**正式包裡整個方法
連同呼叫端都會消失**，不是後門。跳過去的 Stage 結束後照常回報完成。

---

## 這一輪修掉的 bug（都有成因，不只是「修好了」）

| 症狀 | 成因 |
|---|---|
| 開容器只發得出 1 張手牌 | `SyncDeckFromRun` 用「exploreDeck 是空的」判斷第一次，但特殊事件會**先**加一張神牌進去 → 種子邏輯被跳過 → 15 張起始牌組被那 1 張取代 |
| 戰鬥第一回合抽 10 張 | `BattleManager.Start()` 自己會呼叫 `StartBattle()`，我方又呼叫一次 → 開了兩場 |
| 成功時直接跳回地圖 | View 沒有畫 `successText`（失敗有、成功沒有） |
| 手牌用完再點容器會卡死 | `BeginEncounter` 先鎖 `HoldOpen`、先生近照，**之後**才檢查有沒有牌 |
| 手牌打完時角色立繪冒出來 | `ClearTargetViews()` 無條件開 `portraitImage`，但 `Destroy` 是延遲的 |
| 快捷欄點不到 | 見 ② 的三個坑 |
| 執行時洗版 InvalidOperationException | 用到舊版 `UnityEngine.Input`（專案已切 Input System） |

> 💡 最後一項值得記住：舊 Input API **編譯期完全看不出來**，仍然存在、仍然編得過。
> `RunDebugPanel` 開頭有這個坑的註解，全專案要一致。

---

## ⛔ 交付前一定要做的

1. **`useDemoRoute` 要關掉** —— 現在開著是為了測戰鬥，不關組員看到的還是一條直線
2. **`demoRouteKinds` 插的戰鬥節點**要還原
3. **五間房的 slot 對位** —— 座標是照透視推的，還沒對著實際背景調。
   中屋有那張「（擺位參考草圖・刻意停用）」可以打開參考
4. **兩套新 UI 的樣式** —— 機率對話與快捷欄都還是灰底方塊

## 卡在別人身上的

- **戰鬥 Console 的 block/special 錯誤** —— 已代修（見 Next.md），但要他確認
- **`EnemyData.enemyId`** —— 已代填五個，要他確認命名
- **死亡選單歸屬** —— 決定是我方，但還沒做
- **`chest_Document`「固定出現」** —— 已排除在寶箱配額外，但還沒決定出現在哪

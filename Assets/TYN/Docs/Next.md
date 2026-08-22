# 接下來做什麼

> 更新：2026-08-22 · 這份是**短的**。全景看 [SystemsStatus.md](SystemsStatus.md)，慣例與踩坑看 [HANDOFF.md](HANDOFF.md)

---

## 現在的狀態

一條龍已經通了：**主選單 → 地圖 → （可能插播事件）→ 探索／對話／商店／特殊事件／戰鬥 → 回地圖**。

**戰鬥自 2026-08-22 起也在這條線上了**（`Stage_Battle` prefab，見〈卡在別人身上的〉）。

| 系統 | 狀態 |
|---|---|
| 打牌判定（探索＋對話共用） | ✅ 已驗收 |
| 商店（八格貨架、金幣、依區域換貨） | ✅ 已驗收 |
| 對話（選項＝判定目標、關鍵字上色、結果動效） | ✅ 已驗收 |
| 對話氣泡 bark（招呼／閒聊／成敗／結語） | ✅ 已驗收 |
| 戰利品表（商店與寶箱共用） | ✅ 離線驗證 |
| 條件層（旗標／侵蝕度／六種條件） | ✅ 離線驗證 |
| 事件系統（資料 → 庫 → Stage → 流程掛鉤） | ✅ 已驗收 |
| 角色池（區域標籤查詢＋條件指名） | ✅ 離線驗證，**已掛上對話節點** |
| 漁村小屋／中屋／大屋 ＋ 五種容器 | ✅ 已實機玩過（2026-08-22），修掉兩個問題見下 |
| HP／SAN 轉接頭 `PlayerVitals` | 🔶 程式好了、`RunStateManager` 也進 EventScene 了，**只差 `startingMaxHp` 的數值** |
| 戰鬥（`Stage_Battle`） | 🔶 打得起來，但 `EnemyData.enemyId` 全空 → **指定不了對手** |

### 實機玩過之後修掉的兩個問題（2026-08-22）

- **手牌用完再點容器會卡死** —— `BeginEncounter()` 先把對話框鎖成 `HoldOpen`、
  先生出寶箱近照，**之後**才檢查有沒有牌；沒牌就 return，畫面留下一個關不掉的空框。
  檢查已經搬到動對話框之前，並新增 `Out Of Cards Text` 告訴玩家原因。
- **手牌打完的瞬間，角色立繪出現在寶箱近照後面** —— `ClearTargetViews()` 無條件把
  `portraitImage.enabled` 打開，但同一個函式裡的 `Destroy` 要到該幀結束才生效。
  改成只有 `HoldPortrait` 時才還原，否則連 sprite 一起清掉。

> 💡 兩個都是**收尾那一半沒有跟開場對稱**。`SpawnTargetView` 特地關掉立繪、
> `ClearTargetViews` 卻無條件開回來；開場鎖了 `HoldOpen`、失敗路徑卻沒解鎖。
> 之後寫這種「開場動 UI」的流程，記得先問「早退的那條路誰負責還原」。

---

## 給文案組員的表

[WritingTemplate.md](WritingTemplate.md) —— **空白表格＋逐欄註解**，他在文件／試算表裡填，
我們負責搬進 Unity。裡面兩個附錄是重點：**獎懲能寫什麼**（分成「現在就生效」
「先寫著之後才動」「現在寫不了」）與**觸發條件能寫什麼**。

## ~~要決定的事~~ ✅ 已回答（2026-08-21）

**那 10 件 `curio_*` 收藏品（無聲的小鈴、褪色的面具…）＝ 刻意先留著的，不是舊稿。**

所以它們現在完全不會出現在遊戲裡 —— 沒登記進 `ItemDatabase`、也沒有稀有度標籤，
商店的收藏品池撈不到。**這是預期狀態，不用修。**
哪天要啟用再登記進去、各補一個 `Common` / `Uncommon` / `Rare` 就好。

## 你手上的待辦（都是編輯器裡的擺位，不用改程式）

| 事情 | 在哪 |
|---|---|
| 事件結束鍵的位置 | `Stage_Event.prefab / EndButton`（目前左下角，不直覺） |
| 房間 slot 對位 | `Room_Village_Small / _Medium / _Large`，大屋第五格是我隨便補的 |
| 商店背景圖 | 圖還沒進專案；8 個 `Slot_*` 是手動擺位，等圖來對齊層板 |
| ~~地圖 tooltip 面板~~ | ✅ 擺位已處理，hover 有回饋了（2026-08-21） |

### 地圖 hover 的回饋改成顏色（2026-08-21）

原本 hover 是**放大 1.1 倍**，跟「當前位置 1.2 / 可前往 1.0 / 去不了 0.8」撞同一個通道。
改成 hover 時**變鮮豔**，三個 `NodeUI_*` 的 `Scale Hover` 已全部調回 1。

- 內建的 UI tint 是乘法、節點平常又是純白，往上沒有空間，所以加了一支
  `MapNodeHighlight.shader`（＋ 同名 `.mat`，已指到三個 prefab 的 Image）
- 兩個旋鈕在 `MapNodeUI` 的 Inspector：**飽和度**（紅圈更紅）與**暖光量**（墨線也跟著亮）。
  水墨圖的黑筆觸飽和度是 0，只調飽和度對墨線沒作用，所以兩個要一起用
- **只有「可前往」的節點會亮** —— 亮起來在玩家眼裡等於可以點

---

## 下一步（建議順序）

### ~~1. 角色池~~ ✅ 做完了（2026-08-18）

`CharacterPool` ＋ `Chars_Dialogue_Village`，已掛上 `Stage_Dialogue`。
細節見 [SystemsStatus.md](SystemsStatus.md) §3.0.0c。

```
[指名 campbell]   權重 100   無條件
[標籤查詢 漁村]   權重  40   【深淵】侵蝕度 ≥ 50
```

侵蝕度 0 時只有第一條合格，所以**現在的行為跟掛上去之前一模一樣**（坎貝爾）。
要看到它換人，把第二條的門檻暫時調低（或在事件裡累積侵蝕度）。

> ⚠️ **時藏還不能真的上場** —— 他沒有立繪，也沒有成功／失敗／結語台詞。
> 池子挑到他的時候 Console 會發警告，不會靜靜地少東西。

### ~~2. 把剩下六個事件建成資產~~ ✅ 做完了（2026-08-18）

大綱〈事件〉章的八個事件**全部建好並登記**了。
新增了一種效果 `ConsumeItemByTag`（「消耗糧食」不指定是哪一種魚）。

| 標題 | 條件 | 狀態 |
|---|---|---|
| 暴食之深淵 | 無（權重 100，盡量第一個出現） | ✅ |
| 無人的小船 | 無 | ✅ |
| 海市蜃樓 | 深淵 ≥ 50% | ✅ |
| 損壞的祭壇 | 神牌 < 3 | 🔶 神牌效果沒接上 |
| 喂米可吃飯 | 武器牌 ≥ 20 | 🔶 銷毀武器牌沒接上 |
| 門扉 | 已進行 ≥ 800 秒 | 🔶 隨機傳送沒接上 |
| 好餓好餓的貪吃鬼 | 身上有糧食 | 🔶 選項 B 的戰鬥沒接上 |
| 螺湮的祝福 | 旗標「打倒半魚人祭司」 | ⛔ **沒人在立那個旗標** |

> ⚠️ **《螺湮的祝福》的選項 B 在大綱裡是空的** —— 要請文案補。
> ⚠️ 《喂米可吃飯》原文的「（可能獲得某些資源）」現在只有文字沒有實物 ——
> 缺一種「抽一張 `LootTable`」的效果。

### 3. 新手教學 —— ⚠️ **我先前寫錯了，這不是我們的事件**

之前這裡寫「照事件的形狀填即可」。**去看了才發現不對。**

大綱那一章不是一段敘述，是**一連串卡著玩家操作的引導**
（點地圖 → 關地圖 → 抽牌 → 使用卡牌 → 探索場地 → 調查怪物 → 進戰鬥 → 跳過回合 → 牌庫 → SAN → HP…）。
用事件做的話會變成「一直播文字，玩家什麼都不用做」，完全不是教學。

**而且 Romtyui 已經做好一整套了** —— `Assets/Romtyui/codes/UI/Tutorial/`，約 4300 行：
教學序列、步驟、訊號等待、反白遮罩（挖洞只讓某個按鈕能點）、進度存檔。

#### 真正缺的是：**我方沒有人發訊號**

他的 `TutorialSignals` 已經把整條教學需要的訊號都列好了，**包含探索與地圖那一段**。
但實際會發訊號的只有戰鬥那半邊（`BattleManager` / `CardDragUI`）——
因為另一半的觸發點在我方的檔案裡，**他碰不到**。

#### ✅ 已補上（2026-08-18）

新增 `Core/TutorialSignal.cs` 當轉接頭（跟 `PlayerVitals` 一樣的用意 ——
**不改他的檔案，只呼叫**，而且集中在一支，他哪天改名只要修這裡）：

| 訊號 | 我方的發送點 |
|---|---|
| `MapOpened` | `MapOverlayController.SlideDown()` 攤開且可點之後 |
| `MapClosed` | `MapOverlayController.SlideUp()` |
| `ExploreCardPlayed` | `DialogueEncounterController` 判定跑完（**成敗都算**） |
| `WeaponObtained` | `RunContext.AddItem()`，道具帶 `Weapon` 標籤時 |

> 沒在跑教學時呼叫是**完全安全的** —— 沒有人訂閱，訊號就散掉了。已驗證。

#### ⛔ 還發不出來的（要跟 Romtyui 對）

| 訊號 | 卡在哪 |
|---|---|
| `BookOpened` | 米可（魔法書）的 UI 還不存在 |
| `MonsterInvestigated` | 探索場上還沒有「怪物」這種調查目標 |
| `AltarOpened` / `GodCardObtained` / `PrayerDeclined` | 祭壇現在是我方的事件資產，要決定由誰發 |
| `AssistantEncountered` / `AssistantJoined` / `AssistantDeclined` | 隊伍（協助者）系統還不存在 |

**下週對接要問的**：教學序列資產由誰填？那三個祭壇訊號要不要我方從
`EventStageController` 發（可以在 `EventData` 加一個「開始時發哪個訊號」的欄位）？

### 4. ~~背包 UI~~ → 除錯面板 ✅ ＋ 快捷欄（等別人）

**背包 UI 被否決了** —— 組員要的是快捷欄，食物／收藏／卡片完全分開。合理，
玩家不需要開一個大背包翻東西。

但「開發時看不見身上有什麼」是另一回事，所以做了 `Core/RunDebugPanel.cs`：

- **IMGUI，按 F1 開合**，掛在 `[SYSTEM]` 上。不用拉 Canvas、不用對位
- `#if UNITY_EDITOR || DEVELOPMENT_BUILD` —— **正式包裡整個類別會消失**
- 分頁是「全部／糧食／收藏品／補給／其他」，**就是日後快捷欄要分的那幾類** ——
  先證明「依標籤過濾」這條路走得通，快捷欄接的是同一份資料
- 查不到的道具會標成「**沒登記**」，抓 id 打錯與忘記登記

#### ⚠️ 做快捷欄的人第一天就要知道：卡片不在背包裡

武器牌買下去是進 `RunStateManager.savedDeck`（戰鬥端持有），
跟 `RunContext.inventory` 是**兩份不同的東西**。所以「卡片」那一格查的是另一個來源。
面板刻意把兩者分開列，就是為了讓這件事一眼看得到。

#### 快捷欄還卡著

**食物快捷欄需要「使用道具」這個動作，那個還不存在**，
點下去要發生什麼（回多少 HP、扣多少 SAN）也還沒有數值。

---

## 卡在別人身上的

| 事情 | 狀況 |
|---|---|
| ~~起始戰鬥牌組~~ | ✅ 解了 —— `StartingDeck_Default`（8 張，照他 SampleScene 那副**去掉兩張神牌**），已掛上 `GameFlowManager` |
| ~~武器牌的 `CardData`~~ | ✅ 接了 —— 商店那 4 格 = 匕首／弩／矛／盾（`human_*`），價格仍是佔位 |
| **`startingMaxHp` 的數字** | 🔶 欄位通了，**只差一個數值**。填了戰鬥就會套用我方的血條而不是他的預設值，要跟他對 |
| ~~**`Stage_Battle` prefab**~~ | ✅ 2026-08-22 做好了，在 `Assets/TYN/Stages/`。已註冊進 StageHost（`customParent = WorldRoot`） |
| **`EnemyData.enemyId` 全是空的** | ⛔ 五個敵人資產都沒填 → `ReserveEncounterByEnemyData()` 整組跳過，**指定不了對手**。<br>建議：`boss` / `coral_paguroidea` / `fish_priest` / `tua_khoo_tai` / `minnow`。<br>⚠️ `fish_priest` 要跟《螺湮的祝福》的旗標 `killed_fish_priest` 對得上 |
| 死亡選單誰管 | 戰鬥失敗時他會開自己的死亡選單，那跟我方的輪迴結算是兩套。要確認「玩家按重來」之後歸誰 |
| 收藏品的效果 | Romtyui 說他會做，之後把效果資料放進大資料 |

### 戰鬥 Console 的兩組錯誤 —— 查過了，**不是我方造成的**（2026-08-22）

包成 prefab 之後 Console 會一直噴這兩組。**已經確認跟 prefab 化無關**，
但如果 Romtyui 請我方代修，資料都在這裡，不用重查。

#### ① `Parameter 'block' / 'special' does not exist`

`EnemyVisualAnimationController.PlayTriggerOnAnimators()` **無條件** reset 六個 trigger
（`idle` / `atk` / `hurt` / `death` / `block` / `special`），但各敵人的 AnimatorController
實際只有 4～5 個：

| 敵人 | controller 現有的參數 | 缺 |
|---|---|---|
| Boss | idle, atk, hurt, death, special | **block** |
| coral Paguroidea | atk, block, idle, hurt, death | **special** |
| Mermaid Priest | idle, atk, hurt, death, block | **special** |
| tuā-khoo-tai | idle, atk, hurt, death | **block、special** |
| 雜魚 | idle, atk, hurt, death, block | **special** |

**兩種修法**（要他決定走哪條，因為這是設計問題不是 bug）：
- 把缺的參數補進那幾個 controller —— 動的是他的美術資產
- reset 前先檢查參數存不存在 —— `EnemyUnit` 裡已經有現成的 `AnimatorHasTrigger()` 可以用

#### ② `Animator is not playing an AnimatorController`

三個 Animator **沒有指定 Controller**：

```
monsterCanvas/monster_Panel/MonsterPos_1/Image1/StatusRoot_01/血量UI (1)/BlockRoot_01/BlockAnimator_01
                            MonsterPos_2/Image2/StatusRoot_02/血量UI (1)/BlockRoot_02/BlockAnimator_02
                            MonsterPos_3/Image /StatusRoot_01/血量UI (1)/BlockRoot_03/BlockAnimator_03
```

`EnemyUnit.AnimatorHasTrigger()` 去讀 `animator.parameters` 時就會噴。

#### 為什麼確定不是我方弄的

- 這兩組都指向**同一個還沒做完的功能：格擋（block）**。物件建好了、controller 沒指定、參數沒補。
- 用 git 比對過 **`7b2bc6b`**（合併完 `damege_test`、但還沒重構 SampleScene 的那一版）——
  三個 `BlockAnimator_*` 當時就在，而且本來就掛在 `BattleContentRoot/monsterCanvas/…` 底下，
  那整棵樹從一開始就是 `BattleSystemPrefab` 的一部分。
- 我方重構只搬了六個物件：`Player`、`BattleDeck`、`MonsterLightReveal`、`WorldVisualRoot`、
  `ScreenShakeController`、`Freeform Light 2D` —— 沒有一個是它們，也不是它們的父物件。

> 💡 **這個驗法之後可以重複用。** 再遇到戰鬥端的錯誤、要判斷是不是我方 prefab 化弄出來的，
> 就拿 `7b2bc6b`（合併完、重構前）的版本比對。
> `git show 7b2bc6b:Assets/Romtyui/scene/SampleScene.unity | grep ...`

### 戰鬥接入 —— 形狀已經接好了（2026-08-19）

**已完成：**

- `Stages/BattleStageController.cs` —— 進場預約敵人 → `StartBattle()`；
  收到結束訊號 → 立 `killed_<enemyId>` 旗標 → `NotifyStageComplete()`
- `BattleManager.EndBattle()` **加了兩行**（經同意動了 `Assets/Romtyui/`）：
  勝利發 `TutorialSignals.BattleWon`、失敗發 `"BattleLost"`。
  ⚠️ 勝利那一行**放在 `SaveFromBattle()` 之後** —— 早一步發，我方會讀到上一場的 HP／SAN／牌組
- `TutorialSignal.BattleWon` / `.BattleLost` —— 我方對應的常數。
  ⚠️ `BattleLost` **兩邊都是字面值**，改字串要一起改

**為什麼不輪詢**：勝利時他會 `SetActive(false)` 自己，但**失敗時物件還開著**（他去開死亡選單），
所以輪詢只抓得到一半。

**還跑不完整**，卡在上表那兩個 ⛔。

---

## 正式配內容前要還原的測試腳手架

- `EventLibrary.globalChance` = **1**（每站必檢查事件）→ 大綱寫的是「有概率觸發」
- `MapGenerationSettings.Use Demo Route` 開著 → 固定直線路線。
  `demoRouteKinds` 已插入一個戰鬥節點（為了測 `Stage_Battle`），正式配內容前要改回來
- `RoomContent_Village` 裡 `chest_RequiresKey` / `chest_Document` 權重 = 0
- `RoomLibrary` 的 `Room_Village_01` 權重 = 0（舊測試房，確認新的沒問題後可封存）
- 商人的彩蛋條件 `campbell_absent` 是**手填**的（隊伍系統還不存在）
- 收藏品／武器牌／一般糧食的**品名與價格都是佔位**
- `Chars_Dialogue_Village` 的漁村那一條門檻是 **深淵 ≥ 50** ——
  想在測試裡看到它換人就暫時調低，`verbose` 打開會印出每一條被跳過的原因

---

## 版控備註

`.mcp.json` 被 `.gitignore` 忽略，**不會跟著進版控** ——
換機或清工作區之後要重建，內容見記憶或 [HANDOFF.md](HANDOFF.md) §5。

`Library/` 裡的 TMP 動態字型圖集（`LiberationSans SDF - Fallback.asset`）會在畫到
新的中文字時被改寫，常常無故出現在 `git status` 裡。那是快取，缺的字 Unity 會自己補，
**看到它變動直接 `git checkout` 還原就好**，不用進 commit。

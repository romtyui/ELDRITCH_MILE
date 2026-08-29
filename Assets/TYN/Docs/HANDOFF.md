# 交接：新對話從這裡接上

> 更新 2026-08-29（第三輪）。**新對話第一句就說「讀 Assets/TYN/Docs/Handoff.md」。**
> 這一份是「現在在哪、下一步做什麼」。
> 附件重做的細節看 [ItemsFromSpec.md](ItemsFromSpec.md)；
> 機率對話的觀察與實驗看 [DialogueProbability.md](DialogueProbability.md)；
> 四大工作流的來龍去脈看 [Sprint_0829.md](Sprint_0829.md)，全景看 [SystemsStatus.md](SystemsStatus.md)。

---

## 第三輪（2026-08-29，換帳號之後）做完的

| 做了什麼 | 在哪 |
|---|---|
| 遺物分「貨架彩色／持有白色」兩張圖，四件接上美術 | `ItemData.shelfIcon`、`ShopSlotUI` |
| 地圖下拉時墊戶外背景（不再看到天空底色） | `MapOverlayController.backdrop` |
| 菁英戰的節點有自己的圖示 | `NodeUI_Elite.prefab`、`MapView.eliteNodePrefab` |
| 對話的高亮改成**照屬性上色**，雙屬性回答亮混色 | `ProbabilityAnswerUI`、`ProbabilityDialogueView` |
| 對話結束有「（獲得 XXX）」的結算 | `ProbabilityDialogueSession.LastOutcomeNotes` |
| 對話有自己的**離開鍵**，話全講完才出現 | `ProbabilityDialogueStageController.endButton` |
| 地圖背景可調暗（只套在地圖上） | `StageBackdrop.tint` |
| 快捷欄 Menu 不出現、感應區 307px → 67px | `UIPanel`、`EdgeRevealUI.triggerZone` |
| 事件盤點：關掉三個效果沒接的、暴食權重歸位 | `EventLibrary.asset` |
| 地圖多一種**有分支**的固定路線，首排固定神牌 | `DemoRouteShape.Branching`、`firstLayerKind` |
| 生成完會檢查有沒有走不到／出不來的節點 | `MapGenerator.WarnIfUnreachable` |
| 貪吃鬼那場戰鬥打到祭司 → 改成 `tua_khoo_tai` | `Event_hungry_glutton` |
| 中段節點改隨機，補上對話節點與「一定會有」的保證 | `dialogueChance`、`guaranteedKinds` |
| F1 面板加「指定對手開打」，Boss 終於驗得到 | `RunDebugPanel.quickBattleEnemyIds` |
| 戰鬥的 `Global Light 2D` 補回 `Stage_Battle.prefab` | 併場景時漏掉的那顆黑色 Multiply 光 |
| 後製兩顆 Global Volume 補回 ＋ 相機打開 `renderPostProcessing` | 「渲染」的大宗，上一輪只補了燈 |
| 「指定對手」改走 Formation，之前寫的那條沒人讀 | `BattleStageController.ReserveFormationFor` |
| 戰鬥道具面板的接口補上（效果資產還缺） | `ItemData.battleItemEffect`、`BindItemsFromInventory` |
| 背景自動對齊相機，祭壇不再露出上緣底色 | `Core/BackdropFit.cs`、`StageBackdrop.fitToCamera` |
| 地圖改成畫出**所有**連線，走之前就看得到路網 | `MapView.lineDisplay = AllConnections` |
| 六個背景的擺法統一（地圖／五間房／事件／對話） | 全部 `BackdropFit`，落在同一個範圍 |
| 連線長度改成照兩點距離算，不再寫死 | `lineThickness` / `lineEndGap` / `lineMinLength` |
| 神牌的結算移到收尾之後（離開前的最後一頁） | `outroTailLines`、`settlementLast` |
| 事件有優先序，暴食之深淵一定先出 | `EventData.priority` |
| 轉場加上「黑幕停一拍」＋ 非對稱淡入淡出 | `holdBlackSeconds` / `holdBlackAfterEventSeconds` |
| 轉場地點卡（沿用開地圖那支 banner） | `[SYSTEM]/StageTitleBanner`、`nodeTitles` |
| 神牌動畫的 Canvas 補上 Scale With Screen Size | `AnimCanvas` 是全專案唯一漏掉的一個 |
| Build 設定：事件密度 1 → 0.35、關掉 verbose | 開場那一站另外保證有事件 |
| 判定的「成功／失敗」動效補回來（變暗之前） | `ProbabilityAnswerUI.PlayResultFlash` |
| 容器會給金幣，空箱保底；商店加金幣圖示 | `ChestInteractable.moneyMin/Max`、`MoneyIcon` |
| 打完 Boss 演一段結局，然後回主選單 | `runFinishedEvent`、`Event_run_finished` |
| 戰後照 enemyTier 給金幣（10~20 / 30~50 / 80~120） | `BattleStageController.tierRewards` |
| 出牌會往畫面中央射出去再淡掉 | `ProbabilityCardUI.playLaunchAnimation` |
| 中途死掉也演結局（先共用同一份） | `runFailedEvent` 留空 = 沿用 |
| 戰鬥中收起食物／遺物快捷欄 | `visibleInStages` 拿掉 Battle |
| 機率數字 29 → 58，版面改成上下分層 | `PD_Answer` |
| 機率牌左上角的黑色 `3` 關掉 | 共用卡面 prefab 上沒人接的舊文字 |

### 遺物的兩張圖

`ItemData` 現在有兩欄：

| 欄位 | 用在哪 | 美術 |
|---|---|---|
| `icon` | 快捷欄（持有遺物 UI） | **白色**那張 |
| `shelfIcon` | 商店貨架 | **彩色**那張 |

沒填 `shelfIcon` 就退回 `icon`（`ItemData.ShelfIcon`）——
食物只有一張圖，不必兩欄都填。

已接：快艇鑰匙、釣竿、魚叉、貪婪的大口（`貪食魚頭` ＝ 它的彩色版）。

⛔ **這四件現在都掛著 `NoEffect`，所以商店抽不到它們**
（`Loot_Sub_Relics` 的 `excludeTags` 排掉了）——
彩色那張圖目前在遊戲裡看不到，要等效果補上、跑
`Tools/spec_rebuild/gate_no_effect.py` 解禁之後才會出現在貨架上。
**圖是接好的，不是沒接。**

美術資料夾裡還有 `遺物儲存區` / `遺物儲存_框` 兩張，**還沒有人用** ——
那是持有欄的容器圖，要換的話是改快捷欄那條的底圖，不是 ItemData。

### 對話的高亮顏色 ＝ 屬性色

`ProbabilityAnswerUI.tintHighlightByAttribute`（預設開）。規則兩條：

1. **牌有屬性** → 用牌自己的顏色。玩家看到「我這張紅牌讓這個回答亮了紅」。
2. **黑白牌（`None`）** → 它對任何回答都有效，牌的顏色沒有意義，
   所以退回**回答自己的顏色**；收兩種屬性的回答因此亮成**兩色的混色**。

混色是平均 RGB，不是取第一個 —— 取第一個的話「紅＋藍」與「藍＋紅」
會亮成不同顏色，而那兩個回答其實是一樣的。

顏色一律來自 `AttributeChartData`，跟色點、卡框同一份。
`highlightBlend`（0.5）調偏多少、`highlightBoost`（1.12）調提亮，
**alpha 不參與** —— 連 alpha 一起動的話 hover 會變成整塊浮出來。

要退回原本那個米黃色就把 `tintHighlightByAttribute` 取消勾選。

### 對話的結算與離開鍵

之前 `Session.RunOutcome` 把 `EventEffect.Apply` 的回傳值丟掉了，
所以對話成功拿到東西、畫面上完全沒有結算。現在：

1. `RunOutcome` 把提示收進 `LastOutcomeNotes`（在 `OnEnded` **之前**填好）
2. `View.ShowOutcome()` 把「（獲得 XXX）」**排在最後一頁**
   （不是 `ShowInstant` —— 那會把玩家正在讀的最後一句刷掉）
3. Stage 輪詢 `PopupService.IsIdle`，話全播完才把 `endButton` 放出來

⚠️ **順序是有意義的**：結算排在最後一頁，離開鍵等那一頁也讀完才出現 ——
所以「拿到什麼」一定看得到。這跟事件那邊
（`EventStageController.endButton` ＋ `Update` 輪詢）是同一套。

`endButton` 是從 `Stage_Event.prefab` 的 EndButton 複製過來的，
文字改成「離開」，位置照抄（左下 60,430）——
**版面沒有用眼睛對過**，跟回答列／手牌會不會打架要進 Play 看。

⚠️ 留空 `endButton` 會退回舊的「等一下、點一下或逾時就走」，
`endMinSeconds` / `endAutoSeconds` 那兩格**只有那時才有作用**。

### 背景對齊相機（2026-08-29 第八輪）

祭壇上緣露出底色，就是交接文件早就量過的那個 **0.92 個單位**：

```
祭壇背景　y −6.12 ~ 5.08
相機　　　y −4.00 ~ 6.00　（正交、size 5、位在 y = 1）
          ↑ 上緣差 0.92（1080p 約 78px 的天空底色）
          ↓ 下緣反而多出 2.12 —— 整張圖偏低
```

新元件 **`Core/BackdropFit.cs`**：量出背景的實際 bounds，把整組**位移到相機中心**；
位移完還蓋不滿才放大（會在 Console 說一聲，因為放大等於裁掉美術的取景）。

驗算（祭壇）：對齊後 y **−4.60 ~ 6.60**，上下各多 0.60、左右各多 1.07 → 蓋得住 ✓

⚠️ **`background` 那一格要明確指定。** 留空會挑「面積最大的 SpriteRenderer」，
但祭壇那組的**玻璃瓶比背景還寬**（21.79 vs 19.91）—— 猜出來的是玻璃瓶，
照著它對齊只會更歪。猜的時候會在 Console 吵一聲。

已套用：

| | 怎麼掛 |
|---|---|
| 祭壇 `Stage_SpecialEvent` | 美術是 prefab 裡的固定物件 → `Art_Altar` 直接掛 `BackdropFit` |
| 事件／機率對話 | 美術是執行時生成的 → `StageBackdrop.fitToCamera` 打勾 |
| 地圖 | 同上 |

`StageBackdrop.fitToCamera` 打開之後 **Offset 那一格就不重要了**（會被量出來的值蓋掉）。
要照美術調好的取景擺就取消勾選，那時 Offset 才是真相。

> 房間美術（`Room_Village_*`）是同一組數字，只是靠 prefab 根整個往上擺了 1
> 才剛好蓋住。**換一張比例不同的圖，那個手調值就失效了** ——
> 真的要治本，房間也掛同一支。

### 背景的擺法統一了 —— 全部走 `BackdropFit`

之前有**兩套規則並存**，所以地圖與探索的背景看起來高度不一樣：

| | 規則 | 背景實際範圍 |
|---|---|---|
| 探索的房間 | prefab 根手動擺在 **y = 1** | y −5.12 ~ 6.08 |
| 地圖／事件／對話 | `fitToCamera` **量出來置中** | y −4.60 ~ 6.60 |

差 **0.52 個單位**（1080p 約 56px）—— 那個 y = 1 是當年對著中屋那張圖手調的，
換一張比例不同的圖就失效，而且**沒有任何地方寫著它是「規則」**。

現在**只有一套規則：量出背景 bounds，置中到相機**。六個都驗過，
全部落在同一個範圍：

```
y −4.60 ~ 6.60　　x −9.96 ~ 9.96
　Room_Village_01 / Small / Large / Outdoor / Room_Altar
　Art_Village_Outdoor（地圖／事件／對話）
```

#### ⚠️ 房間要掛在**根**上，不能掛在美術上

`Slot_*` 與 `Art_*` 是**兄弟**（都是房間根的子物件）。
只移動美術的話兩者會脫鉤 —— 畫上去的櫃子旁邊就會浮出一個寶箱，
而那正是交接文件 F 節在講的那個矛盾畫面。

掛在房間根上，美術與 slot **一起走**，對位完全不受影響。
（對齊後房間根從 y = 1.00 變成 y = 1.52，slot 跟著上移 0.52，相對關係不變。）

#### 什麼時候呼叫

| 誰 | 怎麼觸發 |
|---|---|
| 房間 | `ExploreStageController` 在 **`ApplyDressing` 之後**呼叫 `Fit()` |
| 執行時生成的背景 | `StageBackdrop.fitToCamera` |
| prefab 裡的固定美術（祭壇） | 元件自己的 `OnEnable` |

⚠️ **房間一定要等擺設切完才量** —— `ApplyDressing` 會切換子物件，
切完才是這一間房真正的樣子。

> 地圖用的是**戶外**那張、探索用的是**中屋**那張，所以畫面內容本來就不同；
> 統一的是「怎麼擺」，不是「擺什麼」。

### 第十三輪（2026-08-30）

#### 戰後獎勵：照敵人階級給錢

`BattleStageController.tierRewards` —— **階級決定範圍，範圍之內隨機**：

| 階級 | 金幣 |
|---|---|
| Minion | 10 ~ 20 |
| Elite | 30 ~ 50 |
| Boss | 80 ~ 120 |

階級從**節點**上讀（`RunNodeData.enemyTier`，地圖生成時由 `EncounterPlanner` 排的）——
所以「這一站值多少」在玩家走進去**之前**就決定了。
現場看「打了哪幾隻」的話，同一個節點重進可能給不一樣的錢。

亂數綁 run 種子 ＋ 節點 id，所以**重打同一站不會刷出不同的錢**。
事件插播的戰鬥（《貪吃鬼》）沒有節點階級，會退回清單第一筆。

⬜ **道具與選卡這一版不做**（2026-08-30 使用者定案：先給錢就好）。
要加的話這裡多一個 LootTable 欄位，流程不用動。

#### 出牌動效：往畫面中央射出去

`ProbabilityCardUI` —— 抬起 → 沿拋物線飛向畫面中央 → 邊縮邊淡掉。

| 欄位 | 預設 |
|---|---|
| `launchSeconds` | 0.32 |
| `launchEndScale` | 0.55（一邊飛一邊變小，像被吸進去） |
| `launchArcHeight` | 90（拱一點才像「拋出去」而不是「被拖過去」） |
| `launchTargetOffset` | (0, 60)（往上偏一點，更靠近機率數字） |

⚠️ **`OnPlayRequested` 在動畫開始的當下就發，不是跑完才發。**
等動畫跑完的話機率數字會慢半拍 —— 那比沒有動畫更糟。
動畫的作用是把玩家的視線送到畫面中央，剛好接上 `AnimateProbability` 的 countUp。

⚠️ 出牌被拒絕時（`ReturnHome`）要把動畫停掉並復原 scale／active ——
不然牌會一邊飛走一邊「回到原位」。

#### 中途死掉也演結局

`runFailedEvent` **留空 ＝ 沿用打完 Boss 那一份**。
《文本.md》那段獨白本來就寫得兩邊都成立：
「恭喜你，決定了世界的末路／**恭喜你，迎向了人生的終點**」說的既是通關也是死亡，
而「再一次...開始你的旅途吧」正是輪迴的入口。
要讓死亡有自己的說法時再填一筆，程式不用動。

#### 戰鬥中不能用食物

`ShortcutBar_Food` / `ShortcutBar_Relic` 的 `visibleInStages` **拿掉 Battle**。

> 遺物欄也一起收了 —— 遺物本來就是被動觸發（戰鬥開始時灌進 `RelicsInventory`），
> 戰鬥中「用」不了；而且戰鬥自己有 `Relics_Panel` 可以看。
> 要把遺物欄留在戰鬥裡的話，把 Battle 加回**那一條**的清單就好。

#### 機率數字字級翻倍（29 → 58）

⚠️ **字級翻倍之後版面要跟著改，不然會被裁掉**：58 級的「100%」需要 150×67，
但原本那一格只有 107×89，而且 `enableWordWrapping` 是開的 —— 數字會換行。

改成**上下分層**而不是左右分欄：

| | 之前 | 現在 |
|---|---|---|
| 回答文字 | 158 寬（左右分欄時） | **337 × 108**（比原本的 315 還寬） |
| 機率數字 | 107 × 89，下緣凸出框外 10px | **168 × 68**，完全在框內 |

數字本來就在右下角，讓回答文字把**底部**讓出來就好，寬度可以全留著。
`enableWordWrapping` 關掉 —— 數字換行比溢出更難看。

#### 機率牌左上角的黑色數字

那是共用卡面 prefab（`EP_cardexplore_template`）上的 **「消耗」**，
寫死的黑色 `3`；旁邊還有一個 **「效果描述」** 寫死的黑色「造成傷害」。

⛔ `CardViewUIExplore` 的 `costText` / `descriptionText` / `nameText`
**三個欄位全都是空的** —— 從來沒有人去更新它們，畫面上就是那幾個固定的字。

機率牌的數字與效果都畫在美術裡，所以在 `AttachCardUI` 把這兩個關掉。
**只關複製品，不動 prefab** —— 探索打牌是已驗收的流程。

⚠️ **探索那邊有同一個問題**（同一個 prefab、同樣沒接欄位）。
現在看不出來可能是被美術蓋住或位置剛好在框外。
要一起關的話是一行的事，但那是動已驗收流程，等你說。

### 第十二輪（2026-08-29）

#### 判定動效補回來了

原本是「按下去 → 選項直接變暗」，玩家看不到判定那一刻。
舊版探索打牌的 `DialogueOptionUI.PlayResultFlash` 本來就有這一下，
換成 `ProbabilityAnswerUI` 之後掉了 —— 現在照同一套補回去：
**底色壓暗 → 字換成「成功／失敗」→ 淡回來 → 才變暗**。

⚠️ **兩個順序問題比動畫本身難**：

1. `SetDisabled()` 在動效跑的時候**不會立刻變暗**，會排到動效結束。
   Session 是「判定 → 立刻停用」同一幀連著發的，照做的話那一下閃光
   會打在已經半透明的格子上，效果整個弱掉。
   （**可不可以點是當下就鎖的**，只有「看起來變暗」被延後。）
2. `HandleOptionResolved` 原本立刻 `SetPlayVisible(false)` ——
   那會把整排回答的 alpha 打成 0，**動效照跑但玩家一格都看不到**。
   現在改成等動效跑完才收、才播 successText。失敗那條（`HandlePromptChanged`）同理。

`playResultFlash` 取消勾選就回到直接變暗。

#### 金幣

- 素材接上了：商店的金幣數字左邊加了 `MoneyIcon`（`ART/資源/金幣.png`），
  `moneyFormat` 從 `金幣 {0}` 改成 `{0}`（字讓給圖示）。
- **所有容器都會給錢**（8 個 prefab 都掛 `ChestInteractable`，一次全中）：
  `moneyMin/Max` 預設 **3~8**。
- ⛔ **空箱保底**：`emptyBonusMoneyMin/Max` 預設 **5~12**，
  在「道具一件都沒掉」時**額外**加上去。
  開到完全空的箱子是最掃興的結果 —— 玩家花了牌、花了時間換到「什麼都沒有」。

【為什麼金幣不做進 LootTable】戰利品表產出的是 `ItemStack`，
而金幣在 `RunContext.money` 不在背包裡 —— 硬塞會讓「金幣」變成一件背包道具。
所以錢是容器自己的欄位，跟道具分開算（種子也錯開 `^ 31`，
不然換一張戰利品表連金額都會跟著變）。

#### 打完 Boss 的結局

`GameFlowManager.runFinishedEvent` → **`Event_run_finished`**（文本出自《文本.md》）。
借用事件模組，因為它已經有這一段需要的全部東西：一句一句播、名字框、
結果文字、專屬的結束鍵 —— 另外寫一個「結局 Stage」等於把那些再做一次。

流程：打完 Boss → `RunFinished` → 遺產結算 → **演結局事件** → 主選單。

⚠️ **三個雷**：

1. `weight = 0` —— 不然它會跑進隨機事件池，玩家第三站就看到結局獨白了。
2. `currentStage != Event` 的防迴圈 —— 結局本身是事件 Stage，
   它演完也回報 `RunFinished`，不擋就會一直重播自己。
3. `once = false` —— 每一輪打完都要播。

收尾那句「謝謝測試！／如果可以，請幫我們填寫問卷。」放在 `options[0].resultText`，
**沒有說話者 → 走旁白公版**，跟劇中人的台詞分開。
⚠️ 那是**測試版限定**，正式版要拿掉；問卷連結還沒有，有了補在同一格。

### Build 前的設定（2026-08-29 第十一輪）

| | 之前 | 現在 | 為什麼 |
|---|---|---|---|
| `EventLibrary.globalChance` | 1（測試值） | **0.35** | 1 ＝ 每一站都插事件，那是測試用的密度 |
| `EventLibrary.verbose` | true | **false** | 每一站印一串判定 log，給人試玩不需要 |
| `guaranteeEventOnFirstNode` | （新增） | **true** | 見下 |

實測 300 場（走一條路到底）：一場遇到 **1~7 個事件、平均 3.4**，
沒有任何一場是 0 個。（globalChance=1 時是一場 8~20 個。）

#### ⚠️ 調低 globalChance 會打壞「開場介紹」—— 所以補了一格

`priority = 100` 保證的是「**輪到事件時**它先出」，
但**保證不了「這一站有沒有事件」**—— 那一關（`globalChance`）在更前面。

所以 0.35 之下，《暴食之深淵》有 **65% 的機率不會在開場出現**，
會晃到第三、四站才冒出來 —— 那就不是介紹了。

`GameFlowManager.guaranteeEventOnFirstNode`（預設 on）：
**第 0 層不擲那一關**，直接進候選。

兩件事各管各的，這樣才對得起來：

| | 管什麼 |
|---|---|
| `globalChance` | 這一站到底**要不要**有事件 |
| `priority` | 要有的話**先輪到誰** |

⚠️ 只跳過「要不要有事件」那一關 —— **條件、優先序、事件自己的機率全部照跑**。
第 0 層沒有任何合格的事件時就是安靜地不觸發，不會卡住。

驗過：**300/300 場第一站都有事件** ✓

#### Build 前的其他確認

| | |
|---|---|
| Build 場景清單 | 只有 `EventScene` 勾著（其餘 6 個取消勾選，4 個指向 `_Archive`） |
| 目標平台 | StandaloneWindows64、`0.1.0` |
| Development Build | **關** → F1 除錯面板不會進 build（`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`） |
| `FullScreenMode` | `FullScreenWindow` ＝ **跑桌面原生解析度**，不是 1920×1080（見下一節） |

### ⛔ 神牌動畫在 build 裡會走鐘：`AnimCanvas` 是唯一沒開 Scale With Screen Size 的

**是的，它沒有設。** 全專案 12 個 Canvas 只有它一個不一樣：

| Canvas | Scaler |
|---|---|
| `AnimCanvas`（神牌動畫） | ⛔ **ConstantPixelSize，ref 800×600** |
| 其餘全部（HUD／地圖／對話／戰鬥的另外三個…） | ScaleWithScreenSize **1920×1080** |

`ConstantPixelSize` ＝ **不隨解析度縮放**，scaleFactor 永遠是 1。

| 解析度 | Constant | ScaleWithScreen | 差多少 |
|---|---|---|---|
| 1280×720 | 1.000 | 0.667 | −33% |
| **1920×1080** | 1.000 | 1.000 | **一樣** |
| 2560×1440 | 1.000 | 1.333 | +33% |
| 3840×2160 | 1.000 | 2.000 | +100% |

⚠️ **所以在編輯器裡永遠看不出來** —— Game View 就是 1920×1080，兩種模式完全相同。
而 Player Settings 是 `FullScreenMode = FullScreenWindow`，
**build 出來是跑桌面的原生解析度**，不是 1920×1080 ——
在 1440p／4K 螢幕上神牌動畫就會比其他 UI 小一大截。

> 這**不是**併場景弄壞的：`SampleScene` 原本就是 ConstantPixelSize 800×600。
> 是原本就在的設定，只是以前沒有 build 過所以沒人看到。

**已改成** ScaleWithScreenSize 1920×1080 match 0.5，跟同一個 prefab 裡另外三個一致。
**1080p 下是零變化**（兩種模式數值相同），其他解析度才會有差 —— 所以這個改動很安全。

#### 順帶：`match` 值不一致（沒動）

專案裡有些 Canvas 是 `match = 0`（只看寬度）、有些是 `0.5`（寬高幾何平均）。
**16:9 之下三種 match 算出來完全一樣**，所以現在看不出差別；
螢幕不是 16:9（16:10、21:9）時各 Canvas 才會縮得不一樣。
要不要統一是視覺決定，先不動。

### 「事件接回節點」很突兀 —— 三個原因，先修了第一個

事件是**前置不覆蓋**（演完再接回原本的節點），所以會有一次「玩家沒按任何東西、
畫面卻換了地方」的轉場。突兀感有三個來源，是**三件不同的事**：

| | 是什麼 | 狀態 |
|---|---|---|
| 1. 時間感 | 淡出 → **立刻**切 → 淡入，中間**一格全黑都沒有** | ✅ 已修 |
| 2. 資訊 | 玩家不知道自己現在在哪 | ⬜ 待決定 |
| 3. 敘事 | 事件的收尾與節點的開場之間沒有橋 | ⬜ 待決定 |

#### 1. 剪接節奏（已修）

原本是 `FadeOut(0.4) → 切 → FadeIn(0.4)`，**對稱、而且沒有停頓**。
人眼會把那讀成「同一個畫面閃了一下」，而不是「換了一個地方」——
跟資產、載入速度都無關，純粹是剪接節奏。

電影剪接的老規矩：**淡出快、黑幕停一拍、淡入慢**。停頓的長短就是
「這兩個場景之間隔了多遠」。現在：

| 欄位 | 值 |
|---|---|
| `fadeOutSeconds` | 0.30（離開快） |
| `fadeInSeconds` | 0.55（進入慢） |
| `holdBlackSeconds` | 0.25（一般轉場） |
| `holdBlackAfterEventSeconds` | **0.50**（事件→節點、事件安排的戰鬥→節點） |

事件→節點總長 0.80 → **1.35 秒**，而且中間有 0.5 秒是真的全黑。

⚠️ **接續型的那兩跳要停久一點** —— 玩家沒有按任何「前往」的動作。
一般的「地圖上點了節點」是玩家自己選的，反而不需要那麼久。

⚠️ `HoldBlack` 走 `unscaledDeltaTime` —— 轉場期間 `timeScale` 有可能是 0
（暫停、或戰鬥端把時間停住），用 scaled 會整個卡在黑幕裡。

> `ScreenFader.defaultDuration` 現在不再被用到，兩個秒數都從 GameFlowManager 傳進去。

#### 2. 資訊：地點卡（已做）

業界的標準答案是 **Title In / 地點卡** —— 淡入之前在黑幕上打出地名。
RPG Maker 社群的說法很直白：**「這比單純淡入、然後把角色放在一個全新的地方好」**，
因為它建立了「我到了某個地方」的感覺。Resident Evil Village、
Resident Evil Requiem 都是這一套。

沿用專案原本就有的 `MapBannerUI.ShowMapTitle(string)`
（淡入 → 停留 → 淡出）—— **跟開地圖時那句「地圖」是同一支元件、同一個字型**，
所以兩張卡看起來是一套的。

新物件 **`[SYSTEM]/StageTitleBanner`**：

⚠️ **不能用地圖那一顆**（`[MAP_OVERLAY]/MapPanel/MapBanner`）——
它跟著地圖一起滑出畫面，轉場時根本不在畫面上。

⚠️ **Canvas order 要在黑幕之上**：黑幕是 9000，卡片是 **9500**。
排在底下的話會被整片黑蓋掉，而且不會有任何錯誤訊息，只是看不見。

⚠️ `MapBannerUI` 原本用 `WaitForSeconds` 與 `Time.deltaTime`（**受 timeScale 影響**）。
它現在也在轉場期間跑，而轉場中 `timeScale` 有可能是 0 ——
會**永遠卡在黑幕裡**。已改成 `WaitForSecondsRealtime` / `unscaledDeltaTime`。

##### 有插播事件時會出現兩張卡

```
《暴食之深淵》 → 演事件 → 環境 → 房間
```

事件先報自己的名號、節點再報自己的 —— 那兩次換畫面就從
「莫名其妙跳了兩下」變成「一段插曲，然後才到目的地」。
事件卡用的是 `EventData.title`（現成的、本來就給玩家看的），
格式在 `eventTitleFormat`（預設 `《{0}》`）。

##### ⛔ 文案是佔位的

`GameFlowManager.nodeTitles` 目前**沿用地圖 tooltip 既有的用詞**：

| 節點 | 卡片 |
|---|---|
| Event（探索房） | 環境 |
| Combat | 敵人 |
| Boss | 首領 |
| Shop | 商店 |
| SpecialEvent | 特殊 |
| Dialogue | 遭遇 |

**那是佔位不是定案** —— 地點卡是玩家每一站都會看到的東西，
值得給它真正的地名（「漁村・空屋」那種）。改 Inspector 那一格就好，不用動程式。
留空的類型會自動退回純黑幕停頓。支援 rich text（可以像 `mapEnterText` 一樣上色）。

##### 時間

淡出 0.30 → 卡片 1.35（0.25 淡入 ＋ 0.85 停留 ＋ 0.25 淡出）→ 淡入 0.55
＝ **一次轉場 2.20 秒**。嫌長就調 `StageTitleBanner` 上的 `mapTitleHoldTime`
（地圖那顆是 2 秒，轉場這顆調成 0.85）。
`showTitleCards` 取消勾選就整個關掉，退回只有黑幕停頓的版本。

#### 3. 敘事：讓事件的收尾指向目的地（未做）

零程式，純文案：事件的最後一句改成朝向下一站
（「你把船推回岸邊，回頭看見那扇門。」）。
成本是**每個事件都要寫一次**，而且事件是隨機插在任何節點前面的，
所以只能寫得夠泛用。

#### 4. 另一條路：不要插入，改成取代

Slay the Spire 那一套：**事件節點就是事件**，演完直接回地圖，不會再接一個房間。
根本地消滅這個轉場。

代價是推翻既有的決定（「前置，不覆蓋。玩家不會因為運氣好觸發了事件
反而少玩到一間房。」），而且事件的密度要重新配 —— 這是個設計決定。

### 神牌的結算移到最後（2026-08-29 第十輪）

之前是「選完 → 進牌庫 → 祭壇仍然是塌的」，結算夾在中間。
現在：

```
① 你跪了下來…（劇情）      選完的當下，ShowInstant
② 凹槽裡的水面恢復平靜。
   祭壇仍然是塌的。         收尾（outroLines）
③ 【深淵】的牌進了你的牌庫。 結算 —— 離開前的最後一頁
```

【怎麼切】**空行 ＝ 分段**，跟對話框的分頁規則同一套 ——
`takenText` 的**最後一段**當結算，前面全部是劇情。
**文案完全不用改**，本來就是那個格式。

整段沒有空行時（`defaultTakenFormat` 那種一句話），整段都當結算，
在選完的當下直接播 —— 不然玩家點下去會有一拍完全沒有反應。

`settlementLast` 取消勾選就回到舊行為。

#### `ChoiceStageController.outroTailLines`

新的 `protected` 清單，**排在 `outroLines` 之後**。
事件、對話那兩邊的結算也都是「最後一頁」，這下三個環節的節奏一致了。

⚠️ **不要改用直接往 `outroLines` 塞** —— 那是序列化資料，
執行時塞東西會看起來像「這個 prefab 本來就有那一句」。

### 事件有優先序了

《暴食之深淵》是這張地圖的開場介紹，本來跟《無人的小船》都是 weight 1，
所以誰先出全看運氣。

`EventData.priority`（預設 0，**數字大的先出**）：

| | 管什麼 |
|---|---|
| `priority` | **哪一批**先輪到（大的整批贏過小的） |
| `weight` | **同一批之內**誰被抽到 |

只用權重做不出「一定先出」—— 權重再高也只是機率高，
而開場介紹被壓到第三站才出現就沒有意義了。

挑的順序：**條件 → 優先序 → 權重 → 事件自己的機率**。

現在的設定：

| 事件 | priority |
|---|---|
| 暴食之深淵 | **100** |
| 其餘 | 0 |

模擬 200 次第一站：**暴食之深淵 200/200**。
它 `once=true`，觸發過就退場，第二站起無條件的只剩《無人的小船》。

⚠️ 高優先的事件**還是要過條件與機率那兩關** ——
條件不成立時會直接輪到下一批，不會把整站卡住。

⚠️ 「最高的那一**批**」不是「最高的那一**個**」——
同一個 priority 有好幾個時還是隨機，不然就變成一張固定的播放清單了。

### 連線長度改成照實際距離算

打開 `AllConnections` 之後才看得出來：**線的長度是寫死的**。

```
舊：rect.sizeDelta = lineSize;      // 固定 5 × 200
實測節點距離：123 ~ 267
```

所以短的那些兩端各戳出去一截、長的那些接不到 —— 兩種都像「沒接上」。
而且距離會隨層數、路徑數、視窗比例改變，**任何寫死的值遲早都會錯**。

現在：

```csharp
float length = Mathf.Max(lineMinLength, dir.magnitude - lineEndGap * 2f);
rect.sizeDelta = new Vector2(lineThickness, length);
```

| 欄位 | 預設 | 是什麼 |
|---|---|---|
| `lineThickness` | 5 | 粗細（就是舊 `lineSize` 的 x） |
| `lineEndGap` | 28 | 兩端各留的空隙，讓線不要壓在節點圖示底下（節點 70×70） |
| `lineMinLength` | 8 | 扣完空隙至少留這麼長，避免靠很近的兩點算出 0 或負數 |

⚠️ 舊的 `lineSize`（Vector2）**已移除** —— 一個「x 有用、y 沒用」的欄位是陷阱。

**50 張圖 1171 條線驗過**：距離 123~267 → 畫出來 67~211，
**0 條需要用最小值兜底**，所以空隙設定是安全的。

> 兩端縮一樣多，所以中點沒變 —— 位置與旋轉的算法完全沒動。
> 節點的縮放會隨狀態變（當前 1.2 / 可前往 1.0 / 去不了 0.8），
> 所以 28 是折衷值，不可能三種狀態都剛好貼齊。

### 地圖連線：改成畫出全部

`MapView.lineDisplay` 之前是 **`VisitedPathOnly`**（只畫走過的），
所以還沒走過的節點之間**一條線都沒有** —— 玩家沒辦法判斷
「我往 1 走，之後能到 2 還是 3」。

改成 **`AllConnections`**：整張路網一開始就看得到，可以提前規劃。

> enum 的說明裡寫「代價是失去未知感」—— 那是當初的取捨，
> 現在的決定是「可規劃」比「未知」重要（2026-08-29 使用者指示）。
> 三種模式都還在，換一格就切得回去。

### 併場景時掉了什麼（2026-08-29 第七輪）

把 `SampleScene` 併進 `EventScene` 時，**三樣東西沒跟過來**。
三樣都不會報錯，所以都是「看起來像壞掉，但查不到錯誤」的那種。

#### ⛔ 一、後製整組不見了 —— 這才是「渲染」的大宗

| | SampleScene | EventScene（修之前） |
|---|---|---|
| Global Volume | **2 顆** | **0 顆** |
| Main Camera `renderPostProcessing` | **True** | **False** |

兩顆 Volume 的內容：

- `SampleSceneProfile` —— Tonemapping ＋ Bloom ＋ Vignette
- `Global Volume (1) Profile` —— ColorAdjustments ＋ Bloom

**兩個條件缺一，後製就整個不會跑。**
上一輪只補了燈（下面那條），但燈只負責明暗；
戰鬥那個味道有一大半是 Bloom 與 ColorAdjustments —— 所以上一輪修完還是「沒有渲染」。

**修法**：兩顆 Volume 放進 `Stage_Battle.prefab`、Main Camera 打開 `renderPostProcessing`。

⚠️ 相機那顆開關**是全域的**（URP 只有 per-camera 這一個），
但 Volume 在 prefab 裡，所以其他環節沒有 Volume ＝ 等於沒有後製。
**要讓整個遊戲吃同一組後製，就把那兩顆 Volume 從 prefab 拖到場景根** ——
那是美術決定，不是順手改（房間美術是照沒有後製的畫面對過位的）。

#### 二、`Global Light 2D`（上一輪補的）

URP 用的是 2D Renderer、Blend Style 0 ＝ Multiply。
`Global`（黑、Multiply）把畫面乘黑、`Freeform` 開一個看得見的區域。
Freeform 在 `BattleSystemPrefab` 裡所以跟著走了，Global 是場景根物件所以漏掉。
一樣放進 `Stage_Battle.prefab`。

> ✅ 順手查過了：`monsterCanvas` / `SceneCanvas` / `AnimCanvas` 的
> **`m_RenderMode` 一直都是 ScreenSpaceCamera，沒有掉**。
> 用 C# 讀 `canvas.renderMode` 會看到 Overlay，那是因為 `worldCamera` 是 null 時
> **getter 回報的是「實際生效的模式」而不是設定值** —— 別被它騙了，
> 要看真相請讀序列化欄位 `m_RenderMode`。相機由 `BindCanvasCameras` 在執行時補。

#### ⛔ 三、「指定對手」從來沒有生效過

**`ReserveEncounterByEnemyData` 寫得進去，但沒有任何程式讀它**
（`TryGetReservedEncounter` 一個呼叫端都沒有）。

真正決定「這場打誰」的是 `EnemyFormationSpawner.SpawnRandomFormation()`，
它只認 **`RunStateManager.TryGetReservedFormation`（Formation）**。
沒有預約 Formation 就走 `encounterPool.GetRandomFormation()` 隨機抽 ——
**所以填對 id 也沒用，照樣隨機**。這就是「叫出來的怪跟名字對不上」。

而且兩邊各自都「成功」了，所以一個錯誤訊息都沒有。

**修法**：`BattleStageController.ReserveFormationFor()`

1. 先找一組**成員完全吻合**的現成 Formation（用它才會沿用美術排好的站位）
2. 找不到才即時 `CreateInstance<EnemyFormationData>()` 捏一組（不寫成資產）

現成的對照（驗過）：

| enemyId | Formation |
|---|---|
| `boss` | `Bosss` |
| `fish_priest` | `test 1` |
| `tua_khoo_tai` | `tuā-khoo-tai` |
| `coral_paguroidea` | `Paguroidea` |
| `minnow` | 沒有單隻的 → 即時捏一組 |

⚠️ 比對用「完全吻合」不是「包含」：`test` 那組是雜魚＋祭司＋雜魚，
用「包含」的話指定打祭司會變成打三隻。

善後不用管 —— `BattleManager` 結束戰鬥時會 `ClearReservedFormation()`。

### 戰鬥裡叫不出食物

**兩件事，先分清楚：**

1. **右邊那條食物快捷欄，戰鬥中本來就能用**（`UIPanel.visibleInStages` 有 Battle，
   走的是 `hpRestore` / `sanRestore`，不經過戰鬥系統）。這一條沒壞。
2. **戰鬥自己的道具面板（`Props_Panel` 上的 `ItemInventory`）永遠是空的** ——
   那才是「叫不出來」。

原因有兩層，而且**第二層卡死了**：

| | 狀況 |
|---|---|
| 沒有人把背包灌進 `ItemInventory` | 我方的缺口 —— **已補** `BindItemsFromInventory()`，與遺物那條完全對稱 |
| ⛔ 全專案**一個 `ItemEffectData` 資產都沒有** | `ItemEffectData` 是 abstract、**沒有任何子類別**。所以 `ItemData.battleItemEffect` 現在填不了 |

`ItemData` 已經補上 `battleItemEffect` 欄位（跟 `relicEffect` 對稱）。
**Romtyui 把 `ItemEffectData` 的子類別與資產做出來、填進那一欄，
戰鬥的道具面板就會自己開始有東西，不用再改程式。**

在那之前，戰鬥中要吃東西請用右邊的快捷欄。

### 中段節點改回隨機（2026-08-29 第六輪）

`useDemoRoute` 關掉 → 走隨機生成。**固定的只有兩頭**：

| | 誰決定 |
|---|---|
| 首排 ＝ 神牌 | `firstLayerKind`（層數固定，不是機率） |
| 最後一層 ＝ Boss | `PickKind` 的第一行（層數固定） |
| 中間全部 | `combatChance` / `shopChance` / `dialogueChance`，其餘歸探索 |

現在的機率：戰鬥 0.45、商店 0.12、對話 0.15、神牌 **0**、其餘 0.28 歸探索。

#### ⛔ 對話節點以前**根本不會出現在隨機地圖上**

`PickKind` 的機率表裡從來沒有 `Dialogue` —— 只有 DEMO 的固定路線寫死了幾個。
所以「換成隨機生成」等於**整個對話環節測不到**，
而且不會報錯，只會「怎麼玩都沒遇到對話」。已補上 `dialogueChance`。

#### 純機率會漏 —— `guaranteedKinds`

實測 200 張圖：**13% 完全沒有商店、6.5% 完全沒有對話**。
測試的人抽到那種圖會以為是功能壞了，而不是運氣。

`MapGenerationSettings.guaranteedKinds`（預設 `Shop` + `Dialogue`）：
缺的話挑一個**中段的探索節點改成它**。

- **只改 `kind`，完全不碰 `nextNodeIds`** —— 所以連通性、交叉、層數都不受影響。
  插一個新節點才要重算連線，那會動到關卡形狀。
- 只挑**探索**節點下手（那類本來就是「其餘機率歸它」的填充），挑不到就放棄並警告。
- 不含首排與 Boss 層 —— 動了就破壞「首排一定拿得到神牌」。

**300 張圖驗過**：沒有商店 0、沒有對話 0、首排不是神牌 0、
Boss 不是剛好一個 0、孤兒 0、死路 0。

> DEMO 的分支路線沒有刪，`useDemoRoute` 打開就切回去。

### Boss 的 F1 快捷

F1 面板多一排「指定對手開打」。**Boss 只能從這裡驗** ——
它在地圖最後一層，正常要打到它得先走完整張圖。

跟上面那顆 `Battle` 按鈕的差別：那顆打的是「當下被預約的對手」（多半是雜魚）；
這一排是先塞 `BattleStageController.PendingEnemyId` 再跳，
**跟《貪吃鬼》事件叫戰鬥的是同一條路** —— 所以這裡驗得過的，事件那邊也會對。

按鈕清單在 `RunDebugPanel.quickBattleEnemyIds`（預設五隻全列）。

### ⛔ 戰鬥的燈掉了：`Global Light 2D` 沒跟著併進來

**症狀**：戰鬥畫面「渲染掉了 / 沒氣氛」，但東西都在、也打得起來。

**成因**：專案的 URP 用的是 **2D Renderer**（`test'_Renderer` ＝ `Renderer2DData`），
Blend Style 0 是 **Multiply**。戰鬥那個「一片黑、只有怪被打光」的畫面
是**兩顆 Light2D 合出來的**：

| 燈 | 設定 | 作用 |
|---|---|---|
| `Global Light 2D` | Global、**黑色**、intensity 2.48、Multiply | 把 Default／sceneUI 整個乘成黑的 |
| `Freeform Light 2D` | Freeform、偏藍、intensity 5.01、order 48 | 在那片黑上開一個看得見的區域 |

`Freeform` 那顆在 `BattleSystemPrefab` 裡面，所以包成 `Stage_Battle.prefab` 時跟著走了；
**`Global Light 2D` 是 `SampleScene` 的場景根物件，併進 `EventScene` 時漏掉了**
（EventScene 一顆 Light2D 都沒有）。

⚠️ **少那顆不會有任何錯誤訊息** —— Multiply 沒有光源時光照貼圖是白的，
乘上去等於沒乘，所以畫面是「全部亮著」而不是「黑畫面」。
症狀是「沒氣氛」不是「東西不見」，很難聯想到燈。

**修法**：照 SampleScene 的設定原封不動複製一顆進 **`Stage_Battle.prefab`**。

⚠️ **不要放回場景根** —— 它是 Multiply 的黑光，常駐的話房間美術、地圖、
對話立繪會跟著一起變黑。放在 Stage prefab 裡才會「只有戰鬥的時候黑」：
`StageHost` 是 Instantiate／Destroy，這一站結束燈也跟著消失。

> 這是照 Romtyui 原場景**重建**的，數值一模一樣但**我沒有進 Play 看過** ——
> 請他們對一眼是不是原本的味道。

### 地圖：分支路線與連線保證（2026-08-29 第五輪）

#### 連線本來就有保證，但看不出來 —— 現在會自己吵

`MapGenerator.Generate` 的出口加了 `WarnIfUnreachable`：
從第 0 層 BFS 一次，**走不到的節點**與**走進去出不來的節點**都印到 Console。

⚠️ **只警告，不自動補線** —— 補線會改變關卡形狀，那是設計決定。
這一關的職責是讓問題現形：少一條線**不會有任何錯誤訊息**，
畫面上那個節點還在、只是永遠是暗的，看起來像「還沒解鎖」而不是「地圖漏了」。

（隨機生成那邊本來就連得通 —— 每個活下來的格子都在某條從起點走到頂的路上。
真正有風險的是手寫的固定路線，所以這一關主要是替它把關。）

#### DEMO 路線多一種形狀：Branching

`MapGenerationSettings.demoRouteShape`：

| | 讀哪一欄 | 長什麼樣 |
|---|---|---|
| `Straight`（舊的） | `demoRouteKinds` | 一層一個節點的直線 |
| `Branching`（**現在用這個**） | `demoRouteLayers` | 一層好幾個節點，玩家要選 |

**直線路線驗不到地圖本身** —— 只有一條路的時候「連線」「可前往／去不了」
「選節點」全部沒有作用，而那些正是地圖這一層要測的東西。

現在的分支路線（7 層 15 個節點）：

```
L0  神牌  神牌            ← 首排，兩個起點
L1  探索  戰鬥  探索
L2  商店  對話
L3  戰鬥  探索  戰鬥
L4  對話  商店
L5  戰鬥  戰鬥
L6  Boss                  ← 終點只有一個
```

驗過：**15/15 全部走得到、0 個死路**。

⚠️ **最後一層只放一個 Boss** —— 打完 Boss 這場 run 就結束了
（`MapData.IsFinalLayer`），放兩個的話另一個永遠走不到。

##### 連線是怎麼算的

`ConnectLayers` 對每一對相鄰層跑**兩輪**：

1. 每個下層節點 → 上層「相對位置最近」的那一個（沒有死路）
2. 每個上層節點 ← 下層「相對位置最近」的那一個（沒有孤兒）

只跑第 1 輪的話，上層比下層多時多出來的那些永遠沒人連過去；只跑第 2 輪則相反。
兩輪都跑，兩種都不會發生。`Connect` 自己會去重。

**不會交叉**：兩輪用的都是同一個單調遞增的對應（`NearestIndex`），
所以線不會打結 —— 隨機生成那邊是靠 `PickNextColumn` 事後擋交叉，
這裡用「本來就不可能交叉的算法」達成，不必檢查。

#### 首排固定出特殊事件（＝ 神牌）

`MapGenerationSettings.firstLayerKind`（預設 `SpecialEvent`）。
**隨機生成與 DEMO 分支路線都吃這一格** —— 在兩個地方各寫一次的話，
改了一邊忘了另一邊，症狀會是「隨機圖開場是神牌、DEMO 圖開場是探索房」。

神牌是主玩法，放在首排就等於「在任何戰鬥之前一定拿得到」。

⚠️ 順帶把 `specialEventChance` 從 0.1 改成 **0** ——
首排已經保證有一次了，中間再隨機長出 SpecialEvent 的話玩家會**重看同一場祭壇戲**
（`Stage_SpecialEvent` 只有一份、兩張牌固定、也沒有 `once` 保護）。
之後神牌內容變多了再調回來。

#### ⚠️ `useDemoRoute` 這一格

我改回 **True**（要測的就是這條固定的分支路線）。
關掉的話走隨機生成 —— 那邊現在也是首排神牌、也連得通，只是節點組成每次不同。

### 敵人指定：填錯 id 跟留空是同一個下場

《好餓好餓的貪吃鬼》選「不給他吃的」原本填 `glutton`，
但 `EnemyDatabase` 裡**沒有這個 id** → `ReserveEncounterByEnemyData` 沒被呼叫
→ 交給戰鬥組自己抽 → 打到的是**半魚人祭司**。

已改成 `tua_khoo_tai`（`魚頭胖魚人_拆`、HP 80、音效是「胖魚人攻擊／受擊／退場」）——
那才是貪吃鬼。

現有的 enemyId（`Assets/Romtyui/Monster/漁村/data/`）：

| id | 資產 | 美術 | HP |
|---|---|---|---|
| `fish_priest` | Mermaid Priest | 新半人魚祭司 | 50 |
| `coral_paguroidea` | coral Paguroidea | 新珊瑚寄居蟹 | 60 |
| `tua_khoo_tai` | tuā-khoo-tai | **魚頭胖魚人** | 80 |
| `minnow` | 雜魚 | （借祭司的圖） | 20 |
| `boss` | Boss | 無臉人魚 boss | 200 |

⚠️ **這是最容易誤判的一種狀況** —— 打得起來、看起來正常，只是對手不對。
`BattleStageController` 找不到 id 時現在會把整份清單印出來，
而且明說「這一場會交給戰鬥組自己抽」。
（程式裡「五個敵人的 Enemy Id 都是空的」那句註解已經過期，一併改掉了。）

### 測試配置（2026-08-29 第四輪）

#### 地圖的背景調暗 —— 只有地圖

`StageBackdrop` 多一欄 `tint`（**白色 ＝ 原樣**，預設不變）。
地圖那一顆設成 **(0.42, 0.44, 0.52)**；對話與事件的維持白色 ——
那兩個環節的背景就是場景本身，調暗會變成另一件事。

是**相乘**不是覆蓋（`color *= tint`），所以美術自己畫的層次留著，
不會整片塗成同一個顏色。改的是 `Instantiate` 出來那一份，動不到 prefab 資產。

#### 快捷欄：Menu 不出現、感應區收窄

| | 之前 | 現在 |
|---|---|---|
| 感應區 `EdgeRevealUI.triggerZone` | 0.16（1920 寬 **≈ 307px**） | **0.035（≈ 67px）** |
| 展開後的容忍 `stickyMultiplier` | 1.6（≈ 491px） | **3.0（≈ 202px）** |
| Menu 時 | 一直在 | **收掉** |

⚠️ **sticky 要放大到 3.0 是有原因的** —— 欄位本身就有 120 寬，
容忍範圍比欄位窄的話，滑鼠移到最左邊那一格上就會觸發收起。
67 × 3.0 = 202px 剛好蓋得住整條欄位再多一點。

「Menu 不出現」**沒有寫新元件** —— 專案早就有 `UIDirector` ＋ `UIPanel`
那一套（面板自己宣告「我屬於哪些環節」，切 Stage 時統一套用）。
兩條欄位各加一個 `UIPanel`，`visibleInStages` 列了除 Menu／Intro 以外的全部
（含 `None` —— 看地圖的時候也要能吃東西）。

> 這是交接文件「坑 3」的同一件事：**動手前先確認有沒有做過了。**
> 我第一版寫了一支新的 `StageVisibility`，發現 `UIPanel` 就是這個東西之後整支刪掉。

⚠️ `useFadeIfAvailable` 要**取消勾選** —— 淡入淡出歸 `EdgeRevealUI` 管，
兩支同時寫 `CanvasGroup.alpha` 會互相蓋掉。

### 事件盤點（2026-08-29 第四輪）

`weight = 0` 就是這個系統的「關掉」（`EventLibrary.Pick` 有 `if (e.weight <= 0f) continue;`）。
關掉的理由一律寫進該事件的 **Notes**。

| 事件 | 現況 | 為什麼 |
|---|---|---|
| 無人的小船 | **開** | 效果都接得上（釣竿 ＋ 侵蝕 5%） |
| 海市蜃樓 | **開** | 條件 `abyss ≥ 50`，一輪內幾乎不會出，不必額外關 |
| 損壞的祭壇 | ⛔ **關** | `GrantGodCard` 沒接，而且 `Stage_SpecialEvent` **就是同一場祭壇戲**、而且真的會給牌 —— 兩份重複，留著的那一份是壞的 |
| 喂米可吃飯 | ⛔ **關** | `DestroyWeaponCard` 沒接，條件「持有 20 張武器牌」測試一輪也達不到 |
| 暴食之深淵 | **開**，權重 100 → **1** | 100 會壓掉其他所有事件，第一個事件幾乎必定是它 |
| 好餓好餓的貪吃鬼 | **開** | 完整可玩（`StartBattle` 是**有接**的），而且是拿到「貪婪的大口」的唯一路 |
| 螺湮的祝福 | **開** | 條件 `killed_fish_priest` ＋ 30%，打贏菁英才有 |
| 門扉 | ⛔ **關** | `TeleportRandomNode` 沒接，開門之後什麼都不會發生 |

`globalChance` **維持 1**（每一站都會插一個事件）—— 那是測試值。
關掉三個之後，前幾站實際上只會從「無人的小船／暴食之深淵」兩個裡抽，
兩個都 `once`，抽完就沒有了，後面的節點會乾乾淨淨。要恢復隨機感就調回 0.35。

### 神牌一定拿得到嗎 —— 是，而且在任何戰鬥之前

⚠️ **神牌不是從事件給的。** 它走 `MapNodeKind.SpecialEvent` →
`Stage_SpecialEvent`（玩家挑一張，`PlayerVitals.AddCardToDeck` 進
`RunStateManager.savedDeck`）。事件裡那個 `GrantGodCard` 是死路，已經關掉。

demo 路線（`useDemoRoute = True`）：

```
0 Dialogue → 1 Event → 2 Shop → 3 SpecialEvent → 4 Combat → … → 9 Boss
```

**神牌在第 3 站，第一場戰鬥在第 4 站** —— 拿到神牌之前不會有任何戰鬥，
所以「打輸了就拿不到」這條路不存在。兩個選項（祈禱／無視）
都會給一張進戰鬥牌庫（螺湮 `octopus_god` ／ 戈厄忒 `goat_god`），選哪個都拿得到。

前置條件也確認過：`startingMaxHp = 100`、`startingDeck = StartingDeck_Default`，
所以 `PlayerVitals.IsReady` 成立，`AddCardToDeck` 不會被擋下來。

**唯一剩下的風險**：第 0~2 站被插播的事件如果抽到《好餓好餓的貪吃鬼》
且玩家選「不給他吃的」，會插一場戰鬥進來（那個效果是有接的），
打輸就走不到第 3 站。要完全排除的話就把貪吃鬼也關掉 —— 但那樣就拿不到貪婪的大口。

⚠️ **給 Romtyui**：`octopus_god` 的 `isGodCard` 沒有打勾（只有 `goat_god` 有）。
目前**全專案沒有任何程式讀這個欄位**，所以不影響玩，但兩張神牌的資料不一致。

### 地圖的背景與菁英節點

- 地圖是在 `SwitchStageInternal(StageType.None)` 之後才下拉的，
  那時場上一個 Stage 都沒有 → 直接看到相機的天空底色。
  現在 `MapOverlayController` 也掛 `StageBackdrop`（`Art_Village_Outdoor`），
  **下拉前生成、收完才 Despawn**（順序反了會看到背景閃一下）。
  已掛在場景的 `[MAP_OVERLAY]` 上。
- 菁英**不是**一種 `MapNodeKind`，是 Combat 節點的 `enemyTier` ——
  所以 `PrefabFor` 改成吃整筆 `RunNodeData`，不是只吃 kind。
  `Encounters_Village` 目前保證一場菁英（`fish_priest`），所以每張圖看得到一個。

---

## 現在的狀態

分支 `recovery-progress`，領先遠端一批 commit（還沒 push）。
組員的 `origin/Scene`、`origin/damege_test` 都已經合進來了。

**這一輪（依《食物》《收藏品》《對話》三份附件）做完的：**

| 做了什麼 | 在哪 |
|---|---|
| 食物 12 件、收藏品 17 件，全部照附件重建 | `Core/Items/` |
| 食物與遺物以外的道具封存（含撬棍、倉庫鑰匙、6 件舊食物） | `_Archive/Items/` |
| `ItemData` 補 `hpCost` / `sanCost` —— 附件有「減少 HP／SAN」的食物 | `Core/ItemData.cs` |
| 兩個對話事件（魔術秀／有…魚…！）做成資產 | `Core/ProbabilityDialogue/Data/` |
| 對話事件庫 ＋ 地圖對話節點接上機率對話 | `ProbabilityDialogueLibrary.cs`、`GameFlowManager` |
| 機率對話改用共用對話框：分頁、講完話才打牌、SpeechBubble | `ProbabilityDialogueView` |
| 戰利品表接回封存後的道具（25 條 TagQuery 全數驗過） | `Core/Loot/` |
| 機率成長公式改成乘法 `P ×= (1+牌面值/100)` | `ProbabilityCardRules.Apply` |
| 沒有效果的 18 件道具擋在隨機取得之外（`NoEffect` 標籤） | `Core/Loot/`、`Core/Items/` |

---

## ⛔ 立刻要確認的

1. **機率對話的回答列與手牌位置要用眼睛看一次** —— 對話框本身已經改成
   **直接用場景那個共用的 DialogueUI**（分頁、打字機、推進鍵、名字框、立繪全部一致），
   所以只剩回答列與手牌的座標要對。它們是照 `option_box`（0,-447.9）與
   `EncounterUI/HandRoot` 換算的，但**我沒有進 Play 模式看過**。
2. ~~長文不會分頁~~ **已解決** —— 走共用對話框，空行換頁，
   開頭「角色名：」的算說話、其餘算旁白。實測兩段各 4 頁。
   而且**話講完才會出現打牌**（`hidePlayUntilSpoken`）。
3. **`dialogueNodeStage`** —— `GameFlowManager` 上的新欄位，預設 `ProbabilityDialogue`。
   已在 Unity 裡確認生效。要整套換回舊的對話就改這一格，不必改程式。
4. **五間房的 slot 對位** —— 還是要人眼對著背景調，我做不了。

> `useDemoRoute` **維持開著**（使用者指示，測試中）。

---

## 待辦（依重要性）

### A. 機率對話的平衡 —— 已定案並實作

成長公式從**加法**改成**乘法** `P ×= (1 + 牌面值/100)`（2026-08-29 使用者定案）。
25% 用一張 100 的牌 → 50%；50% 再用一張 80 → 90%。

回答各收一種屬性時，最好的回答中位數落在 **50%**，只有 9.2% 的手能推到 100%
（加法時是 66.1%）。因此出現了真正的取捨：
全押想要的獎勵約 57% 拿到它，分散押注約 83% 至少拿到一個但拿到哪個由不得你。

**完整的觀察、實驗數據與死角（`P=0` 乘不動、難度旋鈕＝回答收幾種屬性）
全部記在 [DialogueProbability.md](DialogueProbability.md)。**

`Additive` 還留在 enum 裡（規格書的 T01~T16 是照加法寫的），新內容不要用。

### B. Dialog 版型 —— 已 1:1 沿用舊版

**問話直接走共用的對話框**（`PopupService` → 場景的 `DialogueUI`），
與探索打牌、開箱、商店是同一個框 —— 分頁、打字機（40 字/秒）、推進鍵、
名字框、立繪因此全部一致，不必再寫一份。

> 我先前的版本在 prefab 裡另做了一份對話框，那是錯的方向（分頁得再寫一次，
> 兩個框遲早長得不一樣）。已整組拆掉。

- **話講完才出現打牌**：`PopupService.OnAllClosed` 觸發才攤開回答列與手牌
- **SpeechBubble 三個時機都接上**：開場問候／判定反饋／收尾道別，
  與舊版 `DialogueStageController.Say()` 同一套（掛在立繪的點擊區上）
- **Stage 自己一個 Canvas（order 102）** —— DialogueUI 是 101、Canvas_Stage 是 100，
  不抬上去的話回答列會被壓黑蓋住。舊版是把 option_box 放進 DialogueUI 解決的，
  但 prefab 不能引用場景物件
- `PD_Answer` 的底圖用「對話框＿無字」，尺寸與字級照 `answer_1` 換算

細節與實測見 [DialogueProbability.md](DialogueProbability.md) 第 4 節。

### C／D. 食物與收藏品 —— 已做

全部細節在 [ItemsFromSpec.md](ItemsFromSpec.md)。這裡只留最要緊的兩句：

- **三筆戰鬥效果與專案原本不同**（貪婪的大口、人魚的畫像、螺湮御守），
  已依指示以附件為準，差異寫在那份文件的第 4 節。
- ⛔ **【反擊】【燃燒】【灰燼】在 `Assets/Romtyui/` 的程式與資料裡一個字都沒有** ——
  不是「還沒接」，是附件設計的機制戰鬥系統還沒有。光【反擊】就卡住 7 件道具。
  **要先跟 Romtyui 確認【反擊】的定義**，那是投資報酬率最高的一件。
- 其餘的分成「只差一個子類別」（護盾類、解除異常、羊羔 Token…）與
  「要先補觸發點」（`RelicsTriggerType` **沒有 BattleEnd**）兩級，
  完整分級在那份文件的第 5 節。
- 戰鬥端的**基礎建設其實做了不少**（Modifier 系統 12 型 × 8 運算、八種狀態、
  護盾、Token、遺物觸發已接進 BattleManager 四個時間點）——
  缺的是具體的效果資產與子類別。
- **沒有效果的道具已經擋在隨機取得之外**（`NoEffect` 標籤，18 件）。
  收藏品池因此只剩「人魚的畫像」一件 —— 效果補上去就會自己恢復。
  解禁的方式是重跑 `Tools/spec_rebuild/gate_no_effect.py`，見那份文件的第 8 節。
- **任務（事件）獲得的遺物照樣給** —— 貪婪的大口／人魚肉／螺湮御守／釣竿，
  四個事件的獎勵一個都沒動（2026-08-29 使用者定案：戰鬥端這一版來不及做，
  維持「任務獲得」與「有效果」的遺物）。
- **敘述拆成三欄**（`ItemData`）：`description` ＝ 只有效果文字（快捷欄 hover 顯示）、
  `fullDescription` ＝ 故事文本（給日後的圖鑑）、`notes` ＝ 製作備註（沒有 UI 會讀）。
  要顯示完整內容用 `ItemData.FullText`（故事＋效果接起來，**不要另存一份**）。
  33 件全部驗過：description 都以【效果】開頭、故事都在、備註不會外流。

### H. 手牌的卡面比例 —— 兩套手牌拉扯美術的程度不一樣

卡面美術是 **1331 × 2048（比例 0.650）**，但兩套手牌的框都不是這個比例，
而且都沒開 `preserveAspect`：

| | rect | 比例 | 對美術做了什麼 |
|---|---|---|---|
| **開寶箱** `EP_cardexplore_template` | 259 × 264 | 0.983 | 橫向拉寬 **51%** |
| 　↳ 卡框層／武器層 | 269 × 271 | 0.992 | 同上 |
| **對話** `PD_Card`（修之前） | 150 × 210 | 0.714 | 橫向拉寬 **10%** |
| 　↳ Artwork | 134 × 162 | 0.827 | 拉寬 **27%**，而且比卡框小一圈 |

對話那張還有第二個問題：**卡面與卡框是兩張同尺寸的全卡圖（都是 1331×2048）**，
本來要完全疊在一起，但 Artwork 被內縮成 134×162 → 兩層錯位，
畫面上就是彩色邊框歪一圈露在外面。

**已修（只動對話那張）**：`PD_Card` 改成 **137 × 210（0.652）**、
Artwork 拉成滿版與卡框重合、兩層都開 `preserveAspect`。
5 張 ×137 ＋ 4 段間距 ×12 ＝ 733，HandRoot 寬 820 放得下。

⚠️ **開寶箱那張沒有動** —— 它把同一張圖拉寬 51%，比對話那張嚴重得多，
但它是已驗收的探索流程，而且 `卡框層`／`武器層` 還要疊武器美術。
要兩邊看起來一樣的話，那張也要照 0.650 重調 ——**這是一個決定，不是順手改**。

### G. 背景（2026-08-29 使用者指示）

- **小屋／大屋／Room_Village_01 都改用中屋的背景**（`Art_Village_MiddleRoom`，
  scale 0.5 跟中屋一致）。它們原本各自掛著一張舊的 `30_3_0` sprite，已拆掉。
  美術補齊之後各自換回去就好。
- **對話節點的背景先用戶外那張**（`Art_Village_Outdoor`）——
  對話在大綱裡是「路上遇到同行的人」，室內背景對不上。
  由 `ProbabilityDialogueStageController.backdropPrefab` 在進場時生成、離場時收掉。

  ⚠️ 背景**不能放進 Stage 的 prefab 裡** —— Stage 住在 Canvas 底下，
  把 SpriteRenderer 塞進 RectTransform 會被父層縮放扭掉。
  所以是執行時掛到 `WorldRoot`（房間美術也是掛在那裡）。

- **五間房（含 Room_Village_Outdoor）現在全部都是中屋背景。**
- **事件節點也墊戶外背景** —— 事件本來沒有自己的場景，不墊就直接看到相機的天空底色。

#### 背景抽成共用元件 `StageBackdrop`

對話與事件都掛同一支（`Core/StageBackdrop.cs`），欄位在 Inspector 調：
`prefab` / `parentName`（預設 WorldRoot）/ `offset`（預設 **(0,1,0)**）/ `scale`（0.5）。

進場 `Spawn()`、離場 `Despawn()`。**一定要 Despawn** —— 不收的話背景會留到下一站。

#### ⚠️ 背景上緣會露出一條空白 —— 量出來的數字

房間美術在 prefab 裡是 `scale 0.5`，實際世界範圍是
**x −9.96 ~ 9.96、y −6.12 ~ 5.08**；
而相機（正交、size 5、位在 **y = 1**）看到的是 **x −8.89 ~ 8.89、y −4 ~ 6**。

→ **上緣差 0.92 個單位**，換算成 1080p 就是最上面約 78px 的天空色空白。
寬度是夠的，純粹是垂直位置不對（圖的中心在 y −0.52，相機中心在 y +1.00）。

對話的背景已改成**執行時對齊相機**（`fitBackdropToCamera`）——
量圖的實際範圍再算，所以換相機或換一張比例不同的背景都不會破。
驗算：對齊後 y −4.60 ~ 6.60，蓋得住相機的 −4 ~ 6 ✓（連縮放都不用改，只是位移）。

⚠️ **房間那邊是同一組數字**，所以理論上也有同樣的 0.92 空白。
如果現在探索的畫面看起來是滿的，那是在別的地方補過（可能還沒存檔）——
真的要治本，房間也套同一套對齊。

### I. UI 細節（2026-08-29 第三輪）

#### 半魚人事件的名字框

`EventData` 加了 `speakerNames`。內文與結果文字裡
**開頭是「半魚人：」的段落會改用有名字框的對話樣式**，其餘走旁白公版。
判定在共用的 `Core/SpeakerLine.cs`（機率對話的分頁也用同一支）。

⚠️ **一定要列名字，不能只看有沒有冒號** —— 旁白裡本來就有冒號
（「他看向你：那是一種請求」），只看冒號會讓名字框冒出奇怪的東西。

目前只有 `Event_hungry_glutton` 填了（`半魚人`）。其他事件要有名字框就照樣填。

#### 商店的 EXIT 兩張圖平滑替換

兩張圖只差左邊那格裡的小人：`EXIT_人`（未互動，小人在）→ `EXIT`（hover，小人跑了）。

做法是**兩張疊著淡入淡出**（`UI/Scripts/HoverCrossfadeImage.cs`），
不是換 sprite —— 換 sprite 是瞬間切換，在只差一個小元素的圖上會像破圖。

掛在**固定不動的感應區**（`ExitTab_Zone`）上，與 `SlideOutTab` 同一個物件。
兩支都只是接收 enter/exit，不會打架。
⚠️ 上層的 `raycastTarget` 必須是 false，否則感應區收不到 exit，淡入之後淡不回去。

#### 快捷欄改成點擊展開

`openOnHover` 關掉（場景的兩條都改了）。現在是**點邊緣才開**，收起來有三條路：

| 收起的條件 | 欄位 | 預設 |
|---|---|---|
| 滑鼠離開整條欄超過緩衝 | `closeAfterExitSeconds` | 0.4 秒 |
| 展開後完全沒互動 | `idleCloseSeconds` | 6 秒 |
| 點在快捷欄以外的地方 | `closeOnClickOutside` | on |

- **離開要留緩衝**：游標從一格移到另一格途中可能掠過欄位外幾個影格，
  0 的話欄位會在你正要點的瞬間關掉。
- **閒置那條是保險**：用鍵盤開的、或游標根本沒進來過時 `OnPointerExit` 永遠不會來。
- **「點外面」是輪詢滑鼠做的**，不是鋪一塊全螢幕的攔截層 ——
  鋪一塊會把商店貨架、對話選項全部擋掉。
  ⚠️ 走 `Mouse.current` 並判 null（坑 2）。
- **開著時點欄內空白不會收** —— 收起交給上面三條。
  不然玩家想點某一格卻點偏一點，整條就關掉了。

### F. 壁櫥／櫥櫃／爐灶還在用寶箱的圖 —— 待決定

**先釐清兩件容易誤會的事：**

1. **轉向已經有了。** 九個互動物件 prefab 的 `visualVariants` 都填了三個角度
   （正面／正側面／斜側面寶箱），slot 也有 ±12° 隨機旋轉。
   問題不是「沒有轉向」，是「用的是寶箱的圖」。
2. **美術場景裡並沒有畫好的壁櫥與爐灶。** 中間房拆成 BG ＋ 7 層，
   BG 是一間空房，1/5/6/7 是雜物與裝飾、4 是怪物，
   **只有 `3.png` 是真的容器（一個矮櫃）**。

所以「已經在美術場景有固定位置」目前只對**櫃子**成立。

| | 做法 | 代價 |
|---|---|---|
| A. 畫在背景上的家具 | slot 對準畫上去的櫃子，互動物件的 Image **alpha 調 0**（⚠️ 不是 `enabled=false`，那是坑 1），碰撞框與 hover 照常。畫面上看到的就是美術原稿 | slot 變成該房間專屬，要用 `SpawnSlot.contentTag` 綁死「這一格只放櫃子」 |
| B. 可擺放的家具 | 美術給去背 PNG，照現在的隨機擺放流程走 | 要 3 張新圖（壁櫥／爐灶／桌子） |

**建議 A 用在美術已經畫死的，B 用在還沒畫的** ——
否則會出現「畫上去的櫃子旁邊浮著一個寶箱圖示」這種矛盾畫面。

### E. 對話 —— 已做兩段，附件就只有兩段

附件《對話》只有【魔術秀】與【有…魚…！】。兩段都做好並接上地圖了。
附件標「未確定」的三件事我先各選了一邊，見 ItemsFromSpec.md 第 7 節。

---

## 這個專案的三個反覆出現的坑

**新對話接手的人一定要先讀這三條**，每一條都已經害我們踩過至少兩次。

### 坑 1：`frame` 綁的是根物件自己的 Image ＝ raycastTarget

`ShortcutSlotUI` 與 `ProbabilityCardUI` 都是這樣。
**用 `enabled = false` 讓它隱形，整個東西就點不到也拖不動。**
要隱形就調 alpha=0：畫面上看不見，但事件照收。這個坑出現過**三次**。

### 坑 2：舊的 `UnityEngine.Input`

專案已切到 Input System package。舊 API **編譯期完全看不出來**，
仍然編得過，但執行時會洗版 `InvalidOperationException`。
一律用 `Mouse.current` / `Keyboard.current`，而且要判 null。
`RunDebugPanel.cs` 開頭有完整說明。

### 坑 3：先確認組員有沒有做過了

已經重複造過兩套輪子（機率對話另做一套卡牌、快捷欄以為「使用道具」不存在）。
**動手前先 grep `Assets/Romtyui/`。**

---

## 工具上的注意事項

- **`read_console` 不可靠** —— 常常回傳 0 筆。真正的編譯錯誤要看
  `C:\Users\greyl\AppData\Local\Unity\Editor\Editor.log`（`grep "error CS"`）。
  ⚠️ **那份 log 是累積的** —— 一定要先記下行數，只看新增的那一段，
  否則會把幾天前修好的錯誤當成現在的。
- **手改 prefab／asset 的 YAML 是可行的，但有兩個雷**：
  · 新物件的 `fileID` 不要用接近 int64 上限的數字，跟既有的同量級就好
  · `m_Children:` 的序列後面**不可以留空行** —— Unity 會報
  「Transform child can't be loaded / 檔案可能損毀」，而且不會說是哪一個
- **`execute_code` 預設 CodeDom（C# 6）** —— 沒有區域函式、沒有 C# 7 以上語法，
  `foreach (var x in ...)` 的型別推斷會出事，**寫明確型別**，跨命名空間寫全名。
- **改完 C# 要 `refresh_unity` 之後再確認型別真的載入了。**
- **Unity 在 Play 模式時不要改場景物件**，退出時會被丟掉。改 prefab／asset 是安全的。

---

## 驗證的做法（這個專案的習慣）

不進 Play 模式，用離線腳本或 `execute_code` 跑統計驗證。已經這樣驗過的有：

- 地圖 500 張的連通性與不交叉
- 寶箱配額 1000 次分布
- 機率對話規格 T01~T16（16/16）＋ 重構後回歸 17 項
- 房間美術：28 個物件在兩台相機下的螢幕座標比對（最大誤差 0.00004）
- **戰利品表 27 條 TagQuery 全數抽得到東西**（`Tools/spec_rebuild/verify_loot.py`）
- **機率對話平衡 20000 手模擬**（`Tools/spec_rebuild/sim_dialogue.py`）

**能離線驗的就不要開 Play。**
但**版面與位置驗不了** —— 那一類（房間 slot 對位、對話框版型）只能用眼睛看。

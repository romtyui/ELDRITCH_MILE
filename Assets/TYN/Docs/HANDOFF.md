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

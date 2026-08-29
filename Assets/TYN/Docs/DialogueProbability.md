# 機率對話：觀察與實驗記錄

> 2026-08-29。這一份專門記「我們試過什麼、看到什麼、為什麼選現在這個做法」。
> 規則的實作在 `Core/ProbabilityDialogue/`，模擬腳本在
> `Tools/spec_rebuild/sim_dialogue.py`（在專案根目錄 `python` 執行）。

---

## 1. 環節的形狀（已確認符合企劃描述）

2026-08-29 使用者描述的規則，與現有實作**逐條相符**，沒有改動：

| 企劃這樣說 | 實作 | 在哪 |
|---|---|---|
| 卡片同時作用在同屬性的**複數**選項上 | `PlayCard` 掃過所有回答，屬性相符且還可用的**全部**推動 | `ProbabilityDialogueSession.PlayCard` |
| 不會拿到三個選項的獎勵 | 判定成功即 `RunOutcome` ＋ `State = End` | `SelectOption` |
| 判定失敗 → 該選項變灰、不可選 | `available = false` → `SetDisabled()` → 變 `disabledTint`、`OnPointerClick` 直接 return | `ProbabilityAnswerUI` |
| 只能嘗試其他選項 | 失敗後 `State` 回到 `CardPhase`，剩下的回答還能繼續打牌、繼續選 | `SelectOption` |

### 引擎實測（Gatekeeper，三個回答各收兩種屬性）

```
回答吃的屬性： beg = Id Ego ／ logic = Superego Ego ／ both = Id Superego

出牌前：           beg=25%  logic=25%  both=15%
打【自我 100】後：   beg=50%  logic=50%  both=15%   ← 兩個同時被推動
對 both 判定：骰 69 → 失敗（變灰）
再打【本我 40】後：  beg=70%  logic=50%  both=15%(灰) ← 失敗的不再被推動
狀態 = CardPhase                                    ← 還能繼續打其他選項
```

---

## 2. 成長公式：從加法改成乘法

### 觀察到的問題

規格書原本寫的是**加法**（`P += 牌面值`）。實測 30 場裡有 30 場，
最好的回答都能推到 100% —— 玩家從頭到尾沒有做過一個決定。

**病灶**：牌面那些 20~100 的數字是為**探索**設計的
（60 的牌 ＝ 這張牌有 60% 開得開箱子）。直接當百分點加上去太大，
一張 100 的牌自己就填滿一個回答。

⚠️ **交接文件曾經寫「套相剋表最貼合原設計」，那是錯的。**
現在不相符的牌加 0 分，套了相剋表變成加半價 —— 是**變簡單**，不是變難。
模擬證實：套相剋表之後 100% 的手都能推到滿。

### 定案的公式

```
P ×= (1 + 牌面值 / 100)
```

使用者給的兩個例子，實作與模擬都對得上：

| | 結果 |
|---|---|
| 25% 用一張 **100** | 25 × 2.0 ＝ **50%** ✓ |
| 50% 用一張 **80** | 50 × 1.8 ＝ **90%** ✓ |

⚠️ **乘法的死角：`P = 0` 時任何牌都推不動**（0 乘任何數還是 0）。
所以 `baseProbability` 不可以填 0 —— `Session.Begin()` 會在 Console 警告。

⚠️ **每一張牌各自四捨五入一次**，不是最後才進位 ——
玩家看得到每張牌打完的數字，累到最後才進位的話畫面與真值會對不上。

### 20000 手模擬（回答**各收一種屬性**，牌組 15 張、手牌 5 張、基礎 25%）

| 設定 | 平均 | 中位 | ≥90% | ＝100% | 分兩路 EV |
|---|---|---|---|---|---|
| 加法（規格原本） | 85.6% | 100% | 66.1% | **66.1%** | 99.2% |
| 加法 ＋ 牌值 ×0.25 | 49.9% | 50% | 0.7% | 0.1% | 75.9% |
| **乘法 1+x（定案）** | **56.7%** | **50%** | 14.6% | **9.2%** | **83.3%** |
| 乘法 ＋ base 15 | 35.3% | 30% | 1.1% | 0.7% | 59.3% |
| 乘法 ＋ 相剋表 | 77.9% | 81% | 41.4% | 32.1% | 94.7% |

**為什麼選乘法而不是「加法 ＋ 牌值 ×0.25」**：兩者的平均值很接近，
但乘法是玩家算得出來的（「翻倍」「加八成」），而 ×0.25 是一個
「牌面寫 100、實際加 25」的隱形換算 —— 畫面上的數字會騙人。

### 這個公式帶出來的取捨（本來沒有的）

- **全押想要的那個獎勵** → 約 **57%** 拿到它；失敗就沒牌了，事件收掉
- **分散押兩個回答** → 約 **83%** 至少拿到一個，但**拿到哪一個由不得你**

這正是企劃要的：不會三個獎勵都拿到，而且「要賭哪一個」變成一個真的決定。

### 難度旋鈕：一個回答收幾種屬性

同一套公式下，回答收的屬性愈多愈簡單（相符的牌變多）：

| 回答收幾種屬性 | 平均 | ＝100% |
|---|---|---|
| 1 種（魔術秀／有…魚…！） | 56.7% | 9.2% |
| 2 種（Gatekeeper） | 89.0% | 60.6% |

**新內容預設寫一種屬性。** 要做「簡單的對話」再給兩種。
`PDialogue_Gatekeeper` 是舊的測試資料，維持兩種屬性當作對照組。

---

## 3. 目前的設定值

| 欄位 | 值 | 在哪 |
|---|---|---|
| `growth` | `Multiplicative` | 三個 PDialogue 資產都是 |
| `baseProbability` | 25 | 魔術秀／有…魚…！ 的三個回答 |
| `handSize` | 5 | |
| `probabilityCap` | 100 | |
| 牌組 | 3 屬性 × {20,40,60,80,100} ＝ 15 張 | `Stage_Explore` / `Stage_Dialogue` 的 startingDeck |

`Additive` 還留著（enum 的 0），因為規格書的 T01~T16 是照加法寫的。
新內容不要用。

---

## 4. 對話框：1:1 沿用舊版那一套

2026-08-29 使用者要求「講完話才出現打牌」，並且排版、比例、程式配置都 1:1 復刻舊版。

### 做法：驅動**共用的**對話框，不再自己做一個

問話走 `PopupService` → `DialogueBoxUI`（場景裡的 DialogueUI），
與探索打牌、開箱、商店用的是**同一個**對話框。所以分頁、打字機（40 字/秒）、
推進鍵、名字框、立繪全部免費一致 —— 不必在這裡再寫一份，也不會出現兩個長得不一樣的框。

> 我先前的版本在 prefab 裡另做了一份對話框。那是錯的方向：分頁得再寫一次，
> 而且兩個框遲早會長得不一樣。已經整組拆掉（`rewire_pd_shared_box.py`）。

### 分頁規則

**空行 ＝ 換頁。** 附件的一段對話本來就是一句一段，照抄進資產就自然分好了。

每一頁再判斷是誰在講：開頭是「角色名：」的走 `ShowSpeech`（有名字框），
其餘走系統提示的公版（旁白）—— 這是舊版 `ChoiceStageController.Line.speaker`
留空與否的同一套規則。實測：

```
== 魔術秀（坎貝爾）共 4 頁 ==
  第1頁 [說話 坎貝爾] 「好，來表演魔術吧！」
  第2頁 [說話 坎貝爾] 「別一副『這種時候？』的表情嘛！…
  第3頁 [說話 坎貝爾] 「那麼，問題～！…
  第4頁 [說話 坎貝爾] 「帽子中可謂是混沌！…

== 有…魚…！（時藏）共 4 頁 ==
  第1頁 [說話 時藏] 「這裡...有魚。」
  第2頁 [旁白]       路上，時藏突然停了下來。…
  第3頁 [說話 時藏] 「來抓...魚！我...很擅長...餓...要吃！」
  第4頁 [旁白]       時藏看向你，似乎是在詢問你的意見。
```

### 講完話才出現打牌

`PopupService.OnAllClosed` 一觸發才把回答列與手牌打開（`hidePlayUntilSpoken`）。
失敗後 NPC 再說一段時**一樣先收起來**，講完再攤開 ——
不收的話玩家會在對話跑到一半時繼續出牌，兩件事在搶注意力。

⚠️ Stage 的「等玩家點擊才離開」也要先等話講完（`PopupService.IsIdle`），
不然翻第一頁那一下就會被當成「我看完了」，後面幾頁玩家根本看不到。

### SpeechBubble：三個時機都接上了

與舊版 `DialogueStageController.Say()` 同一套 —— 有立繪點擊區就掛在它身上
（氣泡跟著立繪跑），沒有就退回畫面上的預設位置。

| 時機 | 台詞來源 |
|---|---|
| 開場 | `CharacterData.PickGreeting` |
| 判定完 | `PickSuccessLine` / `PickFailureLine` |
| 收尾 | `PickFarewell`（**waitForCurrent = true**，不然會把剛冒出來的判定反饋刷掉） |

挑台詞的亂數**不綁 run 種子** —— 綁了同一場 run 每次都聽到同一句。

場景端已確認：`PopupService`（Canvas_Popup）、`DialogueUI`、`Advance Button`、
`SpeechBubble`、`Speaker_Hitbox`（registerAsSceneSpeaker，bubbleAnchor 已接）都在。

### 為什麼 Stage 自己一個 Canvas

DialogueUI 的 `sortingOrder` 是 **101**，Canvas_Stage 是 **100** ——
回答列與手牌留在 Stage 底下會被 DialogueUI 的壓黑蓋住。

舊版是把 option_box 與 EncounterUI **放進 DialogueUI 裡**解決的，
但 prefab 不能引用場景物件。所以改成「Stage 自己一個 Canvas、
`overrideSorting`、order **102**」＋ `GraphicRaycaster`：效果一樣，
而且 Stage 仍然自帶一份、不依賴場景結構。

---

## 4b. 接上共用對話框時踩到的三個坑

都是 2026-08-29 使用者回報後查出來的，成因都不在「版面數字」而在時序。

### 坑 A：手牌疊在原點、hover 回不來

`SetPlayVisible` 原本是 `handRoot.gameObject.SetActive(false)`。
**Layout 不會在停用的物件上跑** —— 於是在隱藏狀態下生成的卡片，
`ProbabilityCardUI.Bind()` 結尾抓到的 `homePosition` 全是 prefab 的原始值 (0,0)。
結果整手牌疊在容器原點，拖回去也回到原點。

這就是交接文件「坑 1」與 `ShortcutSlotUI` 修過的同一件事。兩邊都補了：

- `SetPlayVisible` 改用 **CanvasGroup**（alpha 0 ＋ blocksRaycasts false），
  物件保持啟用 → Layout 照跑
- `ProbabilityCardUI` 的 `homePosition` 改成**第一次要用到才抓**（`EnsureHome`），
  跟 `ShortcutSlotUI.EnsureHome` 同一個做法

> **要隱形就調 alpha，不要停用。** 這條在這個專案已經出現第四次了。

### 坑 B：開場白整段播兩輪

`Session.Begin()` 的結尾是三發連著跑：

```
OnStarted → OnHandChanged → OnPromptChanged(0, initialPrompt)
```

我在 `HandleStarted` 也講了一次開場白 —— 於是 `OnPromptChanged` 再講一次，整段播兩輪。
**開場白與失敗後的問話走的是同一條路**，交給 `HandlePromptChanged` 一支處理就好。

### 坑 C：改到錯的那一個 LayoutGroup

把 AnswerRoot 從 Vertical 換成 Horizontal 的那支腳本，
是用 `find('HorizontalLayoutGroup')` 定位的 —— 但 **HandRoot 的排在檔案前面**，
所以對齊與間距改到了 HandRoot（幸好它的值不同，沒被改壞），
AnswerRoot 則停在 Vertical 時代的 LowerLeft ／ spacing 14。

現在：AnswerRoot ＝ MiddleCenter／0，HandRoot ＝ LowerRight／12。
**改 prefab 的 YAML 要用「找到那個 GameObject 再改它的元件」，不要用字串位置。**

---

## 4c. 手牌排版與 hover（2026-08-29 第二輪）

### 先回答「為什麼原本房間沒有空白」

**因為房間 prefab 的根本來就擺在 y = 1，剛好等於相機中心。**

```
Room_Village_*  prefab 根 localPosition = (0, 1.00, 0)
相機            正交、size 5、位在 (0, 1, −10) → 可視 y −4 ~ 6
房間背景（scale 0.5）實測世界範圍 y −5.12 ~ 6.08   → 蓋得住 ✓
```

我原本把對話背景擺在 `y = 0`，整張圖就低了 1 個單位 → y −6.12 ~ 5.08，
上緣差 0.92（1080p 約 78px），那條天空色就是這樣來的。

**不是縮放問題，是位置問題。** 所以修法是照房間的擺法用 `(0, 1, 0)`，
**不是**讓程式自己把圖置中 —— 取景是美術一格一格調過的
（commit「房間美術重建：對齊美術原場景的實際畫面」），程式置中會把那份調校蓋掉。
現在只在「真的蓋不滿」時於 Console 提醒，不動它。

### hover 與選取：之前確實沒有

`ProbabilityCardUI` 原本只有 `IPointerClick` / `IBeginDrag` / `IDrag` / `IEndDrag` ——
**沒有 `IPointerEnter` / `IPointerExit`**，所以滑過去不會有任何反應。現在補上了。

至於「選取」：機率對話**沒有兩段式出牌**。一張牌會同時作用在所有屬性相符的回答上，
沒有「要打在哪一個目標」可選，所以不需要探索那邊的 `selectedLift`。
hover 的效果是：**卡片上浮 ＋ 同屬性的回答亮起來**（沿用既有的 `OnAimChanged`）。

### 排版：照抄 `ExploreHandUI.Layout()`

| | 探索（開寶箱） | 對話 |
|---|---|---|
| 卡寬 | 259 | 137 |
| 間距 | 140（重疊 46%） | 100（重疊 **27%**） |
| 最大寬度 | 900 | 760 |
| 對齊 | 置中 | 置中 |
| 疊放 | 左→右，**右側在上** | 同左 |
| 上浮 | hover 40／選取 70 | hover 40（沒有選取） |

實測 5 張：x = −200 / −100 / 0 / 100 / 200，整排 −269 ~ 269（HandRoot 寬 820），
相鄰重疊 37px ＝ 27%，最右邊那張 sibling index 最大 → 在最上層 ✓

三個從探索那邊繼承過來的關鍵決定（都寫在 `LayoutHand()` 的註解裡）：

1. **根物件永遠在 y = 0，上浮只動 `__Visual` 層。**
   把根物件往上移的話游標會被抽走 → exit → 落下 → enter，卡片下緣會瘋狂閃爍。
2. **疊放順序不隨 hover 改變。** 把 hover 的牌提到最上層的話，整排的前後關係
   會在滑鼠掃過去時一直重排，看起來像在跳。狀態變化只由**高度**表達。
3. **間距比卡片窄 ＝ 略有重疊**，那正是實體手牌攤開來的樣子。

### 為此動到的結構

- `PD_Card` 的**卡框從根物件搬到 `Frame` 子物件**。
  根的 Image 留著當**透明的點擊區**（alpha 0、raycastTarget on）——
  ⚠️ 不可以停用，停用就點不到也拖不動（坑 1）。
  這樣 `__Visual` 那層才連卡框一起浮起來。
- `HandRoot` 的 **LayoutGroup 拆掉** —— `LayoutHand()` 自己算位置，兩邊會打架。

### 之後要「統一使用對話這邊的排版」

`LayoutHand()` 與 `ExploreHandUI.Layout()` 現在是**兩份一樣的程式**。
要統一的話建議抽成一支共用的（例如 `HandFanLayout`），
把 `cardSpacing` / `maxHandWidth` / `hoverLift` 當參數傳進去 ——
兩邊卡片寬度不同（259 vs 137），所以數字仍要各自給，但規則只留一份。

---

## 4d. 手牌統一（2026-08-29 定案）

### 為什麼會不一樣

**兩張卡片 prefab 是各自手工做的兩份同一種卡。** 它們畫的是完全同一組圖：

```
explore_Id_100 → visualData = explore_Id_100_vis
   artworkSprite   = 機率牌100無框  1331×2048
   cardFrameSprite = 機率牌框紅     1331×2048
```

| | 承載卡框 | 承載卡面 | 原本的 rect | 對 0.650 的圖做了什麼 |
|---|---|---|---|---|
| 探索 `EP_cardexplore_template` | `卡框層` | `武器層` | 269 × 271 | 橫向拉寬 **51%** |
| 對話 `PD_Card` | `Frame` | `Artwork` | 137 × 210 | 我單獨修過，反而拉得更開 |

### 定案：共用卡面 prefab ＋ 共用排版

| 統一的東西 | 現在在哪 |
|---|---|
| 卡面 | `EP_cardexplore_template`，**兩邊共用**。176 × 271（0.649），兩層都開 preserveAspect |
| 排版規則 | `EldritchMile.UI.HandFanLayout.Arrange()`，兩邊共用 |
| 視覺層 | `HandFanLayout.BuildVisualRoot()`，兩邊共用 |
| 間距／最大寬度／上浮 | 130 ／ 760 ／ 40，兩邊同值 |

5 張的實測：x = −260 / −130 / 0 / 130 / 260，整排 −348 ~ 348（696 寬，HandRoot 820），
相鄰重疊 46px ＝ **26%**，最右邊 sibling index 最大 → 在最上層。

### ⚠️ 互動元件不能做進共用的卡面

`ExploreCardDrag` 與 `ProbabilityCardUI` **都是輸入處理器**，
而 Unity 是把事件送給物件上的**每一個**處理器，不是只送第一個。
兩個同時在的話會「點一下出兩次牌」或「牌被標成 spent 之後再也打不出去」。

共用的 prefab 上**本來就掛著 `ExploreCardDrag`**（探索那邊做進去的），
所以對話端在 `AttachCardUI()` 裡會先把它關掉並 Destroy，再掛自己的
`ProbabilityCardUI`，並把圖層引用從 `CardViewUIExplore.artworkImage / cardFrameImage`
接過來（**不要用名字 Find** —— 美術改個物件名就會靜靜地壞掉）。

### 牌面數字不再另外疊一個

`機率牌100無框` 那張圖**本身就印著 100**。舊的 `PD_Card` 又疊了一個 `Value` 文字，
所以同一張卡上會有兩個數字。共用之後 `valueText` 設成 null，只留美術上的那個。

### `PD_Card.prefab` 已經沒有人用

保留在原地當參考，但**不要再改它** —— 改了也不會反映到遊戲裡。

---

## 5. 還沒做的

- **版面要用眼睛驗一次**。回答列與手牌的座標是照 DialogueUI 的
  `option_box`（0,-447.9，三格中心 -667/-309/49）與 `EncounterUI/HandRoot` 換算的，
  但**我沒有進 Play 模式看過**。
- ⚠️ **卡面尺寸改了會同時影響探索**（現在是共用的）——
  探索的卡從 269×271 變成 176×271，那個畫面會變窄，要一起看。
- **手牌用完但還有回答沒試**：玩家還是可以直接選（用當下的機率），不會卡住，
  但畫面上沒有提示說「你已經沒牌了」。
- ~~hover 上浮~~ **已補**（見 4c）。`HoverRaiseLayer`（整個手牌區在拖曳時提到最上層）
  仍然沒有搬過來 —— 那是解決「拖曳中被對話框蓋住」的，對話這邊還沒遇到。
- **氣泡台詞有缺**：`Say()` 拿到空字串就不出現，所以
  時藏（successLines／failureLines／farewells 都是 0 筆）判定完與收尾都不會有氣泡，
  坎貝爾則是缺 farewells。要有氣泡就把台詞填進 `CharacterData`。

# Phase 7 — 商店、資料庫與對話氣泡

> 建立：2026-08-16 · 編譯 0 error 0 warning · 資料抽取已離線驗證（見 §6）
>
> 這一批回答的是三個問題：**東西存在哪、誰決定出現什麼、角色怎麼講話。**

---

## 0. 商店的設定（企劃提供，2026-08-16 補上）

> `克蘇魯劇情大綱.docx` 的檔案裡**只有〈角色〉一節**（全文 3,943 字），沒有商店段落。
> 〈商店〉是企劃另外貼過來的。日後要找這份設定，看這裡或問企劃，不要去翻那個 docx。

**由一個裝扮怪異的商人開設的移動商店**，販賣稀奇古怪的**收藏品**，這些藏物貌似能提供特殊的力量。

| 設定 | 對系統的意義 |
|---|---|
| **移動商店** | 是同一個商人在各地跑，不是每區一個店主 |
| 原型：**冷原人** | —— |
| 「收藏品之間的**平衡**是很重要的」 | 收藏品之間會有相互作用（像 Slay the Spire 的遺物）。**這套機制還不存在** |
| 「我家的**老闆**」＝坎貝爾 | 商人是坎貝爾（奈亞拉托提普）派的。彩蛋條件依賴「隊伍」系統 |

### 商品有三類（用詞照坎貝爾在教學裡說的）

> 坎貝爾：消耗品、武器...以及收藏品。

| 類別 | 標籤 | 狀況 |
|---|---|---|
| **武器** | `Weapon` | 武器在這個世界會變成卡牌。**佔位 4 張** —— 真身在 Romtyui 的牌組系統 |
| **消耗品／食物** | `Consumable` `Food`（漁獲另有 `SeaFood`） | 6 件。漁村會偏漁獲 |
| **收藏品** | `Curio` + 品質 `Common`/`Uncommon`/`Rare` | **8 件，全部照大綱〈收藏品〉表**，含敘述與效果 |

> 「收藏品只要在身上持有就能發揮效果」—— 是被動的，不會被消耗。

### 移動商店 ≠ 各地賣一樣的東西

商人到處跑，但**他在漁村補的貨自然會是漁獲多一點**。所以貨表**依區域替換**：

| | 武器 | 食物 | 收藏品 | 食物的內容 |
|---|---|---|---|---|
| `Loot_MerchantShop`（預設） | 3 | 2 | 3 | 一般糧食 70 / 漁獲 30 |
| `Loot_MerchantShop_Village`（漁村） | 2 | **4** | 2 | **漁獲 80** / 一般糧食 20 |

兩張表都用 `Table` 條目指回**同一組共用子表**（`Loot_Sub_Weapons` / `Loot_Sub_Relics`），
所以「只有食物不同」寫起來很短，但需要時又能整組改配比
（例如礦山金幣多、遺物也多 —— 加一張表就好，程式不用動）。

> ⚠️ 區域現在是借用節點的 `contentId` 比對的，**資料模型裡還沒有「區域」欄位**。
> 這是刻意留的接縫：區域系統做好之後只要改 `ShopStageController.ResolveStockTable()`，
> 表與 Inspector 的設定都不用動。

### ⚠️ 我先前推錯的，已經改掉

上一版我以為店主是**時藏**、商品是**漁獲** —— 因為當時只有〈角色〉可讀，
而時藏的設定是「關聯場景：港口、漁村」＋「隨機獲取漁獲」。
拿到〈商店〉之後改正：店主換成商人（冷原人）。

中間我一度把食物整個移出商店，那也是錯的（商品本來就有卡片＋食物＋遺物三類），已經補回去。
**沒有任何道具被刪掉過**，只是換了哪張表在用它們。漁獲多了 `Food` 標籤，
因為商人自己說了：

> 「漁村的傢伙，對吃的特別執著...或許是這個的影響，那裡也更容易找到糧食。」

### 從閒聊裡撈到的兩件系統資訊

**① 區域清單**（閒聊裡一句一個，這是目前唯一的來源）

| 區域 | 商人說的 | 對系統的意義 |
|---|---|---|
| 漁村 | 更容易找到糧食 | `Food` 掉落權重高 |
| 教堂 | 不會主動攻擊，但**賦予麻煩的效果** | 敵人以 debuff 為主 |
| 劇場 | 「絕讚炎上中」，要做好**恢復**措施 | 持續傷害 |
| 森林 | **數量最多**，一次應付大量敵人 | 多體戰 |
| 礦山 | **可以搞到一大把金幣** | 貨幣主要來源 |
| 牧場 | 五顏六色、**很擅長偽裝** | —— |

**② 貨幣叫「金幣」，主要產地是礦山。** 商店的錢已經照這個顯示（`金幣 {0}`）。
開局多少、掉落多少還是沒定，`startingMoney` 佔位 120（約買得起 3 件收藏品）。

### 台詞

**問候 3 句、成交 3 句、閒聊 13 句、彩蛋 3 句，全部照原文一字未改**，
存在 `Character_merchant.asset`。

⚠️ 大綱的【觸發商店事件】另有一段**第一次進商店**的教學對白（商人＋坎貝爾一來一往，
還帶出「硬幣」與三類商品的說明）。那是**有腳本的劇情**，不是隨機寒暄，
所以**沒有**塞進 barks —— 它應該走對話框，等教學流程做的時候再接。

⚠️ **貨幣的名字大綱裡有兩種**：坎貝爾說「撿到一些**硬幣**」，商人說「一大把**金幣**」。
目前 UI 顯示「金幣」。要統一的話跟文案講一聲，改一個字串就好。

---

## 1. 你問的：會不會有「庫」來管房間／物品／NPC？

**會，而且業界就是這樣做的。** 專案裡本來就有一半了：

| 庫 | 管什麼 | 狀態 |
|---|---|---|
| `RoomLibrary` | 節點 → 房間 prefab | 早就有 |
| `RoomContentData` | 房間裡生成什麼（權重） | 早就有 |
| `ItemDatabase` | id → 道具 | 早就有，這批補上**價格與標籤** |
| `LootTable` + `LootService` | **誰在什麼場合會出現什麼** | 🆕 這批 |
| `CharacterDatabase` | id → 角色（名字／立繪／寒暄） | 🆕 這批 |

業界的說法叫 **data-driven registries**：商品與房間不寫死在程式裡，
而是放在引擎原生的資料格式（Unity 就是 ScriptableObject）裡，開遊戲時由管理器讀進來。
好處不是「比較優雅」，是**企劃改數值不用叫工程師、不用重編譯、不會在 .cs 上跟人衝突**。

---

## 2. 你問的：漁村商店怎麼做到「以漁獲為主，但也有機會出別的」

### 兩個層次，分清楚就不會亂

```
物品的 tags  說「它是什麼」          魚 = [Consumable, Food, SeaFood]
LootTable    說「這個場合會出什麼」   漁村的食物格 = 80% 漁獲 / 20% 一般糧食
```

**權重寫在表上，不寫在物品上。** 這是這一節唯一重要的一句話。

如果權重寫在物品上（「魚的稀有度＝常見」），那條魚在全世界永遠一樣常見 ——
你沒辦法說「魚在漁村很常見、在內陸城市幾乎沒有」。
表驅動就沒這個問題：換一張表而已。

### 現在的移動商店長這樣

一張貨表 = 三個池子，各自負責一類商品、各自抽固定張數：

```
Loot_MerchantShop（預設）
├─ 池「武器 x3」    → Table → Loot_Sub_Weapons  → TagQuery [Weapon]
├─ 池「食物 x2」    → Table → Loot_Sub_Food_Generic
└─ 池「收藏品 x3」  → Table → Loot_Sub_Relics   → 依品質 50/33/17

Loot_MerchantShop_Village（漁村）
├─ 池「武器 x2」    → Table → Loot_Sub_Weapons      ← 共用同一張
├─ 池「食物 x4」    → Table → Loot_Sub_Food_Village ← 只有這裡不同
└─ 池「收藏品 x2」  → Table → Loot_Sub_Relics       ← 共用同一張
```

**「每類固定幾格」是用池子表達的，不是用權重。** 權重是「這一格比較可能是什麼」，
池子是「這一類保證有幾格」。想要「一定有 3 件武器」就得用池子 ——
把武器權重調高只能讓武器變多，沒辦法保證數量。

實測：預設永遠是 武3 食2 藏3、漁村永遠是 武2 食4 藏2，漁村的 4 格食物裡通常 3–4 格是漁獲；
收藏品品質抽 300 次是 160 / 91 / 49（期望 150 / 99 / 51）。

### 三種條目的差別

| 種類 | 用在 |
|---|---|
| **Item** | 指名。「這間店一定要有撬棍」 |
| **TagQuery** | 一整群。「任何漁獲」。加新道具時**不用回頭改表**，這是它存在的全部理由 |
| **Table** | 轉去抽另一張表。共用的雜物池只寫一次，十張表共用 |

---

## 3. 你問的：寶箱的 tier 到底怎麼分、分多細

### 結論：**分「表」，不分「物品」**

一個寶箱身上不放 tier 數字，放一張 `LootTable` 的引用。

```
漁村普通寶箱 → Loot_VillageChest_T1
漁村精英寶箱 → Loot_VillageChest_T2
```

`ChestInteractable.cs` 一個字都不用改。難度、區域、稀有度全部是「換一張表」。

**為什麼不給物品標 tier**：同一條鹹魚在漁村是像樣的補給、在第四區是雜物。
把階級寫死在物品上，等於宣告它在整個遊戲裡永遠一樣珍貴，第二個區域就會壞。

### 但「品質」是另一回事 —— 這點我要說清楚

大綱的〈收藏品〉表有**品質**欄位（普通／罕見／稀有），這跟上面那段不衝突，
因為它們是兩件不同的事：

| | 屬於誰 | 意思 |
|---|---|---|
| **品質**（普通／罕見／稀有） | **物品** | 「這東西本身有多好」。人魚肉永遠是稀有，這是它的身分 |
| **掉落率** | **表** | 「這個場合有多容易拿到稀有的」。第一區 5%、最終區 30% |

Slay the Spire 就是這樣切的：卡有 Common/Uncommon/Rare 的身分，
但**先決定這一抽要出什麼稀有度、再從那個稀有度的池子裡挑** —— 機率在表上。

所以品質做成標籤（`Common` / `Uncommon` / `Rare`），
`Loot_Sub_Relics` 則用三個依品質過濾的條目來配比 —— 目前是 **50 / 33 / 17**
（Slay the Spire 遺物的 3:2:1，先照抄當起點）。要調難度改這三個數字，不用碰物品。

### 分多細？三到四階，不要更多

| 遊戲 | 階數 |
|---|---|
| Slay the Spire | Common / Uncommon / Rare（3）+ 商店限定 |
| Diablo 系 | TreasureClass 分很多階，但那是**掉落表**的階，不是物品的階 |
| Minecraft | 沒有物品階級，只有 loot_table 檔案 |

專案已經有四階的詞彙了（`RetryCostData.ObjectTier` 的 Tier1–Tier4）。
**沿用同一套字，不要再發明第二套。**

### 一張表的形狀（T1 現況）

| 池 | 機率 | 抽幾次 | 內容 |
|---|---|---|---|
| 保證主獎 | 100% | 1 | 漁獲 50 / 補給 35 / 撬棍 15 |
| 雜物 | 30% | 1–2 | 補給 |

T2 是同一個形狀，只是主獎更好、雜物保證有 2–3 件。
**這就是「難度」在資料上的全部樣子** —— 沒有第二套機制。

---

## 4. 你問的：手遊那種「點角色會有對話氣泡」

### 業界把角色講話分成兩種，不要混

| | 觸發 | 特性 |
|---|---|---|
| **Bark（寒暄）** | 進場自動、點一下 | 沒有選項、可以隨機、漏看沒差 |
| **Dialog Tree（對話樹）** | 走劇情 | 有分支、有狀態、要記玩家選了什麼 |

Pixel Crushers 的 Dialogue System、Yarn Spinner 都是這樣切的。
混在一起的下場是隨機寒暄插進劇情中間，或劇情被寒暄蓋掉。

專案的對應：

```
Bark        → SpeechBubbleUI（🆕）      商人的「歡迎光臨～要來買些什麼嗎？」
Dialog Tree → DialogueBoxUI（既有）     排隊播放、可帶選項與判定
```

### 台詞的四個槽，剛好對上企劃的分段

企劃給的商店對白分成【進入商店】【購買商品】【閒聊】＋彩蛋，
`CharacterData` 就照這個形狀存：

| 企劃的段 | 欄位 | 挑法 |
|---|---|---|
| 進入商店 | `greetings` | **隨機**。每次進店講同一句很快就膩 |
| 購買商品 | `purchaseLines` | **隨機**。而且亂數**不能綁 run 種子** —— 綁了同一間店買三次會聽到同一句 |
| 閒聊 | `chatter` | **照順序輪，不隨機**。隨機的話連點會撞到同一句，看起來像壞了 |
| 彩蛋（坎貝爾不在隊伍） | `conditionalChatter` | 條件成立時**混進閒聊池一起輪** |

「閒聊」文件寫的是「在商店頁面**待機**／點擊商人」—— 所以待機也算觸發：
`CharacterHitbox.idleChatterSeconds` 沒人理他就自己講一句。
**點擊與待機共用同一個輪播索引**，不會各輪各的而重複。

> ⚠️ **待機說話目前是關的**（`idleChatterSeconds = 0`，組員的決定）。
> 機制留著，要開回來把秒數填上去就好。

### 氣泡的動效（2026-08-16 依組員回饋改）

氣泡不再是「淡入之後一直掛在那裡」，改成**冒出來 → 停幾秒 → 自己消失**：

| 參數 | 值 | 作用 |
|---|---|---|
| `popInSeconds` | 0.22 | 冒出來 |
| `popOvershoot` | 0.12 | **衝過頭再回來** —— 這就是「氣泡感」的來源 |
| `popFromScale` | 0.8 | 起始縮放。太小會像從一個點長出來 |
| `autoHideSeconds` | 4 | 講完幾秒自己消失。**0 = 常駐**（開場白也照這個走） |
| `popOutSeconds` | 0.12 | 縮回去。**比冒出來短** —— 消失拖太久會擋住下一句 |

**換句話時是「先縮回去、再彈出來」，不是直接換字。**
直接換字的話玩家常常沒發現內容變了 —— 一顆一直掛在那裡的氣泡，字換了跟沒換看起來一樣。
所以 `Show()` 在氣泡還在畫面上時，會先播縮回去，把新的內容存成 pending，
等縮完了才套用並重新彈出。點角色、買東西都走這條路。

轉場要用 `HideImmediate()` 而不是 `Hide()` —— 後者會播縮回去的動作，
但 Stage 那一刻就要卸載了，動作播不完，殘影會被帶到下一個畫面。

> ⚠️ 彩蛋的條件「坎貝爾不在隊伍內」**現在沒有人在判斷** ——
> 「協助者／隊伍」這個系統還不存在（見 [SystemsStatus.md](SystemsStatus.md) §3）。
> 目前是在 `CharacterHitbox.activeConditions` 手填 `campbell_absent`（已經填了，測得到）。
> 隊伍系統做好之後，改成由程式填這個清單即可，台詞資料不用動。

### 角色畫在背景上，怎麼點得到

商店的店主不是獨立物件，他是背景圖的一部分 —— 沒有 Collider、沒有 Button。
標準做法是在他身上蓋一塊**透明但收得到 raycast** 的 Image：

```csharp
image.color = new Color(r, g, b, 0f);   // 看不見
image.raycastTarget = true;             // 但點得到
```

> ⚠️ 這裡有個一字之差的坑：`image.enabled = false` 會讓它**收不到任何 raycast，而且不報錯**。
> 症狀是「點了完全沒反應」，很難查。所以 `CharacterHitbox.Awake()` 自己把這兩個值設好，
> 不靠人在 Inspector 記得。

### 氣泡的位置：這才是真正要解的問題

角色可能在世界空間，氣泡永遠是 UI，兩邊座標系不同。所以每幀做：

```
錨點的世界座標 → 螢幕座標 → 氣泡父物件的區域座標
```

**不能把氣泡設成角色的子物件**：角色若在世界裡，UI 掛不上去；
角色若是背景圖的一部分，氣泡會跟著背景被 CanvasScaler 縮放而變形。

`SpeechBubbleUI` 會自己判斷錨點屬不屬於某個 Canvas，來決定用哪個相機換算，
所以「畫在背景上的店主」與「世界裡的立繪」共用同一支程式。

> ⚠️ Overlay 的 Canvas 換算時要傳 `null` 而不是 `Camera.main`。
> 傳了相機會整個偏掉，而且不會有任何錯誤訊息。

### 位置怎麼決定

在角色頭頂放一個空的 `RectTransform` 當錨點（`CharacterHitbox.bubbleAnchor`）。
美術換一張背景圖，只要把錨點拖到新的頭頂位置，程式不用動。
氣泡還會自己夾在畫面內，角色站在螢幕邊緣時不會有一半跑出去。

---

## 4.5 離開商店：滑出來的 EXIT 標籤

美術給了 `UI/exitbutton_mat/ExitButton_Shop.png`。行為與探索的 ExitTag 一樣是兩段式，
差別只在**移動的是 X 座標**（縮在右下角、往右藏）。

```
ExitTab_Zone   ← 固定不動的感應區（Image alpha 0、raycastTarget）
└─ Visual      ← 會滑的圖（Image + Button）
```

### 為什麼要拆成兩層

直覺的寫法是「滑鼠進來 → 把自己移出來」。**那會抖。**
標籤滑出去之後，游標底下那一點在標籤上的相對位置變了，很容易變成
「已經不在標籤上」→ exit → 縮回去 → 又碰到 → enter，一秒閃好幾次。
這是 [HANDOFF.md](HANDOFF.md) §4.6 第五條那個坑，手牌上浮踩過一次。

所以 `SlideOutTab` 掛在**固定不動的感應區**上，移動的是子物件。
感應區涵蓋「縮著」與「伸出來」兩個位置，游標在裡面怎麼走都不會抖 ——
順帶還讓玩家不必精準戳到那個小凸角。

### 順手修掉的一個手感問題

`BookmarkHover`（探索的舊版）用的是 `Mathf.Lerp(current, target, deltaTime * speed)` ——
**這個寫法跟幀率有關**，30fps 與 144fps 的手感不一樣，而且永遠到不了終點。
`SlideOutTab` 改成指數平滑，任何幀率下軌跡都一樣，靠近終點會吸附。

> 探索的 ExitTag 還在用舊的那支。要換過來的話欄位是一對一的：
> `hiddenY` → `hiddenOffset.y`、`shownY` → `shownOffset.y`。**這次沒有動它**，
> 因為它現在運作正常，而改它要重接場景引用。

### 確認流程

點 EXIT **不會直接離開**，跳 `LeaveAskPanel`（「確定要離開嗎？」→ 是／否），
與探索的 `ContinueAskPanel` 同一個形狀，走 `UIKind.Dialog` 堆疊。

> ⚠️ 面板跳出來時會**強制把標籤收回去** —— 面板蓋住標籤之後它收不到
> `OnPointerExit`，不主動收的話會一直卡在伸出來的狀態。

---

## 5. 這批新增／改動的檔案

### 新增

| 檔案 | 職責 |
|---|---|
| `Core/LootTable.cs` | 戰利品表（表 → 池 → 條目） |
| `Core/LootService.cs` | 抽取引擎。純函式、亂數外部傳入 |
| `Core/CharacterData.cs` | 一個角色（名字／立繪／寒暄／閒聊） |
| `Core/CharacterDatabase.cs` | id → 角色 |
| `Shop/ShopSlotUI.cs` | 貨架上的一格 |
| `Shop/ShopPanelUI.cs` | 整個貨架 + 錢 |
| `UI/Scripts/SpeechBubbleUI.cs` | 對話氣泡 + 空間轉換 |
| `UI/Scripts/CharacterHitbox.cs` | 背景角色的隱形點擊區 |

### 資產

```
Core/Items/          武器 ×4（card_ph_*）    ── 佔位，戰鬥牌組是 Romtyui 的
                     食物 ×6（fish_* / seaweed / hard_bread / jerky）
                     收藏品 ×8（relic_*）    ── **照大綱〈收藏品〉表，含敘述與效果**
                     補給 ×3（lamp_oil / old_rope / coarse_salt）
Core/Loot/           Loot_MerchantShop（預設）／Loot_MerchantShop_Village（漁村）
                     Loot_Sub_Weapons / Loot_Sub_Relics / Loot_Sub_Food_Village / Loot_Sub_Food_Generic
                     Loot_VillageChest_T1 / Loot_VillageChest_T2
_Archive/Items/      我自編的 10 件佔位收藏品（已被大綱的真品取代，封存不刪）
Core/Characters/     Character_merchant（商人・冷原人）★ 台詞照原文
                     Character_tokizo（時藏）／Character_campbell（坎貝爾）
Core/CharacterDatabase.asset
Stages/Stage_Shop.prefab   ← 整個重建
```

### 改動

| 檔案 | 改了什麼 |
|---|---|
| `Core/ItemData.cs` | 加 `price`、`tags`、`HasTag()` |
| `Core/RunContext.cs` | 加 `money` 與 `AddMoney` / `SpendMoney` |
| `Core/GameFlowManager.cs` | 加 `characterDatabase`、`startingMoney`，加 `Item()` / `Character()` 查詢 |
| `Explore/Scripts/ChestInteractable.cs` | 加 `lootTable` —— 與商店共用同一套抽取 |
| `Stages/ShopStageController.cs` | **整支重寫**。不再繼承 `ChoiceStageController` |

### 商店為什麼不再是「三個選項」

舊版借用對話框的三個選項槽當商品列，挑一件就走 —— 那是 Phase 6 的最小可玩版。
現在是貨架：八格、有價格、買到不想買為止。

連帶不再繼承 `ChoiceStageController`：那個基底的形狀是「開場白 → 選項 → 收尾」，
而且會**強制把對話框撐開** —— 對話框會蓋住貨架。商店的開場白現在是氣泡，不是排隊播放的對白。

---

## 6. 已經驗證過的（不用進 Play 也測得到）

`LootService` 是純函式，所以可以在編輯器直接跑。實測結果：

- **商店抽 8 件，件件不同** ✅
- **同一個種子重進商店，商品一模一樣** ✅（否則玩家離開再進來就能重骰）
- **寶箱重複的東西會併成「舊麻繩 ×3」** ✅
- 編譯 0 error 0 warning ✅
- Stage_Shop.prefab 的 8 格接線、氣泡、店主點擊區全部檢查過，0 個未接 ✅

還驗證了台詞：問候／成交隨機挑得到、閒聊池 13 句、彩蛋條件開啟後變 16 句 ✅

過程中修掉**四個**真的會出事的 bug：

1. **跨批次的重複** —— 一次抽獎湊不滿八件時會再抽一輪，若每輪各自算「不重複」，
   第二輪會從頭開始，貨架上就出現兩份曬乾的海藻。修法是把「已經拿過什麼」傳進去跨批次共用。
2. **`GameFlowManager.Instance` 在編輯器裡是 null** —— 標籤查詢會一筆都查不到。
   所以 `LootService` 的每支 API 都可以自己傳 `ItemDatabase` 進去，測試與編輯器工具一定要傳。
3. **`Table` 條目沒有把「不重複」傳下去** —— 外層寫「抽 3 張卡、不重複」，
   但條目是「轉去卡片表」時，子表每次都從零開始算不重複，**三格會出現同一張卡**。
   「不重複」是外層池子的意圖，委派出去不該把它丟掉。修法是往下傳本池子的集合而不是 null。
4. **資料庫的查表快取不會自己失效** —— 用編輯器腳本 `characters.Add(商人)` 之後，
   `GetById("merchant")` 一直回 `null`，而且**沒有任何錯誤訊息**。
   原因是 `OnValidate` 只有在 Inspector 編輯／Undo／匯入時才觸發，**程式改清單不會觸發**。
   兩個資料庫現在都會比對「建快取時的筆數」與「現在的筆數」，不一致就重建，
   並且多了公開的 `Invalidate()`。
   ⚠️ 只有**數量變化**偵測得到 —— 在程式裡調換既有元素要自己呼叫 `Invalidate()`。

---

## 7. 還沒做 / 要你決定

| 項目 | 狀況 |
|---|---|
| **收藏品的「特殊力量」** | 大綱說藏物「能夠提供特殊的力量」、且「收藏品之間的**平衡**很重要」——<br>**這套機制完全不存在**。現在收藏品只是有價格的道具，買了不會發生任何事。<br>這是商店真正的核心，但它是新系統，不是這批的範圍 |
| **買武器會進戰鬥牌組嗎** | 不會。現在只進背包。戰鬥牌組是 `Assets/Romtyui/` 的，<br>要接得跨隊談（見 [SystemsStatus.md](SystemsStatus.md) §2.1）。武器牌本身也還是佔位 |
| **收藏品的品質→價格** | 我暫定普通 30／罕見 55／稀有 90。**大綱沒有給價格**，等企劃定 |
| **區域欄位** | `RunNodeData` 沒有 region，商店暫時比對 `contentId`。<br>漁村的節點要把 Content Id 設成 `village` 才會換成漁村貨表 |
| **協助者／隊伍** | 不存在。彩蛋條件、「和協助者搞好關係」「協助者之間關係不好」都靠它 |
| **開局金幣多少、礦山掉多少** | `startingMoney` 佔位 120（約 3 件收藏品）。貨幣名稱已定＝**金幣** |
| **分頁（PREV / NEXT）** | 沒做。現在超過八件會被丟掉並警告。參考圖有分頁，要的話再說 |
| **商店背景圖** | 你給的那張還沒進專案。現在是色塊，格子位置是估的，圖進來後要對位 |
| **商品圖示** | 沒有美術 → 用 id 算出的固定顏色色塊代替。同一個道具永遠同一個顏色 |
| **對話節點的常駐氣泡** | 元件做好了但**還沒接到 `Stage_Dialogue`**。因為那裡已經有對話框在顯示「說話者＋內文」，<br>氣泡是要**取代**它、還是同時存在（框放旁白、氣泡放角色），這是手感問題，我不該自己決定 |
| **賣東西給店主** | 沒做。現在只能買 |

---

## 8. 立刻可以做的下一步

1. 把商店背景圖丟進 `Assets/TYN/UI/`，指定給 `Stage_Shop/Background` 的 Image，
   再把 8 個 `Slot_*` 拖到層板上對位（它們是**手動擺位**的，沒有 LayoutGroup，就是為了這個）。
2. 把 `Merchant_Hitbox` 拉到商人身上、`BubbleAnchor` 拉到他頭頂。
3. 進遊戲走到商店節點，確認這六件事：
   - 進場冒出【進入商店】三句之一
   - 發呆 14 秒會自己講閒聊，點他也會講下一句（**同一個順序，不會重複**）
   - 點商品 → 扣金幣 → 說【購買商品】其中一句 → 那一格變「已售出」
   - 金幣不夠 → 「誒～錢不夠哦？」，格子變暗但**還是點得動**（點不動比較難懂）
   - 買光 → 「呀，架上被你清空啦～」
   - 按「離開商店」→ 回地圖

---

## 參考來源

- [Loot Tables | Bedrock Wiki](https://wiki.bedrock.dev/loot/loot-tables) — 表 → 池 → 條目 的三層結構
- [Loot table - Minecraft Wiki](https://minecraft.fandom.com/wiki/Loot_table) — 權重＝自己的權重 ÷ 全部權重總和
- [Merchant | Slay the Spire Wiki](https://slay-the-spire.fandom.com/wiki/Merchant) — 商店的階級配比與「保證有一件」的寫法
- [Card Rewards | Slay the Spire Wiki](https://slay-the-spire.fandom.com/wiki/Card_Rewards) — 先決定稀有度、再從該稀有度的池子抽
- [Separate Game Data and Logic with ScriptableObjects | Unity](https://unity.com/how-to/separate-game-data-logic-scriptable-objects) — 資料與邏輯分離的官方說法
- [Unity ScriptableObjects: Game Data Without the Mess](https://www.angry-shark-studio.com/blog/unity-scriptableobjects-game-data-management/) — 單一資料庫 SO 當唯一真相
- [Bark Tutorial - Dialogue System for Unity](https://www.pixelcrushers.com/dialogue_system/manual2x/html/tutorial_bark.html) — bark 與對話樹的分野
- [Using Speech Bubbles | Yarn Spinner](https://docs.yarnspinner.dev/yarn-spinner-for-unity/unity-add-ons/speech-bubbles/using-speech-bubbles) — Character Bubble Anchor：把錨點的世界座標映射成 canvas 螢幕座標

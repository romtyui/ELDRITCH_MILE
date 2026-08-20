# 戰鬥 Prefab 對接說明

> 給 Romtyui（也寫給 AI 助手讀）。
> **這份是根據 `BattleManager.cs` 的欄位寫的，不是看最新的 SampleScene** ——
> 如果你那邊已經動過結構，以你的為準；這裡講的是「邊界要切在哪」與「怎麼切」。
> 2026-08-21

---

## 一、背景：為什麼要包成 prefab

我們這邊只有一個場景（`Assets/TYN/EventScene.unity`）。
所有環節——探索、對話、商店、事件、戰鬥——都是**執行時生成、結束時銷毀的 prefab**，
由 `StageHost`（`Assets/TYN/Core/StageHost.cs`）管理。

兩個好處，第二個對你我都重要：

1. 場景的 hierarchy 保持乾淨
2. **各環節分屬不同的 prefab 檔案，兩個人同時作業不會在同一個 `.unity` 上衝突**

現在戰鬥住在 `SampleScene`、我們住在 `EventScene`，這是唯一還沒接起來的一塊。

---

## 二、我們的 Stage prefab 是什麼形狀

### 2.1 一個 Stage prefab 的解剖

拿 `Assets/TYN/Stages/Stage_Shop.prefab` 當例子：

```
Stage_Shop                     ← 根物件。掛 ShopStageController（繼承 StageController）
├─ Shelf                       ← 貨架容器
│  ├─ Slot_0 … Slot_7          ← 八格商品，各自掛 ShopSlotUI
├─ ExitTag                     ← 離開鍵
└─ LeaveAskPanel               ← 「確定要離開嗎」
```

重點：

- **根物件只有一個，而且掛著 `StageController` 的子類別**
- **根物件沒有 `Canvas` / `CanvasScaler` / `GraphicRaycaster`**（理由見 §4.1）
- 這個 prefab 需要的東西全部在它自己底下，**沒有任何欄位指向場景**

### 2.2 四條規則

**① 根物件掛一個 `StageController` 子類別**

戰鬥的我已經寫好了：`Assets/TYN/Stages/BattleStageController.cs`。
**你不用寫**，把它掛在 prefab 的根物件上就好。

**② Stage 內部不准做場景切換**

不要有 `SceneManager.LoadScene`。環節結束要「回報」而不是「自己跳」——
下一步由 `GameFlowManager` 決定（通常是地圖下拉）。

戰鬥這一條已經滿足了：你在 `EndBattle()` 加的那兩行 `TutorialEventBus.Raise` 就是回報。

**③ 結束是自動回報，不是玩家按返回鍵**

同上。玩家按的是「戰鬥的結束」，不是「回到地圖」——後者是系統的事。

**④ prefab 不能引用場景物件**

這是 Unity 的限制，不是我們的規定，但它決定了下面所有的取捨。詳見 §3。

### 2.3 場景物件怎麼辦：我們用靜態存取

有些東西天生住在場景（常駐 UI、單例），prefab 需要用它們但**存不下引用**。
我們的做法是**執行時透過靜態屬性去拿**，Inspector 欄位留空。

真實例子（全部在 `Assets/TYN/`）：

| 場景上的東西 | prefab 怎麼拿到它 |
|---|---|
| 對話框 | `PopupService.Instance` |
| 選項面板 | `DialogueOptionsPanel.Instance` |
| 手牌區 | `ExploreHandUI.Instance` |
| 打牌規則引擎 | `DialogueEncounterController.Instance` |
| 角色立繪的點擊區 | `CharacterHitbox.SceneSpeaker` |

寫法長這樣（`DialogueStageController` 的真實程式碼）：

```csharp
[Tooltip("立繪上的隱形點擊區…\n" +
         "⚠️ 它住在**場景**，而 prefab 不能引用場景物件，\n" +
         "所以這裡留空即可 —— 執行時會自己找 CharacterHitbox.SceneSpeaker")]
public CharacterHitbox speakerHitbox;

// 執行時才解析
if (speakerHitbox == null) speakerHitbox = CharacterHitbox.SceneSpeaker;
```

**戰鬥這邊不需要用到這招** —— `BattleManager` 唯一的場景依賴是 `RunStateManager`，
而它本來就是用 `RunStateManager.Instance` 取用的。

---

## 三、什麼包進去、什麼留在場景

### 3.1 判斷法則

> **在 Inspector 上看 `BattleManager`，凡是欄位裡有東西的，那個東西就必須在 prefab 內。**

因為 prefab 存不下場景引用：存檔時**不會報錯**，欄位會靜靜地變成 `None`，
執行時才 `NullReferenceException`。這是最常見也最難查的一種。

### 3.2 ✅ 要包進 prefab

`BattleManager` 有 16 個指向場景物件的欄位：

```
optionMenuUI            playerUnit              enemies / currentEnemy
playerDeck              energySystem            enemyFormationSpawner
handUIController        playerStatusBarUI       turnEndButtonAnimatorUI
turnPhaseBannerUI       hpFillImage             sanFillImage
transformAnimationController                    cardHitEffectController
godCardCorruptionAnimationController            generalCardPlayAnimationController
```

這些的目標物件全部要在 prefab 底下。

### 3.3 ⛔ 千萬不要包：`RunStateManager`

**這是整份文件最重要的一條。**

`RunStateManager` 存的是**整場 run 的 HP／SAN／牌組**，它要活得比任何環節都久
（你自己在 `Awake` 裡寫了 `DontDestroyOnLoad`）。

**它必須留在場景裡，全程只有一份。**

包進戰鬥 prefab 會怎樣：每打一場就生一個新的。你的 `Awake` 有重複檢查會把新的砍掉，
所以**表面上不會壞**——但那是「剛好沒事」，不是對的，而且哪天檢查被改動就會靜靜地掉存檔。

而且 `BattleManager` 是用 `RunStateManager.Instance` 取用它的，**Inspector 上本來就不需要引用**。

### 3.4 ⛔ 也留在場景：教學那一組

`TutorialManager` / `TutorialUI` / `TutorialEventBus` 橫跨探索與戰鬥兩邊——
地圖、抽牌、探索場地那幾步的訊號是從我們這裡發的
（`Assets/TYN/Core/TutorialSignal.cs`）。

所以教學是場景層級的東西，不屬於任何單一環節。

### 3.5 灰色地帶：`OptionMenuUI`（死亡選單）

它被 `BattleManager` 直接引用，所以照 §3.1 的法則要包進去。

但**「玩家死了之後怎麼辦」我們兩邊都有一套**——你有死亡選單，我們有輪迴結算
（遺產、`RunContext.ContributeToMeta`）。這塊重疊了，要當面對一次決定誰負責。
在那之前先照現狀包進去，不影響其他部分。

> ⚠️ 順帶一提：`Assets/Romtyui/codes/Units/OptionMenuUI.cs:90` 還在用
> `Input.GetKeyDown(KeyCode.Escape)`。專案已經切到 Input System，
> **那一行執行時會直接丟 `InvalidOperationException`，編譯期完全看不出來。**
> 死亡選單一開就會踩到。
>
> 改法（對齊你自己的 `BattleDebugHotkeys` 寫法）：
> ```csharp
> var kb = UnityEngine.InputSystem.Keyboard.current;
> if (kb != null && kb[UnityEngine.InputSystem.Key.Escape].wasPressedThisFrame) { … }
> ```

---

## 四、從場景抽 prefab 的兩個坑

我們踩過，`StageHost.ValidateInstance()` 現在會在載入時自動檢查並報警。
但先知道會省很多時間。

### 4.1 根物件不要帶 `Canvas`

**症狀**：prefab 生成了，hierarchy 裡看得到，但畫面上位置／縮放全錯，或整個不見。

**原因**：我們的掛載點（`Canvas_Stage`）已經提供 `Canvas` / `CanvasScaler` / `GraphicRaycaster`。
prefab 根再帶一層不只是多餘——**巢狀之後 `RectTransform` 會停止被 Unity 驅動**。

**做法**（順序不能反，反了會噴相依性錯誤）：

1. 移除 `GraphicRaycaster`
2. 移除 `CanvasScaler`
3. 移除 `Canvas`
4. 把根 `RectTransform` 的 anchor 設成 stretch 全滿（min 0,0 / max 1,1 / offset 全 0）

> 子物件底下要巢狀 Canvas 是可以的（我們的 `Stage_Event` 就有一個，
> 為了讓結束鍵蓋過對話框，`overrideSorting = true` + `sortingOrder = 150`）。
> **限制只在「根物件」。**

### 4.2 移除 Canvas 之後，Scale 會變成 0

**症狀**：prefab 有生成、hierarchy 裡看得到、但畫面上完全空白。

**原因**：根 Canvas 的 `RectTransform` 平常是被 Unity 即時驅動的
（尺寸＝螢幕、scale 由 `CanvasScaler` 算），所以**序列化存下來的值是 0**。
一旦不再被驅動，那個 0 就會真的生效。

**做法**：移除 Canvas 之後**手動把 Scale 改回 (1, 1, 1)**。這個不會自動修正。

### 4.3 世界物件不受影響

3D 的戰鬥場景、角色模型那些掛在 `worldRoot` 底下，不是 UI，沒有上面兩個問題。

**如果戰鬥同時有世界物件與 UI，可以拆成兩個 prefab** ——
`StageHost.StageEntry` 有 `customParent` 欄位，我們這邊可以分別指定掛載點。
跟我說一聲就好。

---

## 五、逐步操作程序

給 AI 助手：這一節可以照順序執行。

**Step 1 —— 先確認邊界**

在 SampleScene 選取 `BattleManager` 所在的物件，看 Inspector 上 16 個欄位分別指向誰。
把那些物件的**共同最上層祖先**找出來——那就是 prefab 的根。

**Step 2 —— 檢查根物件底下有沒有「不該包的」**

`RunStateManager`、`TutorialManager`、`TutorialUI` 如果在那個祖先底下，
**先把它們拖到 hierarchy 的最外層**（變成場景的根物件），再繼續。

**Step 3 —— 處理根物件的 Canvas**

如果根物件有 `Canvas`，照 §4.1 的順序移除三個元件，再照 §4.2 把 Scale 改回 1。

**Step 4 —— 掛上 `BattleStageController`**

在根物件加上 `Assets/TYN/Stages/BattleStageController.cs`。
它的 `battleManager` 欄位留空也可以（會自己在子物件裡找），指定當然更好。

**Step 5 —— 拉成 prefab**

拖進 `Assets/Romtyui/` 底下任一資料夾。建議檔名 `Stage_Battle.prefab`。

**Step 6 —— 驗證（重要，不要跳）**

見下一節。

---

## 六、怎麼驗證有沒有包對

**① 開 prefab 檢查所有欄位**

雙擊 prefab 進入 Prefab Mode（**不是在場景裡看那個實例**），
逐一檢查 `BattleManager` 的 16 個欄位。

> **只要有一個是 `None`，就是那個物件沒被包進來。**
> 在場景裡看是好的、進 Prefab Mode 變 None——這正是「引用了場景物件」的症狀。

**② 檢查根物件**

- 沒有 `Canvas` 元件
- Scale 是 (1, 1, 1)
- 掛著 `BattleStageController`

**③ 檢查場景**

`RunStateManager`、`TutorialManager`、`TutorialUI` 還在場景裡，而且**不在 prefab 內**。

**④ 用程式掃一遍（最保險）**

如果有 AI 助手可以跑 Unity 編輯器程式碼，這段會把所有 `None` 欄位列出來：

```csharp
var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/Romtyui/Stage_Battle.prefab");

foreach (var comp in go.GetComponentsInChildren<MonoBehaviour>(true))
{
    if (comp == null) continue;
    var so = new UnityEditor.SerializedObject(comp);
    var it = so.GetIterator();
    while (it.NextVisible(true))
    {
        if (it.propertyType != UnityEditor.SerializedPropertyType.ObjectReference) continue;
        if (it.objectReferenceValue == null && it.objectReferenceInstanceIDValue != 0)
            Debug.LogError($"斷掉的引用：{comp.GetType().Name}.{it.propertyPath}", comp);
    }
}
```

---

## 七、接起來之後的流程

```
玩家在地圖點戰鬥節點
   → GameFlowManager 載入 Stage_Battle prefab
   → BattleStageController.OnStageEnter()
        · RunStateManager.ReserveEncounterByEnemyData(...)   ← 指定這場打誰
        · BattleManager.StartBattle()
   → （你的戰鬥流程，我們完全不碰）
   → EndBattle()
        · SaveFromBattle()                                   ← HP/SAN/牌組寫回
        · TutorialEventBus.Raise(BattleWon / "BattleLost")   ← 你已經加好了
   → BattleStageController 收到訊號
        · 替打倒的敵人立 killed_<enemyId> 旗標
        · 回報 GameFlowManager → 地圖下拉
```

**HP／SAN／牌組不用互相傳。** 兩邊都透過 `RunStateManager` 讀寫，那是唯一的真相。
我們包了一層 `PlayerVitals`（`Assets/TYN/Core/PlayerVitals.cs`）只是為了不直接碰你的欄位，
行為完全一樣，也沒有改到你的任何檔案。

---

## 八、還要你決定的三件事

**① `EnemyData.enemyId` 全是空的**

五個敵人資產（`Boss` / `coral Paguroidea` / `Mermaid Priest` / `tuā-khoo-tai` / `雜魚`）
的 `enemyId` 都沒填。`ReserveEncounterByEnemyData()` 一看到空 id 就整組跳過並警告——
**所以現在指定不了對手**，只能吃你自己的抽怪邏輯。

建議：`boss` / `coral_paguroidea` / **`fish_priest`** / `tua_khoo_tai` / `minnow`

`fish_priest` 要特別對——大綱《螺湮的祝福》是「打倒半魚人祭司后 30% 機率觸發」，
我們用 `killed_fish_priest` 這個旗標接。**填什麼字都行，兩邊一樣就好。**

**② 起始牌組與開局數值**

你 `SampleScene` 的 `BattleDeck.startingDeck` 是斧／劍／弓／棍／盾×4 ＋ `octopus_god` ＋ `goat_god`。

我們做了 `StartingDeck_Default`，**照你這副但不含那兩張神牌**——
大綱裡神牌要靠事件取得，不該開局就有。如果那兩張是刻意的，跟我說，我加回去。

還缺一個數字：**開局的 HP／SAN 上限是多少？**
填下去之後戰鬥會套用我們設定的血條而不是你的預設值——
那正是「一場 run 接續同一條血條」的用意，但數值要你定。

**③ 死亡之後歸誰**（見 §3.5）

---

## 九、檢查清單

- [ ] 根物件掛了 `BattleStageController`
- [ ] 進 Prefab Mode 看，`BattleManager` 的 16 個欄位**沒有一個是 None**
- [ ] `RunStateManager` **不在** prefab 裡，還留在場景
- [ ] `TutorialManager` / `TutorialUI` **不在** prefab 裡
- [ ] 根物件沒有 `Canvas` 元件
- [ ] 根物件 Scale 是 (1, 1, 1)
- [ ] prefab 內沒有任何斷掉的引用（跑 §6④ 那段掃一次）
- [ ] `OptionMenuUI.cs:90` 的 `Input.GetKeyDown` 改成 Input System

放進 `Assets/Romtyui/` 底下就好，我們這邊 `StageHost` 的清單指過去即可。

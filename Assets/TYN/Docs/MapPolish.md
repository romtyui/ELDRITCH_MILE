# 地圖動效與 Tooltip 操作指引

> 對應 2026-08-16 的 UI 動效調整。程式已完成（編譯 0 error 0 warning）。
>
> 兩項變更：**節點 hover 從縮放改成 tooltip**、**棋子移動從彈跳改成磨砂石桌上的滑行**。

---

## 1. 棋子移動：磨砂石桌上的西洋棋

**不需要任何編輯器設定就會生效** —— 新欄位都有預設值。想調手感再往下看。

### 改了什麼

拿掉了 `Abs(Sin)` 做的腳步上下起伏（棋子是**滑**的，不是走的），換成三件事：

| 欄位 | 預設 | 作用 |
|---|---|---|
| `Avatar Move Curve` | 推出 → 滑行 → 摩擦煞停 | 位移的緩動。**沒有回彈** |
| `Avatar Grain Jitter` | `2`（像素） | 滑行時垂直方向的細微抖動 ＝ 磨砂觸感 |
| `Avatar Grain Frequency` | `14` | 抖動的顆粒密度，越大越細碎 |
| `Avatar Move Duration` | `0.9` 秒 | 整段時間 |

### 調整方向

- **想更「重」**：把曲線後段拉更長（煞停更慢），或 `Duration` 加到 1.1~1.3
- **想更「輕快」**：縮短曲線前段的加速，`Duration` 降到 0.6~0.7
- **太粗糙**：`Grain Jitter` 調到 1
- **要完全平滑**：`Grain Jitter` 設 0

> `Grain Jitter` 大於 5 會變成「在抖動」而不是「在磨擦」，不建議。

### 一個刻意的設計

**抖動幅度綁著當下速度。** 固定幅度的抖動在快停下時會變成「原地發抖」，很假；
綁速度就會自然收斂，停下的瞬間剛好歸零，不需要額外的收尾處理。

---

## 2. 節點 Tooltip

### 為什麼不用縮放了

縮放在這張地圖上已經被「狀態」用掉了 —— 當前 `1.2` / 可前往 `1.0` / 去不了 `0.8`。
再讓 hover 也改縮放，同一個視覺通道要表達兩件事，玩家分不出
「這個比較大」是因為它是當前位置，還是滑鼠剛好在上面。

> ⚠️ **tooltip 面板沒建起來之前，hover 完全沒有回饋。**
> 趕時間的話把 `MapNodeUI.Scale Hover` 設成 `1.1` 暫時頂著（預設 `1` ＝ 關閉）。

### 步驟 1 — 建面板

在地圖覆蓋層底下建一個框：

```
說明框                ← RectTransform + CanvasGroup + MapTooltipUI
├── Title (TMP)
└── Body (TMP)
```

初始狀態設**停用**（程式會自己開關）。

| `MapTooltipUI` 欄位 | 拖什麼 |
|---|---|
| `Panel` | 留空則自動用本物件 |
| `Canvas Group` | 同物件上的 CanvasGroup |
| `Title Text` / `Body Text` | 兩個 TMP。`Body` 可留空 |

### 步驟 2 — 選擇擺放方式

| `Placement` | 行為 | 適合 |
|---|---|---|
| `Follow Node` | 貼在被 hover 的節點旁，程式算位置並夾在畫面內 | 資訊少、想貼著看 |
| **`Fixed`** | **位置完全由你擺，程式不碰** | **固定在右下角這種空間大、不擋地圖的區域** |

選 `Fixed` 時 `Offset` 與 `Screen Padding` 都不會被使用。

#### 用 `Fixed` 的話還要設這些

| 欄位 | 建議 | 為什麼 |
|---|---|---|
| `Keep Frame When Idle` | **✓** | 沒 hover 時保留框、只換文字。一個時有時無的框會讓那塊區域一直閃，玩家也不會知道那裡本來有東西 |
| `Idle Body` | 「把游標移到節點上查看。」 | 閒置時的提示 |
| 面板尺寸 | **固定，不要用 `ContentSizeFitter`** | 固定資訊區跟著文字長短改變大小會很晃 |
| RectTransform anchor | 貼右下角 | 解析度變動才不會跑掉 |

> 勾了 `Keep Frame When Idle` 之後淡入會自動跳過（框一直在，每次 hover 都閃一下更吵），
> 而且「選定節點要移動」「重建地圖」這兩個時機也只會換回閒置文字，不會整塊消失。

### 步驟 3 — 接到 `MapView`

| 欄位 | 拖什麼 |
|---|---|
| `Node Tooltip` | 剛建的面板。**留空則不顯示 tooltip，節點照常可點** |

### 步驟 4 — 填文案

`MapView.Node Tooltip Texts`，五種節點類型各一筆：

| Kind | 標題範例 | 說明範例 |
|---|---|---|
| `Event` | 探索 | 也許有東西可以拿，也許只是一堆廢物。 |
| `Combat` | 戰鬥 | **（尚未開放）** |
| `Boss` | 主宰 | **（尚未開放）** |
| `Shop` | 商店 | **（尚未開放）** |
| `SpecialEvent` | 異常 | **（尚未開放）** |

> **還沒實作的類型也先寫上** —— 玩家看到「商店：尚未開放」比看到空白好，
> 之後功能做完只要改這裡的文字，不用動程式。
>
> ⚠️ 用 Inspector 的 `+` 新增時 `Kind` 一律是 `Event`（零填充，HANDOFF §4.3），**記得逐筆改**。
> 沒有對應條目的類型會退回顯示 enum 名稱，不會是空白。

狀態附註（「你在這裡」「可以前往」「已經去過了」「從這裡過不去」）是另外四個欄位，已有預設值。

---

## 3. 兩個已經處理掉的陷阱

**① 物件在 hover 狀態下被停用時，Unity 不會送 `OnPointerExit`。**
關掉地圖覆蓋層或重建地圖時都會發生，結果說明框留在畫面上關不掉。
已在 `MapNodeUI.OnDisable` 補上。

**② `owner?.` 認不出已 Destroy 的 Unity 物件。**
`?.` 走的是 C# 的 null 判斷，`!= null` 才會走 Unity 覆寫的比較。
場景結束時這兩者的差別就是有沒有 `NullReferenceException` —— 所以那裡刻意不用 `?.`。

---

## 4. 之後可以接的：游標變化

`CursorManager`（`Assets/TYN/UI/`，**是我們這邊的，不用跨隊協調**）已經有現成機制：
`SetCursor(CursorType)`，支援靜態圖與多幀動畫。插入點就是
`MapNodeUI.OnPointerEnter` / `OnPointerExit`，跟 `InteractableBase` 用同一套。

### ⚠️ 但沒有素材之前不要接

`SetCursor` 找不到對應的 `CursorData` 時會 `Debug.LogWarning` 然後 return，
而且它的「狀態沒變就跳過」防護對缺資料的類型無效 ——
**滑過節點時會每一幀噴一次警告**，Console 直接被洗版。

有素材之後的三步（約十分鐘）：

1. `CursorType` 加一個值（例如 `HoverMapNode`）
2. `CursorManager.cursorDataList` 加一筆，圖拖進 `Textures`
3. `MapNodeUI` 的那兩個方法各加一行

---

## 5. 關於 Tooltip 的實作選擇

Romtyui 有一份 `TooltipUI`，其實夠通用（`TooltipEntry` 就是 title + body）。**沒有共用**，因為：

- 它不在 `EventScene` 裡，要用就得把對方的 prefab 搬進我們的場景
- 那等於讓地圖的外觀與生命週期綁在戰鬥那邊
- 依專案分工慣例（只動 `Assets/TYN/`），這種耦合不逕行建立

**若日後決定全遊戲統一一套 tooltip**，換掉的成本很小 ——
節點只透過 `MapView.ShowNodeTooltip()` 這一個出口要求顯示，
改那一個方法的內容即可，節點本身一行都不用動。

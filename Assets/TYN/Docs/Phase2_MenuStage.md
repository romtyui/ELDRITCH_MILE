# Phase 2 — 主選單 Stage 操作指引

> 對應 `SceneConsolidationPlan.md` §6 Phase 2。程式已完成（`Assets/TYN/Stages/MenuStageController.cs`，編譯 0 error），本文只涵蓋編輯器操作。
>
> 版本：2026-08-08 · 預估 30–45 分鐘

---

## 這一關為什麼最輕

MenuScene 只掛了 4 個腳本，其中 **3 個保留、1 個封存**：

| 腳本 | 處置 | 說明 |
|---|---|---|
| `TrickButton` | ✅ 保留 | hover 時文字變 START 並轉粉色 |
| `BGLiquidController` | ✅ 保留 | 背景液態扭曲，會 `new Material()` 複製材質 |
| `BoilFrameEffect` | ✅ 保留 | 邊框沸騰抖動，記錄 `anchoredPosition` 當基準 |
| `SceneLoader` | 📦 已封存 | 由 `MenuStageController` 取代 |

沒有任何跨場景依賴，所以搬起來乾淨。

---

## 事前提醒：三顆按鈕目前綁的是同一件事

檢查舊 MenuScene 的 `OnClick` 綁定後發現，**START / SETTINGS / RESTART 三顆全部綁 `SceneLoader.LoadUIScene`** —— 也就是 SETTINGS 和 RESTART 從來沒有實作過，只是佔位。

`MenuStageController` 對應提供三個方法，語意未定的兩個**刻意不實作**，只印 log：

| 方法 | 狀態 |
|---|---|
| `OnStartClicked()` | ✅ 實際可用，呼叫 `GameFlowManager.StartNewRun()` |
| `OnSettingsClicked()` | 佔位。Romtyui 有 `OptionMenuUI`，日後要接從這裡叫 |
| `OnRestartClicked()` | 佔位。語意待定 |
| `OnClearProgressClicked()` | ⚠️ 破壞性：清除跨輪迴進度。若 RESTART 的本意是這個，改綁這支 |

---

## 步驟 1 — 從 MenuScene 抽出 Prefab

1. 開啟 `Assets/TYN/MenuScene.unity`
2. 在 Hierarchy 選取 **`Canvas`**（底下含 `BG`、`frame_st/se/re`、`START_Btn`、`SETTINGS_Btn`、`RESTART_Btn`、`Navi`、`Text (TMP)` 等）
3. 拖進 Project 視窗的 `Assets/TYN/Stages/` 資料夾 → 產生 `Canvas.prefab`
4. 改名為 **`Stage_Menu.prefab`**

> **只抽 `Canvas`**。`Main Camera`、`EventSystem`、`Directional Light` 不要抽 —— EventScene 已經各有一份，重複會出事。

### ⚠️ 必做：把根物件的 Canvas 拆掉

**這是每個 Stage prefab 都會踩到的坑，抽完 prefab 一定要做這步。**

從舊場景的根 `Canvas` 拉出來的 prefab，根物件會帶著 `Canvas` + `Canvas Scaler` + `Graphic Raycaster`。掛到 `Canvas_Stage` 底下後會**完全看不見** —— prefab 有生成，但被縮到 0。

**原因**：根 Canvas 的 RectTransform 是被 Unity **即時驅動**的（尺寸 = 螢幕、scale 由 CanvasScaler 算），所以序列化存下來的是無意義的 0：

```
m_LocalScale: {x: 0, y: 0, z: 0}     ← 一旦不再是根 Canvas，這個 0 就是真的 0
m_SizeDelta:  {x: 0, y: 0}
m_Pivot:      {x: 0, y: 0}
```

一旦它變成別人的子物件，那些值就不再被驅動，`localScale` 真的變成 0。

**修法**（進 prefab 編輯模式，選根物件）：

1. 移除 **`Graphic Raycaster`**
2. 移除 **`Canvas Scaler`**
3. 移除 **`Canvas`**

> 順序不能反 —— 後兩者 `RequireComponent(Canvas)`，Canvas 還在時 Unity 不讓你先移除它。

4. RectTransform 手動改回：

| 欄位 | 值 |
|---|---|
| Anchor Preset | **stretch / stretch**（按住 **Alt** 點右下角那個） |
| Left / Top / Right / Bottom | 全 `0` |
| Pivot | `0.5` / `0.5` |
| **Scale** | **`1` / `1` / `1`** |

> **Scale 一定要手動改**。移除 Canvas 之後 Unity 不會幫你修正，它會維持 0。

`Canvas_Stage` 已經提供 Canvas / Scaler / Raycaster，Stage prefab 不需要自己再帶一份。

> 🛡️ `StageHost` 已加入執行期偵測：若 Stage prefab 根物件仍帶 Canvas 會出現黃字警告，Scale 為 0 會出現紅字錯誤，訊息裡直接寫了怎麼修。

### `MenuManager` 與 `Anim` 怎麼處理

先確認它們在 Hierarchy 的位置：

- 若在 `Canvas` **底下** → 已隨 prefab 一起抽出，不用管
- 若是 **場景根層級的獨立物件** → 檢查上面掛了什麼元件：
  - 有腳本或動畫控制選單 UI → 拖進 `Stage_Menu.prefab` 內
  - 空的或沒用到 → 忽略，隨舊場景封存

`BGMManager` **不進 prefab** —— 見步驟 4。

---

## 步驟 2 — 掛上 MenuStageController

1. 雙擊 `Stage_Menu.prefab` 進入 Prefab 編輯模式
2. 選根物件 → `Add Component` → **`MenuStageController`**

| 欄位 | 型別 | 值 |
|---|---|---|
| `Verbose Log` | bool | `✓`（開發期建議開） |

3. 重接三顆按鈕的 `OnClick`（原本指向 `SceneLoader`，該物件已不存在，欄位會顯示 `Missing`）：

| 按鈕 | OnClick 設定 |
|---|---|
| `START_Btn` | 移除舊項目 → `+` → 拖 **prefab 根物件** 進 Object 欄 → 下拉選 **`MenuStageController → OnStartClicked ()`** |
| `SETTINGS_Btn` | 同上，選 `OnSettingsClicked ()` |
| `RESTART_Btn` | 同上，選 `OnRestartClicked ()` |

> ⚠️ 下拉選單要挑 **`MenuStageController`** 分類底下的方法，不要挑到 `GameObject` 或 `Transform` 的同名項目。
>
> ⚠️ Object 欄位拖 **prefab 內的根物件**，不要拖場景裡的東西 —— prefab 不能引用場景物件，會變成 `None`。

4. 存檔離開 Prefab 模式

---

## 步驟 3 — 註冊到 StageHost

1. 開 `EventScene`，選 `[STAGE_HOST]`
2. `StageHost` 的 `Stages` 清單 `+` 新增一筆：

| 子欄位 | 型別 | 值 |
|---|---|---|
| `Type` | `StageType` | **`Menu`** |
| `Prefab` | `GameObject` | 拖 `Stage_Menu.prefab` |
| `Custom Parent` | `Transform` | **留空**（純 UI，會自動掛在 `Ui Root` = `Canvas_Stage` 底下） |

3. 選 `[SYSTEM] → GameFlowManager`，把 `Boot Stage` 從 `None` 改回 **`Menu`**

---

## 步驟 4 — BGMManager 搬進常駐層

BGM 要跨 Stage 持續播放，所以**不能**放在 `Stage_Menu.prefab` 裡（Stage 一切換就被 Destroy）。

1. 回 `MenuScene`，複製 `BGMManager` 物件（Ctrl+C）
2. 開 `EventScene`，貼到 **`[SYSTEM]`** 底下（Ctrl+V）
3. 確認它上面若有 `AudioSource`，`Play On Awake` 與 `Loop` 依需求設定

---

## 步驟 5 — 封存舊場景

1. `File → Build Profiles` → Scene List 取消勾選 `MenuScene`（先別移除，保險）
2. 確認 Phase 2 驗收全數通過後，再把 `Assets/TYN/MenuScene.unity`（連同 `.meta`）移到 `Assets/TYN/_Archive/Scenes/`

> 建議**驗收通過再搬**。萬一 prefab 有東西沒抽乾淨，舊場景還在原位比較好比對。

---

## 步驟 6 — 驗收

按 Play：

- [ ] 畫面從全黑淡出後，**看到主選單**
- [ ] 背景液態扭曲動畫正常（`BGLiquidController`）
- [ ] 三個邊框各自沸騰抖動，且**漂移方向不同**（`BoilFrameEffect` 有各自的隨機種子）
- [ ] hover 按鈕時文字變 `START` 並轉粉色（`TrickButton`）
- [ ] BGM 有播
- [ ] Console 無紅字

點 **START**：

- [ ] Console 出現 `[選單] START`
- [ ] Console 出現 `[Run] 開始新的一場 run（seed …）`
- [ ] 畫面淡出 → 選單消失
- [ ] Console 出現 `[Flow] 沒有指定 MapOverlay…` **或**地圖面板從上方滑下

> 最後一項取決於 Phase 1 有沒有把 `Map Overlay` 欄位拖好。此時地圖是空的（沒有節點），只會看到一塊空面板 —— **這是正確的**，節點生成是 Phase 3。

點 **SETTINGS / RESTART**：

- [ ] Console 分別印出「尚未實作」，畫面無變化

---

## 常見錯誤

| 症狀 | 原因 | 解法 |
|---|---|---|
| **選單有生成但完全看不見** | 根物件還是 Canvas，`localScale` 停在 0 | 見步驟 1 的「必做」區塊。Console 會有 StageHost 的紅字提示 |
| 選單顯示但大小/位置錯亂 | 根物件的 Anchor 或 Pivot 還是舊 Canvas 的 `0,0` | 同上，改成 stretch 全滿、Pivot 0.5 |
| 按 START 沒反應，Console 無訊息 | 按鈕 OnClick 沒接上，或還指著 `Missing` | 回步驟 2.3 重接 |
| `[選單] 場上沒有 GameFlowManager` | EventScene 的 `[SYSTEM]` 底下沒有它，或被停用 | 檢查 Phase 1 §2.2 |
| 選單出現但按鈕點不到 | `Canvas_Stage` 的 Sort Order 被 `[MAP_OVERLAY]`(300) 蓋過 | Stage 是 100、Map 是 300 —— 地圖收起時應把 `blocksRaycasts` 關掉，`MapOverlayController` 已處理；若仍擋住，檢查 `MapPanel` 的 CanvasGroup 有沒有拖給腳本 |
| 邊框三個一起往同方向漂 | `BoilFrameEffect` 的 `Start()` 沒跑到（物件初始為停用） | 確認 prefab 內三個 frame 都是啟用狀態 |
| 背景材質變成全白／破圖 | `BGLiquidController` 會 `new Material(bgImage.material)`，prefab 化後材質引用可能斷 | 進 prefab 模式檢查 `BG` 的 Image → Material 是否還指向原本的材質 |
| BGM 切換 Stage 後斷掉 | `BGMManager` 被放進 `Stage_Menu.prefab` 了 | 移到 `[SYSTEM]` 底下（步驟 4） |

---

## 完成後

接著是 **Phase 3 — 地圖改為常駐覆蓋層**（2 天，最關鍵的一關）：把 `MapData` 從 `PerspectiveMapGenerator` 抽掉、刪掉 5 個轉場協程、地圖 UI 搬進 `[MAP_OVERLAY]`。做完之後「狀態隨場景死亡」這個最大的病根就解決了。

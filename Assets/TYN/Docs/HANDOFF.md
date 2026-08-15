# ELDRITCH_MILE — 接手說明（先讀這份）

> 給下一個接手 `Assets/TYN/` 的人／AI。
> 本文只寫**其他文件沒寫、但不知道會踩坑**的事。架構、進度、待辦都在別的文件裡，這裡只給指路與慣例。
>
> 最後更新：2026-08-14

---

## 0. 一分鐘看懂這個專案

2D Roguelike 卡牌遊戲（Unity 6000.4.1f1、URP）。玩家在地圖上選節點 → 進入探索房間 → 用**機率卡**跟寶箱／NPC 互動 → 打牌判定成功或失敗 → 回地圖。

原本流程散在 5 個場景，正在整合成**單一 `EventScene`**。目前 Build Settings 只剩 1 個場景。

**現在的進度**：Phase 0–3 完成（清場、Core 層、主選單、地圖覆蓋層），Phase 4 探索進行中，打牌環節第一批已可玩。

---

## 1. 文件閱讀順序

| 順序 | 文件 | 讀它做什麼 |
|---|---|---|
| 1 | **本文** | 慣例、工具、踩坑清單 |
| 2 | [RoadmapNext.md](RoadmapNext.md) | **接下來要做什麼**（最新，2026-08-14） |
| 3 | [EngineeringGuide.md](EngineeringGuide.md) | 命名空間、資料夾、架構原則 |
| 4 | [SceneConsolidationPlan.md](SceneConsolidationPlan.md) | 架構為什麼長這樣、19 條企劃約束（C1–C19） |
| 5 | `Phase*_*.md` | 各階段的**編輯器操作**步驟與驗收清單。最新一份是 [Phase4c2_RetryAsk.md](Phase4c2_RetryAsk.md)（C12，程式已完成、**編輯器操作待做**） |
| ⚠️ | [Status.md](Status.md) | **已過時**（停在 2026-08-08），只當歷史快照看 |

> 提到「C7」「C18③」這種代號時，指的是 `SceneConsolidationPlan.md` §4.0 的企劃約束編號。那是所有設計決策的依據。

---

## 2. 跟這位開發者合作的規矩

這些是他明確講過的，不要重新提案：

| 規矩 | 說明 |
|---|---|
| **只動 `Assets/TYN/`** | `Assets/Romtyui/` 是隊友的（戰鬥系統）。需要他那邊配合時**提出來讓開發者去談**，不要直接改 |
| **封存，不刪除** | 淘汰的檔案連 `.meta` 一起移到 `Assets/TYN/_Archive/`。確認穩定後才由開發者決定真刪 |
| **舊碼預設重寫，不逐行改造** | 他說過「代碼和設計上有太多大便」。遇到遺留程式，預設判斷是封存重寫，只有明確有用且與新架構相容的才保留 |
| **先問清楚再動大刀** | 設計選擇（手感、流程）給建議與取捨，讓他決定；技術細節可以自己判斷 |

---

## 3. 最有用的一招：不開 Unity 也能驗證編譯

**每次改完 C# 都用這個檢查，不要等 Unity 編譯。** 它會對 Unity 的真實組件編譯整個專案，秒級回饋。

在暫存目錄建 `verify.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NoWarn>CS0108;CS0114;CS0649;CS0169;CS0414;CS0067;CS0162;CS0219;CS0618;CS1591</NoWarn>
    <ProjectRoot>C:\Users\greyl\OneDrive\文件\GitHub\ELDRITCH_MILE</ProjectRoot>
    <UnityManaged>C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Data\Managed</UnityManaged>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$(ProjectRoot)\Assets\TYN\**\*.cs" />
    <Compile Include="$(ProjectRoot)\Assets\Romtyui\**\*.cs" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include="$(UnityManaged)\UnityEngine\*.dll"><Private>false</Private></Reference>
    <Reference Include="$(ProjectRoot)\Library\ScriptAssemblies\*.dll"
               Exclude="$(ProjectRoot)\Library\ScriptAssemblies\*Editor*.dll;$(ProjectRoot)\Library\ScriptAssemblies\Assembly-CSharp.dll">
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

```bash
dotnet build <path>/verify.csproj -v quiet --nologo
```

> `Library/ScriptAssemblies/` 要有東西（Unity 至少完整編譯過一次）。若暫存目錄被系統清掉，重建這個檔即可。

**讀 Unity Console 不必開 Unity**：`%LOCALAPPDATA%\Unity\Editor\Editor.log` 有全部輸出，包含 Play 時的 `Debug.Log`。

---

## 4. 這個專案反覆踩到的坑

前四個各踩過**兩次以上**，看到症狀先想這裡。

### 4.1 RectTransform 的「死值」

從別的場景複製 UI 過來，物件會**看不見**：`Scale (0,0,0)`、寬高 0。

原因：root Canvas 或 stretch 佈局的 RectTransform 值是被 Unity **即時驅動**的，序列化存的是無意義的 0。一旦換父層就不再被驅動，那些 0 就變成真的 0。

**症狀特別難查**：物件在 Hierarchy 看得到、`SetActive(true)` 也成功、沒有任何錯誤訊息，但畫面上完全不存在。

`StageHost` 與 `PopupService` 已加自動偵測會噴紅字。新做 UI 時記得手動確認 Scale 是 1。

### 4.2 `using` 寫在 namespace 外面會綁到舊型別

`_Archive/` 在 `Assets/` 底下所以**照樣編譯**，裡面的舊型別仍佔用全域命名空間。

C# 名稱解析在同一層「宣告永遠贏過 using 匯入」，而檔案最上方的 `using` 註冊在**全域層**。所以與未封存的舊型別同名時會綁到舊的，而且**編譯得過**，只在型別轉換時才爆。

**解法**：`using` 寫在 `namespace` 內部（且要在所有型別宣告之前）。`Map/MapView.cs` 有註解範例。

### 4.3 Unity Inspector 的 List `+` 會零填充

用 Inspector 的 `+` 新增 `List<SerializableClass>` 元素時，**不會套用 C# 的欄位初始值**。`public float weight = 1f;` 會變成 `0`，然後條目被靜默跳過。

`RoomLibrary` / `RoomContentData` 已加針對性警告。設計新的權重表時記得提醒使用者手動填。

### 4.4 停用的 Graphic 收不到 raycast

`image.enabled = false` 的 Image **完全不接收點擊**，而且不會有錯誤。

「看不見」和「點不到」同時發生時先查這個。要保有可點區域但不顯示，用 **alpha 0** 而不是停用元件。

### 4.5 其他

| 坑 | 說明 |
|---|---|
| **`OnEnable` 不會在初始 inactive 的物件上執行** | 預設隱藏的 UI 面板用 `OnEnable` 訂閱事件會漏掉。改用 `Start`（`SetActive(true)` 那一刻才跑）或掃描式註冊 |
| **prefab ↔ 場景的引用是單向禁止的** | prefab 不能在 Inspector 引用場景物件，**場景物件也不能引用 prefab 內部的東西**。跨界一律用單例（專案慣例：`PopupService.Instance` 這種） |
| **拖曳結束時 Unity 也會送 `OnPointerClick`** | 沒濾掉 `eventData.dragging` 的話，放開卡牌會順便把它選起來 |
| **URP 沒有 `Clear Flags`** | 對應欄位是 Camera 的 **Environment → Background Type** |

---

## 5. Unity MCP

已設定好，可直接讀 Hierarchy、Inspector 欄位、Console，比 parse `.unity` 的 YAML 快得多。

**兩個已知問題**：

- 專案路徑含中文「文件」，必須設 `PYTHONUTF8=1`，否則永遠偵測到 0 個 instance
- **`manage_asset` 的 `move` 會回報 `failed` 但其實成功了** —— 不要看回傳值，自己驗證檔案位置

`git mv` 對**未 commit 的新檔**會失敗（`bad source`），那種情況用一般 `mv`。

---

## 6. 動檔案前先確認 Unity 有沒有開

大批搬移（尤其含 `.meta`）時，Unity 邊 import 邊搬有機率讓 `.meta` 脫鉤、GUID 斷掉。

```powershell
Get-Process Unity -ErrorAction SilentlyContinue | Select-Object MainWindowTitle
```

**視窗標題有 `*` 代表有未存檔變更** —— 這時絕對不要動檔案，先請開發者存檔關閉。

單一檔案 + Unity 閒置時風險低，做過幾次沒事；但 20 個以上或含大型資產時務必請他關掉。

---

## 7. 目前狀態速覽

| 項目 | 現況 |
|---|---|
| 分支 | `recovery-progress`，最新 commit `f8bf980「Phase4」` |
| 工作區 | 有未提交改動：C12「在試一次？」的程式（2 個新檔）與文件 |
| Build Settings | **1 個場景**（`EventScene`） |
| 程式碼 | Core 26 + Map 3 + Stages 1 + Explore 14 個檔案 |
| 封存 | `_Archive/Scripts/` 25 個腳本、`_Archive/Scenes/` 4 個場景 |
| 編譯 | 0 error 0 warning |

**下一步請直接看 [RoadmapNext.md](RoadmapNext.md) §1「立即優先」。**

---

## 8. 命名空間對照（最常搞混）

| 命名空間 | 內容 | 為什麼要分 |
|---|---|---|
| `EldritchMile.Core` | 流程總管、資料層、判定、UI 服務 | 與 `_Archive/` 的全域舊型別撞名，必須隔離 |
| `EldritchMile.Map` | 地圖繪製 | 同上（`MapData` / `RunNodeData` 撞名） |
| `EldritchMile.Explore` | 探索房間、互動物件、卡牌 UI | 同上（`RoomController` 等撞名） |
| （全域） | `Stages/MenuStageController`、保留的舊卡牌腳本 | 沒有撞名問題，且 Inspector 綁定少一層阻力 |

**跨命名空間引用時，`using` 記得寫在 `namespace` 裡面**（見 §4.2）。

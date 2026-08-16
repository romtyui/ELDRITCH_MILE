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
| 2 | [SystemsStatus.md](SystemsStatus.md) | **全景**：哪些系統做完了、待做的卡在什麼、**以及劇情大綱裡的內容需要哪些系統**（2026-08-15） |
| 3 | [RoadmapNext.md](RoadmapNext.md) | 接下來要做什麼的**細節**與各項決策的來龍去脈 |
| 4 | [EngineeringGuide.md](EngineeringGuide.md) | 命名空間、資料夾、架構原則 |
| 5 | [SceneConsolidationPlan.md](SceneConsolidationPlan.md) | 架構為什麼長這樣、19 條企劃約束（C1–C19） |
| 6 | `Phase*_*.md` | 各階段的**編輯器操作**步驟與驗收清單 |
| 📺 | [DemoRoute.md](DemoRoute.md) | **要跑一次完整流程給人看**就讀這份（含目前哪些節點類型會斷） |
| 7 | [Phase6_Dialogue.md](Phase6_Dialogue.md) | 對話／商店／特殊事件三個 Stage（2026-08-16，**尚未完整驗收**） |
| ⚠️ | [Status.md](Status.md) | **已過時**（停在 2026-08-08），只當歷史快照看。全景改看 `SystemsStatus.md` |

> **劇情／角色設定**在 `克蘇魯劇情大綱.docx`（不在 repo 內，向開發者索取）。
> 它與工程的對照整理在 `SystemsStatus.md` §3 —— 那是唯一記錄
> 「劇情裡寫的東西需要哪些系統」的地方。

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

### 4.6 UI 疊放與事件傳遞（2026-08-16 一天內踩了十個）

做對話選項與手牌動效那天連續撞到的，成因各不相同但都**不會報錯**：

| 坑 | 症狀 | 為什麼 |
|---|---|---|
| **指標事件只往祖先傳，不傳兄弟** | hover 元件掛在感應區上，滑到卡片完全沒反應 | `IPointerEnter/Exit` 沿 hierarchy **往上**送。要掛在「感應區與內容的共同祖先」 |
| **`SetParent` 是附加到最後** | 卡面只剩卡框，圖不見了 | 為了避開 `childCount` 變動而倒著迭代 → 整疊圖層翻過來。要先收集再依原序搬 |
| **`RaycastAll` 會回傳被遮住的東西** | 卡片放在對話框中間也會打中後面的目標 | 掃全部命中物＝穿透。投放判定**只能認 `results[0]`** |
| **兩個東西搶 `SetAsLastSibling`** | 拖曳中的卡片偶爾沉到對話框後 | 誰贏看執行順序。拖曳要用**專用圖層**，且每次取用都推回最上層 |
| **hover 改變了被 hover 的東西** | 卡片在下緣瘋狂閃爍 | 上浮把卡片從游標底下抽走 → exit → 落下 → enter。**位移只動視覺子層，可點區域不動** |
| **透明的全幅 Button 會吃掉所有點擊** | 選項點不到 | `Advance Button`（alpha 0、1760×344）壓在選項上。`Button` 會**消化**點擊不讓它冒泡 |
| **子物件是空殼 Button** | 同上 | 拉預設 Button 留下的殘骸，alpha 0、零監聽，唯一作用是擋點擊 |
| **覆蓋層是「滑出畫面」不是停用** | 靠 `OnDisable` 做的收尾一律失效 | `MapOverlayController` 改 `anchoredPosition`，子物件從頭到尾都是啟用的。用 `OnClosing()` hook |
| **遲到的 `OnPointerExit` 會復活剛關掉的東西** | 地圖收起後 tooltip 又冒出來 | 滑走時節點離開游標 → exit → `Hide()` → 「閒置時保留框」又把它開起來。需要**總開關**讓關閉後的顯示要求一律失效 |
| **用「自己的旗標」判斷「整區的狀態」** | 拖曳中經過別張卡，目標上的機率就消失 | 拖曳中的卡 `blocksRaycasts = false`，游標穿透到底下的卡，那些卡的 `IsDragging` 都是 false，於是它們去改了預覽。守衛要問「**整個手牌區**有沒有人在拖」 |

> **共同教訓**：這十個沒有一個會噴錯誤訊息。查 UI 問題時先問三件事 ——
> 「事件到得了嗎」「順序是誰決定的」「這個收尾真的會被呼叫嗎」。
>
> 最後一個還多一條：**`blocksRaycasts = false` 會把事件送到不該處理它的物件上**，
> 那些物件用自己的旗標判斷，當然判斷不出來。凡是「同類物件有多個」的 UI，
> 狀態要記在管理者身上，不是各自記。

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
| 分支 | `recovery-progress` |
| Build Settings | **1 個場景**（`EventScene`） |
| 程式碼 | 54 個檔（Core 30 + Explore 14 + Map 3 + Menu 3 + UI 3 + Stages 1） |
| Phase 4c | **✅ 四批全部驗收完成** —— 打牌判定、回合感、失敗結案與重試、屬性與相剋 |
| Phase 6 | 🔶 **對話／商店／特殊事件已實作**，對話測到可出牌，其餘待驗收 |
| ⚠️ 未提交 | **24 個修改 + 38 個新檔**，最新 commit 是 `28421c6 Phase4c4` |
| 封存 | `_Archive/Scripts/` 25 個腳本、`_Archive/Scenes/` 4 個場景 |
| 編譯 | 0 error 0 warning |

**下一步請看 [SystemsStatus.md](SystemsStatus.md) §6「建議順序」**（全景與理由），
細節再往 [RoadmapNext.md](RoadmapNext.md) §1 追。

> ⚠️ 有三件事卡在 Romtyui，建議**一次談完**（見 `SystemsStatus.md` §2.1）：
> 戰鬥結束事件、run 開始就初始化 HP／SAN、世界污染進度歸誰管。

---

## 8. 命名空間對照（最常搞混）

| 命名空間 | 內容 | 為什麼要分 |
|---|---|---|
| `EldritchMile.Core` | 流程總管、資料層、判定、UI 服務 | 與 `_Archive/` 的全域舊型別撞名，必須隔離 |
| `EldritchMile.Map` | 地圖繪製 | 同上（`MapData` / `RunNodeData` 撞名） |
| `EldritchMile.Explore` | 探索房間、互動物件、卡牌 UI | 同上（`RoomController` 等撞名） |
| （全域） | `Stages/MenuStageController`、保留的舊卡牌腳本 | 沒有撞名問題，且 Inspector 綁定少一層阻力 |

**跨命名空間引用時，`using` 記得寫在 `namespace` 裡面**（見 §4.2）。

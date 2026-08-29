# 依附件重建道具與對話的腳本

2026-08-29 用來把《食物.pdf》《收藏品.pdf》《對話.pdf》變成 Unity 資產的一次性工具。
**放在 Assets 外面**，免得 Unity 幫它們產生一堆 .meta。

| 檔案 | 做什麼 |
|---|---|
| `gen_items.py` | 產生 12 件食物 ＋ 17 件收藏品的 ItemData 資產、封存舊的、重寫 ItemDatabase |
| `gen_dialogue.py` | 產生兩個 ProbabilityDialogueData ＋ 兩張缺的戰利品子表 |
| ~~`restyle_pd.py`~~ | **已作廢** —— 它在 prefab 裡另做了一份對話框。改用下面那支 |
| `rewire_pd_shared_box.py` | 把機率對話改成驅動**共用的** DialogueBoxUI，拆掉本地那份 |
| `gate_no_effect.py` | 把「還沒有效果」的道具打上 NoEffect 標籤並排除出所有戰利品表 |
| `verify_loot.py` | **離線驗證**：每一條 TagQuery 與指名 itemId 都抽得到東西 |
| `sim_dialogue.py` | **離線模擬**：機率對話的平衡（20000 手） |
| `dumpitem.py` / `dumpui.py` | 把 Unity YAML 讀成人看得懂的樣子（debug 用） |

⚠️ 產生器會**覆寫**資產。要改內容就改腳本裡的表再重跑，
不要一邊改資產一邊改腳本 —— 下次重跑會蓋掉。

用法：`python Tools/spec_rebuild/verify_loot.py`（在專案根目錄執行）

# -*- coding: utf-8 -*-
"""機率對話改用「全專案共用的對話框」，只留下這個環節專屬的東西。

【為什麼要拆掉本地那份對話框】
使用者要求 1:1 復刻舊版的排版、比例與程式配置。舊版的對話框是
`PopupService` → `DialogueBoxUI`（場景裡的 DialogueUI），全專案共用，
分頁、打字機、推進鍵、名字框、立繪都在那一支。

我上一版在 prefab 裡另外做了一份，那會變成兩個長得不一樣的對話框，
而且分頁得再寫一次 —— 正是專案文件一直在警告的「兩個真相」。
所以本地那份整組拆掉，改由 View 去驅動共用的那一個。

【為什麼要自己一個 Canvas】
DialogueUI 的 sortingOrder 是 **101**，Canvas_Stage 是 **100** ——
回答列與手牌留在 Stage 底下的話會被 DialogueUI 的壓黑蓋住。
舊版是把 option_box 與 EncounterUI **放進 DialogueUI 裡**解決的，
但 prefab 不能引用場景物件，所以這裡改成
「Stage 自己一個 Canvas、overrideSorting、order 102」——
效果一樣，而且 Stage 仍然是自帶一份、不依賴場景結構。
"""
import io
import re
import sys

P = 'C:/Dev/ELDRITCH_MILE/Assets/TYN/Stages/Stage_ProbabilityDialogue.prefab'
ROOT_TF = '4289596397439847250'
ROOT_GO = None

CANVAS_ID = 1911000000000000101
RAYCAST_ID = 1911000000000000102
RAYCASTER_GUID = 'dc42784cf147c0c48a680349fa168899'

# 上一版加的本地對話框，整組拆掉
DROP = ['BLACK', 'Dialogbox', 'text_box', 'name_box', 'bodytext_box',
        'NpcName', 'Prompt', 'NpcPortrait', 'Background']

out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')
t = open(P, encoding='utf-8').read()


def docs():
    """[(cls, fid, start, end)]"""
    marks = list(re.finditer(r'^--- !u!(\d+) &(\d+).*$', t, re.M))
    res = []
    for i, m in enumerate(marks):
        end = marks[i + 1].start() if i + 1 < len(marks) else len(t)
        res.append((m.group(1), m.group(2), m.start(), end))
    return res


def go_of_name(name):
    for cls, fid, a, b in docs():
        if cls == '1' and re.search(r'^  m_Name: %s$' % re.escape(name), t[a:b], re.M):
            return fid
    return None


def body(fid):
    for cls, f, a, b in docs():
        if f == fid:
            return t[a:b]
    return ''


# ══════════════════════════════════════════════════════════
# 1. 收集要刪的 GameObject 與它們的所有元件
# ══════════════════════════════════════════════════════════
kill = set()
for n in DROP:
    go = go_of_name(n)
    if go is None:
        out.write('  （沒有 %s，跳過）\n' % n)
        continue
    kill.add(go)
    for c in re.findall(r'component: \{fileID: (\d+)\}', body(go)):
        kill.add(c)
    out.write('  拆掉 %s\n' % n)

ROOT_GO = re.search(r'm_GameObject: \{fileID: (\d+)\}', body(ROOT_TF)).group(1)

new_parts = []
for cls, fid, a, b in docs():
    if fid in kill:
        continue
    new_parts.append(t[a:b])
head = t[:docs()[0][2]]
t = head + ''.join(new_parts)


# ══════════════════════════════════════════════════════════
# 2. 根的子物件只留下這個環節專屬的三個
# ══════════════════════════════════════════════════════════
def tf_of_name(name):
    go = go_of_name(name)
    for c in re.findall(r'component: \{fileID: (\d+)\}', body(go)):
        if body(c).startswith('--- !u!224'):
            return c
    raise AssertionError(name)


keep = [tf_of_name('AnswerRoot'), tf_of_name('HandRoot'), tf_of_name('NoEffectHint')]

a, b = None, None
for cls, fid, s0, s1 in docs():
    if fid == ROOT_TF:
        a, b = s0, s1
seg = t[a:b]
h, rest = seg.split('m_Children:', 1)
_, tail = rest.split('m_Father:', 1)
t = t[:a] + h + 'm_Children:\n' + ''.join('  - {fileID: %s}\n' % k for k in keep) \
    + '  m_Father:' + tail + t[b:]
out.write('\n根底下只剩：AnswerRoot / HandRoot / NoEffectHint\n')


# ══════════════════════════════════════════════════════════
# 3. 根加上 Canvas（order 102）＋ GraphicRaycaster
# ══════════════════════════════════════════════════════════
if 'm_SortingOrder: 102' not in t:
    a, b = None, None
    for cls, fid, s0, s1 in docs():
        if fid == ROOT_GO:
            a, b = s0, s1
    seg = t[a:b]
    seg = seg.replace('  m_Layer: 0',
                      '  - component: {fileID: %d}\n  - component: {fileID: %d}\n  m_Layer: 0'
                      % (CANVAS_ID, RAYCAST_ID), 1)
    t = t[:a] + seg + t[b:]

    t = t.rstrip('\n') + '\n' + """--- !u!223 &%d
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %s}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 1
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_UseReflectionProbes: 0
  m_AdditionalShaderChannelsFlag: 25
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 102
  m_TargetDisplay: 0
--- !u!114 &%d
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: %s}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: %s, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GraphicRaycaster
  m_IgnoreReversedGraphics: 1
  m_BlockingObjects: 0
  m_BlockingMask:
    serializedVersion: 2
    m_Bits: 4294967295
""" % (CANVAS_ID, ROOT_GO, RAYCAST_ID, ROOT_GO, RAYCASTER_GUID)
    out.write('根加上 Canvas（overrideSorting、order 102）＋ GraphicRaycaster\n')


# ══════════════════════════════════════════════════════════
# 4. View 上那幾個指向本地對話框的欄位改成空
# ══════════════════════════════════════════════════════════
for field in ['backgroundImage', 'npcPortrait', 'npcNameText', 'promptText']:
    t = re.sub(r'^  %s: \{fileID: \d+\}$' % field, '  %s: {fileID: 0}' % field, t, flags=re.M)
out.write('View 的 backgroundImage／npcPortrait／npcNameText／promptText 清空（改用共用對話框）\n')

open(P, 'w', encoding='utf-8', newline='\n').write(t)
out.flush()

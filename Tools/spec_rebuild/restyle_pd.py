# -*- coding: utf-8 -*-
"""把 Stage_ProbabilityDialogue 的版型改成跟場景裡的 DialogueUI 一樣。

【為什麼是「複製一版」不是「沿用」】
DialogueUI 是**場景物件**，而 Stage_ProbabilityDialogue 是 prefab ——
prefab 不能引用場景物件（DialogueStageController 的 speakerHitbox 也踩過同一條）。
而且 DialogueUI 上掛著 DialogueBoxUI / DialogueOptionsPanel，
那些是舊那一套的驅動元件，兩個 Stage 同時驅動同一組物件會打架。

所以照使用者的指示：**複製貼上一版**，尺寸與圖檔全部照 DialogueUI 抄，
但住在 prefab 裡 —— 之後要單獨調機率對話的細項不會動到探索打牌。

【數字從哪來】EventScene 的 DialogueUI 子樹，兩邊都是 1920x1080 的
Reference Resolution、都是滿版子物件，所以座標可以直接搬。
"""
import io
import re
import sys

P = 'C:/Dev/ELDRITCH_MILE/Assets/TYN/Stages/Stage_ProbabilityDialogue.prefab'
IMAGE_SCRIPT = 'fe87c0e1cc204ed48ad3b37840f39efc'

SPR_TEXTBOX = '7b8a466c7b327c347b15caee28fa53d7'   # 對話＿文字框
SPR_NAMEBOX = '7a98af8bb02dc294e9754ff926e65e0d'   # 對話＿姓名框

ROOT_TF = '4289596397439847250'      # Stage_ProbabilityDialogue 的 RectTransform

out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')
t = open(P, encoding='utf-8').read()


# ── 小工具 ────────────────────────────────────────────────
def doc_span(fid):
    """回傳 &fid 那一份 document 的 (start, end)。"""
    m = re.search(r'^--- !u!\d+ &%s\b.*$' % fid, t, re.M)
    assert m, 'find &%s' % fid
    n = re.search(r'^--- !u!', t[m.end():], re.M)
    return m.start(), (m.end() + n.start()) if n else len(t)


def edit(fid, pairs):
    """改 &fid 這一份 document 裡的欄位。"""
    global t
    a, b = doc_span(fid)
    seg = t[a:b]
    for key, val in pairs:
        pat = re.compile(r'^(\s+%s: ).*$' % re.escape(key), re.M)
        assert pat.search(seg), '%s 裡找不到 %s' % (fid, key)
        seg = pat.sub(lambda m: m.group(1) + val, seg, count=1)
    t = t[:a] + seg + t[b:]


def set_father(tf, father):
    edit(tf, [('m_Father', '{fileID: %s}' % father)])


def children_of(tf):
    a, b = doc_span(tf)
    seg = t[a:b]
    blk = seg.split('m_Children:', 1)[1].split('m_Father:', 1)[0]
    return re.findall(r'\{fileID: (\d+)\}', blk)


def set_children(tf, ids):
    global t
    a, b = doc_span(tf)
    seg = t[a:b]
    head, rest = seg.split('m_Children:', 1)
    _, tail = rest.split('m_Father:', 1)
    body = ' []\n' if not ids else '\n' + ''.join('  - {fileID: %s}\n' % i for i in ids)
    t = t[:a] + head + 'm_Children:' + body + '  m_Father:' + tail + t[b:]


RT = """--- !u!224 &{tf}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: {sx}, y: {sy}, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:{children}  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {amnx}, y: {amny}}}
  m_AnchorMax: {{x: {amxx}, y: {amxy}}}
  m_AnchoredPosition: {{x: {px}, y: {py}}}
  m_SizeDelta: {{x: {w}, y: {h}}}
  m_Pivot: {{x: 0.5, y: 0.5}}
"""

GO = """--- !u!1 &{go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""

CR = """--- !u!222 &{cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_CullTransparentMesh: 1
"""

IMG = """--- !u!114 &{img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {{fileID: 0}}
  m_Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {sprite}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""

# 固定 id —— 重跑這支不會產生新的 fileID、不會製造假異動
IDS = {
    'BLACK':        (1911000000000000001, 1911000000000000002, 1911000000000000003, 1911000000000000004),
    'Dialogbox':    (1911000000000000011, 1911000000000000012, None, None),
    'text_box':     (1911000000000000021, 1911000000000000022, 1911000000000000023, 1911000000000000024),
    'name_box':     (1911000000000000031, 1911000000000000032, 1911000000000000033, 1911000000000000034),
    'bodytext_box': (1911000000000000041, 1911000000000000042, 1911000000000000043, 1911000000000000044),
}

new_docs = []


def make(name, father, rect, image=None, children=()):
    """新增一個物件。**立刻寫進 t** —— 後面的 set_children/edit 要找得到它。"""
    global t
    go, tf, cr, img = IDS[name]
    ch = (' []\n' if not children
          else '\n' + ''.join('  - {fileID: %s}\n' % c for c in children))
    comps = '  - component: {fileID: %s}\n' % tf
    if image is not None:
        comps += '  - component: {fileID: %s}\n' % cr
        comps += '  - component: {fileID: %s}\n' % img
    new_docs.append(GO.format(go=go, comps=comps, name=name))
    new_docs.append(RT.format(tf=tf, go=go, father=father, children=ch, **rect))
    if image is not None:
        new_docs.append(CR.format(cr=cr, go=go))
        new_docs.append(IMG.format(img=img, go=go, **image))
    t = t.rstrip('\n') + '\n' + ''.join(new_docs)
    del new_docs[:]
    return tf


def rect(px, py, w, h, sx=1, sy=1, amn=(0.5, 0.5), amx=(0.5, 0.5)):
    return dict(px=px, py=py, w=w, h=h, sx=sx, sy=sy,
                amnx=amn[0], amny=amn[1], amxx=amx[0], amxy=amx[1])


def img(sprite=None, color=(1, 1, 1, 1), raycast=1):
    return dict(sprite='{fileID: 0}' if not sprite else
                '{fileID: 21300000, guid: %s, type: 3}' % sprite,
                r=color[0], g=color[1], b=color[2], a=color[3],
                raycast=raycast, script=IMAGE_SCRIPT)


# ══════════════════════════════════════════════════════════
# 既有物件的 fileID（從 prefab 讀出來的）
# ══════════════════════════════════════════════════════════
def go_of_name(name):
    """找到 m_Name 是 name 的那一份 GameObject document。

    ⚠️ 不可以用「從檔頭 lazy match 到 m_Name」那種寫法 ——
    那種 lazy match 會跨過 document 邊界，抓到完全不相干的第一個 GameObject。
    """
    for m in re.finditer(r'^--- !u!1 &(\d+)$', t, re.M):
        a = m.start()
        n = re.search(r'^--- !u!', t[m.end():], re.M)
        b = (m.end() + n.start()) if n else len(t)
        if re.search(r'^  m_Name: %s$' % re.escape(name), t[a:b], re.M):
            return m.group(1)
    raise AssertionError('找不到 GameObject：%s' % name)


def comp_of_name(name, marker):
    go = go_of_name(name)
    a, b = doc_span(go)
    for c in re.findall(r'component: \{fileID: (\d+)\}', t[a:b]):
        ca, cb = doc_span(c)
        if marker in t[ca:cb]:
            return c
    raise AssertionError('%s 沒有 %s' % (name, marker))


def tf_of_name(name):
    return comp_of_name(name, 'RectTransform:')


TF_BG = tf_of_name('Background')
TF_PORTRAIT = tf_of_name('NpcPortrait')
TF_NAME = tf_of_name('NpcName')
TF_PROMPT = tf_of_name('Prompt')
TF_ANSWERS = tf_of_name('AnswerRoot')
TF_HAND = tf_of_name('HandRoot')
TF_HINT = tf_of_name('NoEffectHint')

TMP_NAME = comp_of_name('NpcName', 'TextMeshProUGUI')
TMP_PROMPT = comp_of_name('Prompt', 'TextMeshProUGUI')

# ══════════════════════════════════════════════════════════
# 新物件
# ══════════════════════════════════════════════════════════
tf_black = make('BLACK', ROOT_TF,
                rect(0, 0, 0, 0, amn=(0, 0), amx=(1, 1)),
                img(color=(0, 0, 0, 0.29411766)))

tf_namebox = make('name_box', IDS['text_box'][1],
                  rect(-650.67377, 115.05261, 1420, 210, 0.22, 0.22),
                  img(SPR_NAMEBOX))

tf_bodybox = make('bodytext_box', IDS['text_box'][1],
                  rect(-290, 0, 1035, 140),
                  img(SPR_NAMEBOX, color=(1, 1, 1, 0), raycast=0))

tf_textbox = make('text_box', IDS['Dialogbox'][1],
                  rect(0, -337.9, 2048, 426, 0.93, 0.93),
                  img(SPR_TEXTBOX),
                  children=[tf_namebox, TF_NAME, tf_bodybox])

tf_dialogbox = make('Dialogbox', ROOT_TF,
                    rect(0, 0, 100, 100),
                    None,
                    children=[tf_textbox])

# ── 搬家 ──
set_father(TF_NAME, tf_textbox)
set_father(TF_PROMPT, tf_bodybox)
set_children(tf_bodybox, [TF_PROMPT])

# ── 既有物件對齊 DialogueUI ──
edit(TF_NAME, [
    ('m_AnchorMin', '{x: 0.5, y: 0.5}'),
    ('m_AnchorMax', '{x: 0.5, y: 0.5}'),
    ('m_AnchoredPosition', '{x: -648.7, y: 114.2}'),
    ('m_SizeDelta', '{x: 281.4, y: 39}'),
    ('m_Pivot', '{x: 0.5, y: 0.5}'),
])
edit(TMP_NAME, [
    ('m_fontColor', '{r: 0, g: 0, b: 0, a: 1}'),
    ('m_fontSize', '36'),
    ('m_fontSizeBase', '36'),
])

edit(TF_PROMPT, [
    ('m_AnchorMin', '{x: 0.5, y: 0.5}'),
    ('m_AnchorMax', '{x: 0.5, y: 0.5}'),
    ('m_AnchoredPosition', '{x: 0, y: 0}'),
    ('m_SizeDelta', '{x: 1000, y: 135}'),
    ('m_Pivot', '{x: 0.5, y: 0.5}'),
])
# ⚠️ 附件的對話一段有四句，舊的 DialogueBoxUI 會分頁，這一套不會 ——
#    所以開自動縮字，長文至少不會被切掉。分頁是另一件事，見交接文件
edit(TMP_PROMPT, [
    ('m_fontSize', '36'),
    ('m_fontSizeBase', '36'),
    ('m_enableAutoSizing', '1'),
    ('m_fontSizeMin', '16'),
    ('m_fontSizeMax', '36'),
])

# 立繪：照 DialogueUI 的 character
edit(TF_PORTRAIT, [
    ('m_AnchorMin', '{x: 0.5, y: 0.5}'),
    ('m_AnchorMax', '{x: 0.5, y: 0.5}'),
    ('m_AnchoredPosition', '{x: 0, y: -334.78}'),
    ('m_SizeDelta', '{x: 1285, y: 2048}'),
    ('m_Pivot', '{x: 0.5, y: 0.5}'),
    ('m_LocalScale', '{x: 0.9, y: 0.9, z: 0.9}'),
])

# 回答列：DialogueUI 的 option_box 在 (0,-447.9)，三個選項的中心在 -667/-309/49，
# 所以整列的中心其實是 -309（美術稿本來就偏左）
edit(TF_ANSWERS, [
    ('m_AnchorMin', '{x: 0.5, y: 0.5}'),
    ('m_AnchorMax', '{x: 0.5, y: 0.5}'),
    ('m_AnchoredPosition', '{x: -309, y: -447.9}'),
    ('m_SizeDelta', '{x: 1074, y: 190}'),
    ('m_Pivot', '{x: 0.5, y: 0.5}'),
])

# 手牌：DialogueUI 的 EncounterUI/HandRoot 換算成絕對座標是 (530, 216.5)
edit(TF_HAND, [
    ('m_AnchorMin', '{x: 0.5, y: 0}'),
    ('m_AnchorMax', '{x: 0.5, y: 0}'),
    ('m_AnchoredPosition', '{x: 530, y: 216.5}'),
    ('m_Pivot', '{x: 0.5, y: 0.5}'),
])

# ── 根的子物件順序 = 畫面疊放順序 ──
set_children(ROOT_TF, [TF_BG, tf_black, TF_PORTRAIT, tf_dialogbox,
                       TF_ANSWERS, TF_HAND, TF_HINT])

t = t.rstrip('\n') + '\n' + ''.join(new_docs)
open(P, 'w', encoding='utf-8', newline='\n').write(t)
out.write('Stage_ProbabilityDialogue：版型已改成 DialogueUI 那一套\n')
out.write('  新增 BLACK / Dialogbox / text_box / name_box / bodytext_box\n')
out.write('  NpcName 與 Prompt 搬進對話框，立繪／回答列／手牌位置對齊\n')
out.flush()

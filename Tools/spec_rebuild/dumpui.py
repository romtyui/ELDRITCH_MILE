# -*- coding: utf-8 -*-
"""列出一棵 UI 子樹的 RectTransform 與 Image/TMP 設定。"""
import io
import re
import sys

DOC = re.compile(r'^--- !u!(\d+) &(\d+)(.*)$', re.M)


def load(path):
    txt = open(path, encoding='utf-8', errors='replace').read()
    docs = {}
    marks = list(DOC.finditer(txt))
    for i, m in enumerate(marks):
        end = marks[i + 1].start() if i + 1 < len(marks) else len(txt)
        docs[m.group(2)] = (m.group(1), txt[m.end():end])
    return docs


def f(body, name, d='-'):
    m = re.search(r'^\s+%s: (.*)$' % re.escape(name), body, re.M)
    return m.group(1).strip() if m else d


def main(path, root_name):
    docs = load(path)
    out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')

    go_name, go_comps, tf_of, children = {}, {}, {}, {}
    for fid, (cls, body) in docs.items():
        if cls == '1':
            go_name[fid] = f(body, 'm_Name', '')
            go_comps[fid] = re.findall(r'component: \{fileID: (\d+)\}', body)
        elif cls in ('4', '224'):
            mg = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
            if mg is None:
                continue
            g = mg.group(1)
            tf_of[g] = fid
            children[fid] = re.findall(r'\{fileID: (\d+)\}', body.split('m_Children:', 1)[1].split('m_Father:', 1)[0]) if 'm_Children:' in body else []

    root = None
    for fid, n in go_name.items():
        if n == root_name:
            root = tf_of.get(fid)
            break
    if root is None:
        out.write('找不到 %s\n' % root_name)
        out.flush()
        return

    def walk(tfid, depth):
        cls, body = docs[tfid]
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body).group(1)
        pad = '  ' * depth
        out.write('%s%s  [active=%s]\n' % (pad, go_name.get(g, '?'), f(docs[g][1], 'm_IsActive')))
        out.write('%s   rect anchorMin=%s anchorMax=%s pos=%s size=%s pivot=%s scale=%s\n' % (
            pad, f(body, 'm_AnchorMin'), f(body, 'm_AnchorMax'), f(body, 'm_AnchoredPosition'),
            f(body, 'm_SizeDelta'), f(body, 'm_Pivot'), f(body, 'm_LocalScale')))
        for c in go_comps.get(g, []):
            if c not in docs:
                continue
            ccls, cbody = docs[c]
            eci = f(cbody, 'm_EditorClassIdentifier', '')
            if 'Image' in eci:
                out.write('%s   Image enabled=%s sprite=%s color=%s type=%s preserve=%s raycast=%s\n' % (
                    pad, f(cbody, 'm_Enabled'), f(cbody, 'm_Sprite'), f(cbody, 'm_Color'),
                    f(cbody, 'm_Type'), f(cbody, 'm_PreserveAspect'), f(cbody, 'm_RaycastTarget')))
            elif 'TextMeshProUGUI' in eci:
                out.write('%s   TMP size=%s align=%s font=%s color=%s text=%s\n' % (
                    pad, f(cbody, 'm_fontSize'), f(cbody, 'm_textAlignment'),
                    f(cbody, 'm_fontAsset'), f(cbody, 'm_fontColor'), f(cbody, 'm_text')[:40]))
            elif eci and 'Rect' not in eci and 'CanvasRenderer' not in eci:
                out.write('%s   + %s\n' % (pad, eci.split('::')[-1]))
        for ch in children.get(tfid, []):
            if ch in docs:
                walk(ch, depth + 1)

    walk(root, 0)
    out.flush()


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])

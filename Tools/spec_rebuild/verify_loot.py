# -*- coding: utf-8 -*-
"""離線驗證：每一條 TagQuery 與指名 itemId 都真的抽得到東西。

抽不到的話 LootService 只會印一行 warning 就跳過 ——
在 Console 洗版裡很容易被忽略，然後玩家開了寶箱什麼都沒拿到。
"""
import glob
import io
import os
import re
import sys

ROOT = 'C:/Dev/ELDRITCH_MILE/Assets/TYN'
BS = chr(92)
UPAT = re.compile(BS + BS + r'u([0-9a-fA-F]{4})')
out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')


def unesc(s):
    return UPAT.sub(lambda m: chr(int(m.group(1), 16)), s)


def guid_of(meta):
    return re.search(r'guid: (\w+)', open(meta).read()).group(1)


# ── ItemDatabase 裡登記的道具 ──
db = open(ROOT + '/Core/ItemDatabase.asset', encoding='utf-8').read()
registered = set(re.findall(r'guid: (\w+), type: 2', db.split('items:', 1)[1]))

items = []   # (id, name, tags)
for p in glob.glob(ROOT + '/Core/Items/*.asset'):
    g = guid_of(p + '.meta')
    if g not in registered:
        continue
    txt = open(p, encoding='utf-8').read()
    iid = re.search(r'^  id: (.*)$', txt, re.M).group(1).strip()
    name = unesc(re.search(r'^  displayName: (.*)$', txt, re.M).group(1)).strip('"')
    tagblk = txt.split('  tags:', 1)[1].split('  grantsCard:', 1)[0]
    tags = [x.strip() for x in re.findall(r'^  - (.*)$', tagblk, re.M)]
    items.append((iid, name, tags))

out.write('ItemDatabase 登記 %d 件，資料夾裡 %d 件\n'
          % (len(registered), len(glob.glob(ROOT + '/Core/Items/*.asset'))))
ids = {i[0] for i in items}
assert len(ids) == len(items), '有重複的 id'
out.write('id 全部唯一 ✓\n\n')

# ── 每一張表 ──
problems = 0
tables = sorted(glob.glob(ROOT + '/Core/Loot/*.asset'))
for p in tables:
    txt = open(p, encoding='utf-8').read()
    if 'EldritchMile.Core.LootTable' not in txt:
        continue
    name = os.path.basename(p)[:-6]
    for m in re.finditer(
            r'    - kind: (\d)\n      itemId: ?(.*)\n      requireTags:((?:\n      - .*)*|\s*\[\])\n'
            r'      excludeTags:((?:\n      - .*)*|\s*\[\])\n', txt):
        kind = m.group(1)
        item_id = m.group(2).strip()
        req = re.findall(r'- (.*)', m.group(3))
        exc = re.findall(r'- (.*)', m.group(4))
        req = [x.strip() for x in req]
        exc = [x.strip() for x in exc]

        if kind == '0':
            if item_id not in ids:
                out.write('!! %-28s 指名的 itemId「%s」不在 ItemDatabase\n' % (name, item_id))
                problems += 1
            continue
        if kind != '1':
            continue

        hits = [i for i in items
                if all(any(t.lower() == r.lower() for t in i[2]) for r in req)
                and not any(any(t.lower() == e.lower() for t in i[2]) for e in exc)]
        label = '[%s]%s' % (', '.join(req), (' -[%s]' % ', '.join(exc)) if exc else '')
        if not hits:
            out.write('!! %-28s %s → 抽不到任何東西\n' % (name, label))
            problems += 1
        else:
            out.write('   %-28s %-40s %2d 件：%s\n'
                      % (name, label, len(hits),
                         '、'.join(h[1] for h in hits[:4]) + ('…' if len(hits) > 4 else '')))

out.write('\n%s\n' % ('全部通過 ✓' if problems == 0 else '有 %d 個問題' % problems))
out.flush()

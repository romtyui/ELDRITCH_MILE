# -*- coding: utf-8 -*-
"""把「還沒有效果」的道具擋在隨機取得之外。

使用者 2026-08-29 的指示：
「如果確認 Romtyui 未實作遺物和食物的效果，那沒有效果的遺物食物就先不要讓玩家獲得。」

【做法】給那些道具打一個 `NoEffect` 標籤，然後在每一條 TagQuery 的
excludeTags 加上它。**這樣「解禁」只要把標籤拿掉就好** ——
Romtyui 補完某一件的效果，刪掉那一行 tag，它就自動回到所有池子裡，
不必回頭一張一張改戰利品表。

【判定「有沒有效果」的標準】
  · 收藏品：`relicEffect` 有沒有指到東西
  · 食物　：`hpRestore` / `sanRestore` 有沒有大於 0
  　　　　　（只有 hpCost/sanCost 的等於「只有壞處」，也算沒效果）
  · 武器牌：`grantsCard` 有沒有指到牌
"""
import glob
import io
import os
import re
import sys

ROOT = 'C:/Dev/ELDRITCH_MILE/Assets/TYN'
TAG = 'NoEffect'
BS = chr(92)
UP = re.compile(BS + BS + r'u([0-9a-fA-F]{4})')
out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')


def unesc(s):
    return UP.sub(lambda m: chr(int(m.group(1), 16)), s)


def field(t, k, d='0'):
    m = re.search(r'^  %s: (.*)$' % k, t, re.M)
    return m.group(1).strip() if m else d


def works(t, tags):
    if 'Curio' in tags:
        return 'fileID: 0}' not in field(t, 'relicEffect')
    if 'Weapon' in tags:
        return 'fileID: 0}' not in field(t, 'grantsCard')
    return int(field(t, 'hpRestore')) > 0 or int(field(t, 'sanRestore')) > 0


# ══════════════════════════════════════════════════════════
# 1. 幫沒效果的道具打／拿掉 NoEffect 標籤
# ══════════════════════════════════════════════════════════
gated, freed = [], []
for p in sorted(glob.glob(ROOT + '/Core/Items/Item_*.asset')):
    t = open(p, encoding='utf-8').read()
    head, rest = t.split('  tags:\n', 1)
    tagblk, tail = rest.split('  grantsCard:', 1)
    tags = [x.strip() for x in re.findall(r'^  - (.*)$', tagblk, re.M)]

    name = unesc(field(t, 'displayName')).strip('"')
    want = not works(t, tags)
    has = TAG in tags

    if want == has:
        continue

    if want:
        tags.append(TAG)
        gated.append(name)
    else:
        tags.remove(TAG)
        freed.append(name)

    newblk = ''.join('  - %s\n' % x for x in tags)
    open(p, 'w', encoding='utf-8', newline='\n').write(
        head + '  tags:\n' + newblk + '  grantsCard:' + tail)

out.write('打上 %s 的：%d 件\n' % (TAG, len(gated)))
for n in gated:
    out.write('  · %s\n' % n)
if freed:
    out.write('\n解禁（效果已接上）：%d 件\n' % len(freed))
    for n in freed:
        out.write('  · %s\n' % n)

# ══════════════════════════════════════════════════════════
# 2. Loot_Sub_Relics：三個品質階層現在只剩「普通」有東西
# ══════════════════════════════════════════════════════════
p = ROOT + '/Core/Loot/Loot_Sub_Relics.asset'
t = open(p, encoding='utf-8').read()
head = t.split('  pools:', 1)[0]
note = ('\\u6536\\u85CF\\u54C1\\u3002'                       # 收藏品。
        '\\u2757 \\u539F\\u672C\\u662F\\u666E\\u901A 50 / '   # ❗ 原本是普通 50 /
        '\\u7F55\\u898B 33 / \\u7A00\\u6709 17 '              # 罕見 33 / 稀有 17
        '\\u4E09\\u968E\\uFF0C\\u4F46\\u76EE\\u524D\\u53EA\\u6709'  # 三階，但目前只有
        '\\u4EBA\\u9B5A\\u7684\\u756B\\u50CF\\u63A5\\u4E0A\\u6548\\u679C\\uFF0C'  # 人魚的畫像接上效果，
        '\\u53E6\\u5169\\u968E\\u6703\\u62BD\\u4E0D\\u5230\\u6771\\u897F\\u3002'   # 另兩階會抽不到東西。
        '\\u6548\\u679C\\u88DC\\u9F4A\\u5F8C\\u8ACB\\u6539\\u56DE\\u4E09\\u968E')  # 效果補齊後請改回三階
t = head + '  pools:\n'
t += ('  - note: "%s"\n    chance: 1\n    rollsMin: 1\n    rollsMax: 1\n'
      '    distinct: 1\n    entries:\n' % note)
t += ('    - kind: 1\n      itemId: \n      requireTags:\n      - Curio\n'
      '      excludeTags:\n      - EventOnly\n      - %s\n'
      '      table: {fileID: 0}\n      weight: 100\n'
      '      countMin: 1\n      countMax: 1\n' % TAG)
open(p, 'w', encoding='utf-8', newline='\n').write(t)
out.write('\nLoot_Sub_Relics：三階品質 → 單一條目（只剩普通階有東西）\n')

# ══════════════════════════════════════════════════════════
# 3. 每一條 TagQuery 都排除 NoEffect
# ══════════════════════════════════════════════════════════
ENTRY = re.compile(
    r'(    - kind: 1\n      itemId: ?\n      requireTags:(?:\n      - .*)+\n'
    r'      excludeTags:)((?:\n      - .*)*|\s*\[\])\n')


def add_exclude(m):
    head, exc = m.group(1), m.group(2)
    tags = re.findall(r'- (.*)', exc)
    tags = [x.strip() for x in tags]
    if TAG not in tags:
        tags.append(TAG)
    return head + '\n' + ''.join('      - %s\n' % x for x in tags)


touched = 0
for p in sorted(glob.glob(ROOT + '/Core/Loot/*.asset')):
    t = open(p, encoding='utf-8').read()
    if 'EldritchMile.Core.LootTable' not in t:
        continue
    t2, n = ENTRY.subn(add_exclude, t)
    if t2 != t:
        open(p, 'w', encoding='utf-8', newline='\n').write(t2)
        touched += 1
        out.write('  %s：%d 條 TagQuery 加上 exclude %s\n'
                  % (os.path.basename(p)[:-6], n, TAG))

out.write('\n共改了 %d 張戰利品表\n' % touched)
out.flush()

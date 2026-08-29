# -*- coding: utf-8 -*-
"""機率對話平衡模擬。

牌組取自 Stage_Explore.prefab / Stage_Dialogue.prefab 的 startingDeck：
3 屬性 x {20,40,60,80,100} = 15 張，平均 60（**沒有 0 的牌** ——
0 只存在於 PDialogue_Gatekeeper 的 fallbackCards）。

Deal() 洗過索引後依序取，牌組 15 張 > handSize 5，所以手牌不重複。

────────────────────────────────────────────────────────
【兩種成長公式】

  Additive        P += 牌面值               （Romtyui 規格書原本寫的）
  Multiplicative  P *= (1 + 牌面值/100)     （2026-08-29 使用者提的）

使用者給的兩個例子都對得上乘法：
    25% 用一張 100 → 25 x 2.0  = 50%
    50% 用一張 80  → 50 x 1.8  = 90%

⚠️ 乘法有一個加法沒有的邊界：**P = 0 時任何牌都無效**（0 x 任何數 = 0）。
   baseProbability 不要填 0。
"""
import io
import random
import sys

out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')

ID, SUP, EGO = 1, 2, 3
VALUES = [20, 40, 60, 80, 100]
DECK = [(a, v) for a in (ID, SUP, EGO) for v in VALUES]


# AttributeChart.asset：本我<->超我 = None(0x)，同屬性 = Match(1x)，其餘 = Partial(0.5x)
def chart_mult(card_attr, opt_attr):
    if card_attr == opt_attr:
        return 1.0
    if {card_attr, opt_attr} == {ID, SUP}:
        return 0.0
    return 0.5


class Rules:
    def __init__(self, name, base=25, hand=5, scale=1.0, cap=100,
                 use_chart=False, growth='add'):
        self.name, self.base, self.hand = name, base, hand
        self.scale, self.cap, self.use_chart, self.growth = scale, cap, use_chart, growth

    def factor(self, card, accepted):
        """這張牌對這個回答的有效牌面值（已套倍率與縮放）。不相符回 None。"""
        a, v = card
        if self.use_chart:
            m = max(chart_mult(a, x) for x in accepted)
            if m <= 0:
                return None
        else:
            m = 1.0 if a in accepted else None
            if m is None:
                return None
        return v * self.scale * m

    def apply(self, p, card, accepted):
        f = self.factor(card, accepted)
        if f is None:
            return p
        if self.growth == 'mul':
            return min(self.cap, int(round(p * (1.0 + f / 100.0))))
        return min(self.cap, p + int(round(f)))


def best_reachable(rules, hand, accepted):
    """把手上所有有效的牌都倒進這個回答，能到幾 %。"""
    p = rules.base
    for c in hand:
        p = rules.apply(p, c, accepted)
    return p


def run(rules, accepted_sets, trials=20000, seed=1):
    rng = random.Random(seed)
    hits = 0
    ps = []
    split = []
    for _ in range(trials):
        hand = rng.sample(DECK, rules.hand)
        p = best_reachable(rules, hand, accepted_sets[0])
        ps.append(p)
        if p >= 100:
            hits += 1

        # 分兩路：每張牌指派給「它能推得比較高的那個回答」
        a, b = accepted_sets[0], accepted_sets[1]
        pa, pb = rules.base, rules.base
        for c in hand:
            na, nb = rules.apply(pa, c, a), rules.apply(pb, c, b)
            if na - pa >= nb - pb:
                pa = na
            else:
                pb = nb
        # 先賭 a，失敗再賭 b
        split.append(pa / 100.0 + (1 - pa / 100.0) * (pb / 100.0))

    n = len(ps)
    ps_sorted = sorted(ps)
    return {
        'mean': sum(ps) / n,
        'median': ps_sorted[n // 2],
        'pct100': 100.0 * hits / n,
        'pct90': 100.0 * sum(1 for x in ps if x >= 90) / n,
        'split': 100.0 * sum(split) / n,
    }


def table(title, accepted_sets, cases):
    out.write('\n' + title + '\n')
    out.write('  回答接受的屬性：%s\n' % (accepted_sets,))
    out.write('  %-32s %7s %7s %8s %8s %10s\n'
              % ('設定', '平均%', '中位%', '>=90%', '=100%', '分兩路EV%'))
    out.write('  ' + '-' * 78 + '\n')
    for r in cases:
        s = run(r, accepted_sets)
        out.write('  %-32s %6.1f  %6d  %6.1f%% %6.1f%% %9.1f%%\n'
                  % (r.name, s['mean'], s['median'], s['pct90'], s['pct100'], s['split']))


CASES = [
    Rules('加法（規格原本）', 25, 5, 1.0, growth='add'),
    Rules('加法 + 牌值 x0.25', 25, 5, 0.25, growth='add'),
    Rules('乘法 1+x（新提案）', 25, 5, 1.0, growth='mul'),
    Rules('乘法 + base 15', 15, 5, 1.0, growth='mul'),
    Rules('乘法 + base 10', 10, 5, 1.0, growth='mul'),
    Rules('乘法 + 牌值 x0.5', 25, 5, 0.5, growth='mul'),
    Rules('乘法 + 相剋表', 25, 5, 1.0, growth='mul', use_chart=True),
    Rules('乘法 + 相剋表 + base 10', 10, 5, 1.0, growth='mul', use_chart=True),
]


def worked_examples():
    """把使用者給的兩個例子跑一次，確認公式對得上。"""
    r = Rules('mul', growth='mul')
    a = (ID,)
    out.write('\n【對照使用者給的例子】\n')
    p = 25
    p2 = r.apply(p, (ID, 100), a)
    out.write('  25%% 用一張 100 → %d%%（預期 50）%s\n' % (p2, '✓' if p2 == 50 else '✗'))
    p3 = r.apply(50, (ID, 80), a)
    out.write('  50%% 用一張 80  → %d%%（預期 90）%s\n' % (p3, '✓' if p3 == 90 else '✗'))
    out.write('  0%% 用一張 100  → %d%%（乘法的死角：0 乘不動）\n' % r.apply(0, (ID, 100), a))


if __name__ == '__main__':
    worked_examples()
    table('=== 回答收 2 種屬性（PDialogue_Gatekeeper 現況）===',
          [(ID, EGO), (SUP, EGO), (ID, SUP)], CASES)
    table('=== 回答各收 1 種屬性（魔術秀／有…魚…！ 就是這樣）===',
          [(ID,), (SUP,), (EGO,)], CASES)
    out.flush()

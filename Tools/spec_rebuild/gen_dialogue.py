# -*- coding: utf-8 -*-
"""依《對話.pdf》產生兩個機率對話事件，以及它們需要的兩張戰利品子表。

【附件沒有寫的東西，這裡是怎麼補的】
附件的三個選項各自都是「答對」的分支，沒有失敗文本 ——
但機率對話的規則是「選一個、擲骰、失敗就移掉這個回答」，
所以 failurePrompts / finalFailureText 是照角色語氣自撰的，
文案要換的話直接改這支或改資產，程式不必動。

【屬性怎麼分配】附件沒指定。三個回答各給一種屬性（本我／超我／自我），
這樣牌組建構才會影響「你能爭取哪一個獎勵」——
三個回答都吃同一種屬性的話，選哪個都一樣。
"""
import hashlib
import io
import os
import re
import sys

ROOT = 'C:/Dev/ELDRITCH_MILE'
LOOT_DIR = ROOT + '/Assets/TYN/Core/Loot'
PD_DIR = ROOT + '/Assets/TYN/Core/ProbabilityDialogue/Data'

LOOTTABLE_SCRIPT = None      # 從既有資產讀
PDIALOGUE_SCRIPT = None

ID, SUPEREGO, EGO = 1, 2, 3
BS = chr(92)

LOOT_SUB_WEAPONS = 'dddbf38c09fe28c4190fa2474558c70e'
LOOT_SUB_FOOD_VILLAGE = '9a91b781cfaa79a4ab4104959773a981'

out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')


def esc(s):
    o = []
    for ch in s:
        n = ord(ch)
        if ch == BS:
            o.append(BS + BS)
        elif ch == '"':
            o.append(BS + '"')
        elif ch == '\n':
            o.append(BS + 'n')
        elif n < 128:
            o.append(ch)
        else:
            o.append(BS + 'u%04X' % n)
    return '"' + ''.join(o) + '"'


def guid_for(name):
    return hashlib.md5(('EldritchMile/PD/' + name).encode('utf-8')).hexdigest()


META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def script_guid_of(path):
    return re.search(r'm_Script: \{fileID: 11500000, guid: (\w+)', open(path, encoding='utf-8').read()).group(1)


def write(path, body, name):
    open(path, 'w', encoding='utf-8', newline='\n').write(body)
    m = path + '.meta'
    if os.path.exists(m):
        g = re.search(r'guid: (\w+)', open(m).read()).group(1)
    else:
        g = guid_for(name)
        open(m, 'w', encoding='utf-8', newline='\n').write(META.format(guid=g))
    return g


HEAD = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script}, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: {cls}
"""


# ══════════════════════════════════════════════════════════
# 兩張缺的戰利品子表
#   附件的獎勵寫「隨機一個稀有度為【普通】的食物／收藏品」，
#   現成的 Loot_Sub_* 沒有按品質分的版本
# ══════════════════════════════════════════════════════════
def loot_body(script, name, note, entries):
    b = HEAD.format(script=script, name=name, cls='Assembly-CSharp::EldritchMile.Core.LootTable')
    b += '  pools:\n  - note: %s\n    chance: 1\n    rollsMin: 1\n    rollsMax: 1\n    distinct: 1\n    entries:\n' % esc(note)
    for require, exclude, weight in entries:
        b += '    - kind: 1\n      itemId: \n      requireTags:\n'
        b += ''.join('      - %s\n' % t for t in require)
        b += ('      excludeTags:\n' + ''.join('      - %s\n' % t for t in exclude)) if exclude else '      excludeTags: []\n'
        b += '      table: {fileID: 0}\n      weight: %d\n      countMin: 1\n      countMax: 1\n' % weight
    return b


def main():
    loot_script = script_guid_of(LOOT_DIR + '/Loot_Sub_Weapons.asset')
    pd_script = script_guid_of(PD_DIR + '/PDialogue_Gatekeeper.asset')

    g_food_common = write(
        LOOT_DIR + '/Loot_Sub_Food_Common.asset',
        loot_body(loot_script, 'Loot_Sub_Food_Common',
                  '品質【普通】的食物。《魔術秀》的「裡面是食物」用這張',
                  [(['Food', 'Common'], [], 100)]),
        'Loot_Sub_Food_Common')
    out.write('Loot_Sub_Food_Common       %s\n' % g_food_common)

    g_curio_common = write(
        LOOT_DIR + '/Loot_Sub_Curio_Common.asset',
        loot_body(loot_script, 'Loot_Sub_Curio_Common',
                  '品質【普通】的收藏品。事件限定的那幾件排除在外',
                  [(['Curio', 'Common'], ['EventOnly'], 100)]),
        'Loot_Sub_Curio_Common')
    out.write('Loot_Sub_Curio_Common      %s\n' % g_curio_common)

    # ── 手牌備援：沿用 Gatekeeper 那 18 張（F1 直接跳關時才會用到）──
    gk = open(PD_DIR + '/PDialogue_Gatekeeper.asset', encoding='utf-8').read()
    fb = gk.split('fallbackCards:', 1)[1].split('options:', 1)[0]
    fallback = ''.join('  - {fileID: 11400000, guid: %s, type: 2}\n' % g
                       for g in re.findall(r'guid: (\w+)', fb))

    def effects(items):
        """items: [(kind, key, amount, tableGuid)]"""
        if not items:
            return ' []\n'
        s = '\n'
        for kind, key, amount, table in items:
            s += '  - kind: %d\n    key: %s\n    amount: %d\n    table: %s\n' % (
                kind, key, amount,
                '{fileID: 0}' if not table else '{fileID: 11400000, guid: %s, type: 2}' % table)
        return s

    def opt_effects(items):
        if not items:
            return ' []\n'
        s = '\n'
        for kind, key, amount, table in items:
            s += '    - kind: %d\n      key: %s\n      amount: %d\n      table: %s\n' % (
                kind, key, amount,
                '{fileID: 0}' if not table else '{fileID: 11400000, guid: %s, type: 2}' % table)
        return s

    def attrs(lst):
        """List<ExploreAttribute> 在 YAML 裡是 4 bytes little-endian 的 hex 串。"""
        return ''.join('%02X%02X%02X%02X' % (a & 0xFF, 0, 0, 0) for a in lst).lower()

    def dialogue(name, event_id, npc, prompt, fails, final, options, cap=100, hand=5, base=20):
        b = HEAD.format(script=pd_script, name=name,
                        cls='Assembly-CSharp::EldritchMile.Core.ProbabilityDialogue.ProbabilityDialogueData')
        b += '  eventId: %s\n' % event_id
        b += '  npcId: %s\n' % npc
        b += '  background: {fileID: 0}\n'
        b += '  initialPrompt: %s\n' % esc(prompt)
        b += '  failurePrompts:\n' + ''.join('  - %s\n' % esc(f) for f in fails)
        b += '  finalFailureText: %s\n' % esc(final)
        b += '  handSize: %d\n' % hand
        b += '  fallbackCards:\n' + fallback
        b += '  options:\n'
        for oid, text, attr, success, outcome in options:
            b += '  - optionId: %s\n' % oid
            b += '    text: %s\n' % esc(text)
            b += '    baseProbability: %d\n' % base
            b += '    acceptedAttributes: %s\n' % attrs([attr])
            b += '    successText: %s\n' % esc(success)
            b += '    successOutcome:%s' % opt_effects(outcome)
        b += '  terminalFailureOutcome: []\n'
        b += '  probabilityCap: %d\n' % cap
        return b

    GRANT_TABLE = 8   # EventEffect.Kind.GrantFromTable

    # ══════════════════════════════════════════════════════
    # 【魔術秀】坎貝爾．地點不限
    # ══════════════════════════════════════════════════════
    magic = dialogue(
        'PDialogue_MagicShow', 'village_magic_show', 'campbell',
        prompt=(
            '坎貝爾：「好，來表演魔術吧！」\n\n'
            '坎貝爾：「別一副『這種時候？』的表情嘛！偶爾也得來點娛樂活動吧？\n'
            '而且，我還為努力的你準備了獎勵哦！要是拿到的話不是很開心嗎？」\n\n'
            '坎貝爾：「那麼，問題～！\n'
            '這個看似平平無奇，但其實暗藏玄機的帽子中到底藏了什麼呢？」\n\n'
            '坎貝爾：「帽子中可謂是混沌！在看到之前，都無法確定裡面隱藏了什麼。\n'
            '來吧！這種未知的感覺也是一種刺激！」'),
        fails=[
            '坎貝爾：「唔——差一點點！」\n他把帽子往你面前又送了送。「再想想？帽子還在這裡呢。」',
            '坎貝爾：「最後一次機會囉。」\n笑容沒有變，但他的手指在帽沿上敲得比剛才快了一些。',
        ],
        final='坎貝爾：「唉呀，這次是我贏了。」\n他把帽子扣回頭上，到最後也沒讓你看見裡面有什麼。',
        options=[
            ('food', '「裡面是食物。」', ID,
             '坎貝爾：「正確，你還真是個貪吃鬼呢！\n'
             '前面的路還有很長，多吃點東西補充體力吧！」',
             [(GRANT_TABLE, '', 1, g_food_common)]),

            ('weapon', '「裡面是武器。」', SUPEREGO,
             '坎貝爾：「答對了！有能保護自己的力量比什麼都還要重要！\n'
             '要是覺得害怕的話就想想在你身邊的我吧！」',
             [(GRANT_TABLE, '', 1, LOOT_SUB_WEAPONS)]),

            ('curio', '「裡面是剛才路邊撿到的東西。」', EGO,
             '坎貝爾：「誒你看到了？啊，不對啦！\n'
             '就算看起來像垃圾，也是有用的收藏品哦！拿在手上總會有好事的！」',
             [(GRANT_TABLE, '', 1, g_curio_common)]),
        ])
    g = write(PD_DIR + '/PDialogue_MagicShow.asset', magic, 'PDialogue_MagicShow')
    out.write('PDialogue_MagicShow        %s\n' % g)

    # ══════════════════════════════════════════════════════
    # 【有...魚...！】時藏．地點不限
    # ══════════════════════════════════════════════════════
    fish = dialogue(
        'PDialogue_ThereAreFish', 'village_there_are_fish', 'tokizo',
        prompt=(
            '時藏：「這裡...有魚。」\n\n'
            '路上，時藏突然停了下來。\n'
            '在他的面前，有一個十分突兀的池子，裡面貌似有什麼東西的影子在游動。\n\n'
            '時藏：「來抓...魚！我...很擅長...餓...要吃！」\n\n'
            '時藏看向你，似乎是在詢問你的意見。'),
        fails=[
            '時藏看著水面，又看看你。\n「...那個，不行嗎？」',
            '池子裡的影子動得慢了下來。時藏的肚子叫了一聲。\n「快...要沒有了。」',
        ],
        final='等你們終於決定好的時候，池面已經恢復平靜了。\n時藏蹲在岸邊，沒有再說話。',
        options=[
            ('dive', '「跳下去抓抓吧！」', ID,
             '時藏：「交給...我！我很...擅長...抓魚！」\n\n'
             '說完，時藏便跳入池中，揚起巨大的水花。\n'
             '池子表面恢復平靜，彷彿什麼都沒有發生過。\n\n'
             '過了數分鐘，池子依舊沒有任何動靜。\n'
             '你不由得有些擔心。\n\n'
             '就在你忐忑不安地四處走動時——\n\n'
             '時藏：「呼、哇啊——！」\n\n'
             '時藏從池中爬了上來，手上抓著幾隻看著像魚的生物。\n\n'
             '時藏：「吃飽...了！這是...你的份...！」\n\n'
             '他把剛抓到的收穫交給了你，然後一副沒事人的樣子準備繼續前進。\n\n'
             '你收下了那份禮物，儘管手上因此沾上了一些魚腥味。\n'
             '至少他看起來挺開心的。',
             [(GRANT_TABLE, '', 1, LOOT_SUB_FOOD_VILLAGE)] * 3),

            ('scoop', '「在岸邊撈撈看吧！」', EGO,
             '時藏：「我...知道了...！」\n\n'
             '時藏沒有冒然跳入池中抓魚，而是乖乖地站在池邊用手撈魚。\n'
             '動作看上去就像一隻貓。\n\n'
             '過了數分鐘，你聽見池邊傳來一陣動靜。\n'
             '時藏帶著像是某種魚類的生物來到你的身旁。\n\n'
             '時藏：「這個...給你！是...謝禮！你也...吃！」\n\n'
             '你收下了那份禮物，儘管手上因此沾上了一些魚腥味。\n'
             '至少他看起來挺開心的。',
             [(GRANT_TABLE, '', 1, LOOT_SUB_FOOD_VILLAGE)]),

            ('tools', '「嘗試尋找周圍的工具。」', SUPEREGO,
             '你們在周圍探索了一番，找到了勉強堪用的工具。\n'
             '等回到池子旁邊時，池中的黑影就已經消失不見了。\n\n'
             '雖然最後沒能抓到魚，讓時藏有些失望，'
             '但剛才找到的工具貌似能用在其他地方。',
             [(GRANT_TABLE, '', 1, LOOT_SUB_WEAPONS)]),
        ])
    g = write(PD_DIR + '/PDialogue_ThereAreFish.asset', fish, 'PDialogue_ThereAreFish')
    out.write('PDialogue_ThereAreFish     %s\n' % g)
    out.flush()


if __name__ == '__main__':
    main()

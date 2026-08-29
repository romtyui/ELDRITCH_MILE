# -*- coding: utf-8 -*-
"""依《食物.pdf》《收藏品.pdf》產生 ItemData 資產。

【數值換算】附件寫的是「微量／小量／中等／大量」，不是數字。
玩家 HP／SAN 上限都是 100（GameFlowManager 的 startingMaxHp/San），
所以這裡定成：微量 5、小量 10、中等 20、大量 35；SAN 小量 10、中等 20。
換算表放在這支程式與 Docs/ItemsFromSpec.md，兩邊要一起改。
"""
import hashlib
import io
import os
import re
import sys

ROOT = 'C:/Dev/ELDRITCH_MILE'
ITEMS = ROOT + '/Assets/TYN/Core/Items'
ARCHIVE = ROOT + '/Assets/TYN/_Archive/Items'

ITEMDATA_SCRIPT_GUID = '73b3ac59f45a61146ae44b55edfe3c3c'
HEALING_10PCT_GUID = 'dbb6b8b508589c541a50cbae19daebee'   # RelicEffect_HealingReceived_10Percent

BS = chr(92)


def esc(s):
    """Unity 寫 YAML 字串的方式：非 ASCII 一律 \\uXXXX。"""
    out = []
    for ch in s:
        o = ord(ch)
        if ch == BS:
            out.append(BS + BS)
        elif ch == '"':
            out.append(BS + '"')
        elif ch == '\n':
            out.append(BS + 'n')
        elif o < 128:
            out.append(ch)
        else:
            out.append(BS + 'u%04X' % o)
    return '"' + ''.join(out) + '"'


def guid_for(name):
    """由檔名決定的固定 GUID —— 重跑這支程式不會換 GUID、不會製造假異動。"""
    return hashlib.md5(('EldritchMile/Item/' + name).encode('utf-8')).hexdigest()


ASSET = """%YAML 1.1
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
  m_Name: {file}
  m_EditorClassIdentifier: Assembly-CSharp::EldritchMile.Core.ItemData
  id: {id}
  displayName: {name}
  icon: {icon}
  description: {desc}
  fullDescription: {full}
  price: {price}
  tags:
{tags}  grantsCard: {{fileID: 0}}
  hpRestore: {hp}
  sanRestore: {san}
  hpCost: {hpc}
  sanCost: {sanc}
  consumeOnUse: 1
  relicEffect: {relic}
  notes: {notes}
"""

META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def sprite_ref(guid):
    return '{fileID: 0}' if not guid else '{fileID: 21300000, guid: %s, type: 3}' % guid


def so_ref(guid):
    return '{fileID: 0}' if not guid else '{fileID: 11400000, guid: %s, type: 2}' % guid


def split_notes(text):
    """把敘述切成「玩家看得到的」與「製作備註」。

    **第一行以 ※ 開頭之後的全部** 都算備註 —— 包含它下面縮排的續行。
    這樣規格表可以照抄附件、一段寫完，不必在兩個欄位之間手動搬。

    ⚠️ 之所以要切，是因為 `description` 會**直接出現在快捷欄的說明框裡** ——
    「※ 戰鬥端未接」那種字玩家會看到。
    """
    body, note = [], []
    hit = False
    for line in text.split('\n'):
        if not hit and line.lstrip().startswith('※'):
            hit = True
        (note if hit else body).append(line)
    return '\n'.join(body).rstrip(), '\n'.join(note).strip()


def split_effect(text):
    """把「故事」與「效果」切開。

    第一行以【效果】開頭之後的全部算效果，前面的算故事。

    ⚠️ 快捷欄 hover 的說明框**只顯示效果** —— hover 的當下玩家要的是
    「這個吃下去會怎樣」，不是讀一段故事。故事留給日後的圖鑑
    （`ItemData.fullDescription`，用 `FullText` 接起來顯示）。
    """
    story, effect = [], []
    hit = False
    for line in text.split('\n'):
        if not hit and line.lstrip().startswith('【效果】'):
            hit = True
        (effect if hit else story).append(line)
    return '\n'.join(story).strip(), '\n'.join(effect).strip()


def write_item(spec):
    file = spec['file']
    path = os.path.join(ITEMS, file + '.asset')

    body, notes = split_notes(spec.get('desc', ''))
    full, desc = split_effect(body)
    if spec.get('notes'):
        notes = (notes + '\n' + spec['notes']).strip()

    body = ASSET.format(
        script=ITEMDATA_SCRIPT_GUID,
        file=file,
        id=spec['id'],
        name=esc(spec['name']),
        icon=sprite_ref(spec.get('icon')),
        desc=esc(desc) if desc else '',
        full=esc(full) if full else '',
        price=spec.get('price', 0),
        tags=''.join('  - %s\n' % t for t in spec['tags']),
        hp=spec.get('hp', 0),
        san=spec.get('san', 0),
        hpc=spec.get('hpCost', 0),
        sanc=spec.get('sanCost', 0),
        relic=so_ref(spec.get('relic')),
        notes=esc(notes) if notes else '',
    )
    open(path, 'w', encoding='utf-8', newline='\n').write(body)

    mpath = path + '.meta'
    if os.path.exists(mpath):
        guid = re.search(r'guid: (\w+)', open(mpath).read()).group(1)
    else:
        guid = guid_for(file)
        open(mpath, 'w', encoding='utf-8', newline='\n').write(META.format(guid=guid))
    return guid


# ══════════════════════════════════════════════════════════════
# 食物 —— 《食物.pdf》12 件，順序照附件
# ══════════════════════════════════════════════════════════════
F = 'Consumable'
FOOD = [
    dict(file='Item_food_dry_bread', id='food_dry_bread', name='乾麵包',
         tags=[F, 'Food', 'Common'], price=8, hp=10,
         desc='口感乾硬的麵包，雖然可能會咬壞牙齒，但在這個世界算是能優秀地補充能量的食物。\n\n'
              '【效果】恢復（小量）HP。'),

    dict(file='Item_food_bottled_water', id='food_bottled_water', name='瓶裝水',
         tags=[F, 'Food', 'Common'], price=5, hp=5,
         desc='還裝有些許清水的瓶子，但裡頭能聞到某種臭味，似乎放了許久，'
              '或許塑料瓶對環境帶來的污染更加可怕。\n\n'
              '【效果】恢復（微量）HP。'),

    dict(file='Item_food_industrial_alcohol', id='food_industrial_alcohol', name='工業酒精',
         tags=[F, 'Food', 'Common'], price=14, hp=10, san=10,
         desc='瓶身的標籤早已變得模糊不清，光是飲下一口喉嚨便如被火焰灼燒一般，'
              '剎那間所有的恐怖和不安都煙消雲散。\n\n'
              '【效果】恢復（小量）HP ＆ 恢復（小量）SAN ＆ 下回合開始時抽取的手牌 -1。\n'
              '※ 戰鬥端未接：「下回合抽牌 -1」需要 Romtyui 的 ItemEffectData。'),

    dict(file='Item_food_strange_fish', id='food_strange_fish', name='奇怪的魚',
         tags=[F, 'Food', 'SeaFood', 'Common'], price=16, hp=20, sanCost=10,
         desc='那是否能稱作為「魚」呢？酷似人類的嘴中偶爾會發出不知名的呢喃。\n\n'
              '【效果】恢復（中等）HP ＆ 減少（小量）SAN。'),

    dict(file='Item_food_twisted_tentacle', id='food_twisted_tentacle', name='扭曲的觸手',
         tags=[F, 'Food', 'SeaFood', 'Uncommon'], price=26, hp=20,
         desc='村中不允許食用與久都留布神形象相近的「那個」，然而在難耐的飢餓中，'
              '這個規定毫無意義...不知為何，無論吃下多少次都會再生，這一定是神明的恩惠。\n\n'
              '【效果】恢復（中等）HP ＆ 獲得 1 層【反擊】。\n'
              '※ 戰鬥端未接：【反擊】層數需要 Romtyui 的 ItemEffectData。\n'
              '※ 美術：深藍色或紫色的章魚觸手，下方有一片扭曲蠕動的結果彷彿還會再生。'),

    dict(file='Item_food_squirming_oyster', id='food_squirming_oyster', name='蠕動的生蠔',
         tags=[F, 'Food', 'SeaFood', 'Uncommon'], price=24, hp=10, sanCost=10,
         desc='總有一種被無數只眼睛注視著的錯覺，但那一定是錯覺吧...'
              '話說回來，這個生蠔原本就只有空殼嗎？\n\n'
              '【效果】恢復（小量）HP ＆ 減少（小量）SAN ＆ 獲得 1 層【護盾】。\n'
              '※ 戰鬥端未接：【護盾】層數需要 Romtyui 的 ItemEffectData。\n'
              '※ 美術：類似生蠔的外殼，但裡面藏著許多眼睛。'),

    dict(file='Item_food_mystery_skewer', id='food_mystery_skewer', name='謎之烤串',
         tags=[F, 'Food', 'Theater', 'Common'], price=12,
         desc='上面燃燒的火焰彷彿永遠不會熄滅，如同焦炭一般的肉塊只有放入口中之時'
              '才能知曉其真面目...是棉花糖啊。\n\n'
              '【效果】獲得 1 層【燃燒】＆ 獲得一張【灰燼】（TOKEN）。\n'
              '※ 戰鬥端未接：這件食物**完全沒有戰鬥外效果**，接上之前吃了不會有事發生。\n'
              '※ 地區：劇院（附件標「未確定」）。\n'
              '※ 美術：一串不明黑色物體，被火焰包裹著看不見真身。'),

    dict(file='Item_food_blood_curd', id='food_blood_curd', name='奢侈的血塊',
         tags=[F, 'Food', 'Castle', 'Uncommon'], price=28, hpCost=20,
         desc='將血液凝固成塊後製作而成，據說受到某些特定的吸血眷屬的喜愛。\n'
              '「區區人類怎麼會理解這道佳餚的美味呢...等等？」\n\n'
              '【效果】減少（中等）HP ＆ 下次攻擊時恢復與傷害等量的 HP。\n'
              '※ 戰鬥端未接：「下次攻擊吸血」需要 Romtyui 的 ItemEffectData。\n'
              '※ 地區：城堡（附件標「未確定」）。\n'
              '※ 美術：類似米血或鴨血的紅色方塊狀食物，擺盤像某種高級法式料理，旁邊有香料點綴。'),

    dict(file='Item_food_infant_fruit', id='food_infant_fruit', name='幼小的果實',
         tags=[F, 'Food', 'Forest', 'Common'], price=12, hp=10,
         desc='長相宛如嬰兒般的水果，貌似會發出啼哭聲，味道卻異常甜美。\n'
              '...在那部名作好像也出現過類似的東西。\n\n'
              '【效果】恢復（小量）HP ＆ 獲得 1 張【羊羔】（TOKEN）。\n'
              '※ 戰鬥端未接：【羊羔】Token 需要 Romtyui 的 ItemEffectData。\n'
              '※ 地區：森林（附件標「未確定」）。\n'
              '※ 美術：長得像嬰兒的黃色果實，上面帶有血絲。'),

    dict(file='Item_food_bone_meat', id='food_bone_meat', name='帶骨肉塊',
         tags=[F, 'Food', 'Ranch', 'Rare'], price=45, hp=20,
         desc='看似某種生物的肉，上面散發著帶有奇怪顏色的磷光。\n'
              '吃起來味道就像普通的烤肉...只是在不知不覺間，身上的這種肉好像越來越多了？\n\n'
              '【效果】恢復（中等）HP。每經過 3 個節點，將身上隨機一個食物轉化為這個食物。\n'
              '※ 未接：「每 3 個節點轉化」需要一個跨節點的持有物鉤子，目前沒有這種機制。\n'
              '　 附件作者自己也註明「不知道能不能做出來這個效果但我先寫」。\n'
              '※ 地區：牧場（附件標「未確定」）。\n'
              '※ 美術：類似卡通形象的肉，表面有彩色的光澤。'),

    dict(file='Item_food_birthday_cake', id='food_birthday_cake', name='生日蛋糕',
         tags=[F, 'Food', 'Rare'], price=55, hp=35, san=15,
         desc='孩童的憧憬，在末世許下的微小願望。\n'
              '雖說味道比超市賣的廉價蛋糕還要糟糕，但此時卻能給你帶來些許慰藉。\n\n'
              '【效果】恢復（大量）HP ＆ 恢復（小量或中等）SAN。\n'
              '※ 附件寫「恢復（大量）HP＆（小量或中等）SAN」，沒寫是回還是扣；\n'
              '　 依敘述（慰藉）取「恢復」、取中間值 15。'),

    dict(file='Item_food_stargazy_pie', id='food_stargazy_pie', name='仰望星空',
         tags=[F, 'Food', 'Rare'], price=60, hp=35, sanCost=20,
         desc='在派中心匯集的是來自遙遠彼方的殘響，沒有人知道烤製這個派的是什麼人，'
              '又是為了什麼目的...至少味道很不錯。\n\n'
              '【效果】恢復（大量）HP ＆ 解除所有異常狀態 ＆ 減少（中等）SAN。\n'
              '※ 戰鬥端未接：「解除所有異常狀態」需要 Romtyui 的 ItemEffectData。\n'
              '※ 美術：派的上層顏色是類似宇宙的那種混沌感。'),
]

# ══════════════════════════════════════════════════════════════
# 收藏品 —— 《收藏品.pdf》17 件
#   取得方式：事件限定 = EventOnly（LootTable 已經用這個標籤排除）
#   神系標籤（General / Abyss / BlazingSong / Chaos）目前沒有表在查，
#   先標上去，之後要照神系分池不必回頭重填
# ══════════════════════════════════════════════════════════════
C = 'Curio'
CURIO = [
    # ── 一般 ──
    dict(file='Item_relic_ruby', id='relic_ruby', name='紅寶石',
         tags=[C, 'Common', 'General'], price=30,
         desc='閃耀的紅色寶石，貌似深受那個古怪商人的喜愛。\n\n'
              '【效果】進入商店時，失去這個收藏品，獲得（大概抓個數量）金幣。\n'
              '※ 未接：需要商店進場時的鉤子。金額附件沒定，暫定 60。'),

    dict(file='Item_relic_moon_toad', id='relic_moon_toad', name='月之蟾蜍',
         tags=[C, 'Rare', 'General'], price=90,
         desc='中國神話中的月亮上貌似居住着某種蟾蜍，然而它和你印象中的蟾蜍好像有一點不一樣...？\n\n'
              '【效果】商店的所有商品價格降低 20%。\n'
              '※ 未接：需要商店定價時的鉤子（ShopStageController 算價的地方）。'),

    # ── 深淵 ──
    dict(file='Item_relic_boat_key', id='relic_boat_key', name='快艇鑰匙',
         tags=[C, 'Common', 'Abyss'], price=30,
         desc='接到了若子的聯絡，村子的情況似乎變得有些奇怪，真是令人擔心。\n'
              '沒有人願意在這種海況下出海，只有我一個人也要過去。\n'
              '認識的大叔願意把船借給我真是太好了。\n'
              '要等我哦，若子。\n'
              '——漁夫的記憶 1\n\n'
              '【效果】戰鬥開始時，獲得 5 層【護盾】。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    dict(file='Item_relic_fishing_rod', id='relic_fishing_rod', name='釣竿',
         tags=[C, 'Common', 'Abyss'], price=30,
         desc='在通往村子的海上出現的一大片濃霧，我沒多想就闖了進去。\n'
              '結果在霧中的漂流已經過了好幾天了，沒想到會在這種地方遇難。\n'
              '帶來的糧食都吃光了，雖然不抱希望，但只能靠釣竿想辦法了。\n'
              '——漁夫的記憶 2\n\n'
              '【效果】每次戰鬥結束時，隨機獲得一個品質為【普通】的食物。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別（而且要有 BattleEnd 觸發點，'
              '現在的 RelicsTriggerType 只有 BattleStart／回合開始／回合結束／出牌）。'),

    dict(file='Item_relic_harpoon', id='relic_harpoon', name='魚叉',
         tags=[C, 'Common', 'Abyss'], price=30,
         desc='村裡到處都是像魚一樣的怪物在互相廝殺，它們看到我後也撲了上來。\n'
              '要不是手上帶著魚叉，不然早就——\n'
              '這個地獄到底是怎麼一回事，其他人都去哪裡了？\n'
              '若子...你一定要沒事啊！\n'
              '——漁夫的記憶 5\n\n'
              '【效果】觸發【反擊】時，造成的傷害增加 10 點。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    dict(file='Item_relic_mermaid_portrait', id='relic_mermaid_portrait', name='人魚的畫像',
         tags=[C, 'Common', 'Abyss'], price=30, relic=HEALING_10PCT_GUID,
         desc='今天的祭典真開心！雖然只分到了一點點，但肉真的很好吃！\n'
              '媽媽說，只要獻上祭品的話，神明大人就一定會幫助我們的！\n'
              '所以我會努力祈禱的，希望回到海里的人魚姐姐，能夠早點遇到神明大人！\n\n'
              '【效果】食物的 HP 恢復量增加 10%。\n'
              '※ 已接上現成的 RelicEffect_HealingReceived_10Percent —— '
              '這個效果原本掛在「貪婪的大口」上，附件把它改給了這一件。'),

    dict(file='Item_relic_broken_rod', id='relic_broken_rod', name='斷裂的釣竿',
         tags=[C, 'Uncommon', 'Abyss'], price=55,
         desc='雖說釣上了，但是...這個，真的是魚嗎？\n'
              '我從來沒見過這種形狀的魚...那個看著我的眼神實在是令人毛骨悚然。\n'
              '但已經餓了好幾天的我也沒有資格挑三揀四了，我強忍噁心咬下那條魚身上的肉。\n'
              '結果，還挺美味的。\n'
              '還能...吃到更多嗎？\n'
              '——漁夫的記憶 3\n\n'
              '【效果】每次戰鬥結束時，隨機獲得一個品質為【罕見】的食物。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別 ＋ BattleEnd 觸發點。'),

    dict(file='Item_relic_broken_engine', id='relic_broken_engine', name='損壞的引擎',
         tags=[C, 'Uncommon', 'Abyss'], price=55,
         desc='在那之後不知過了多久，我終究還是在船上失去了意識。\n'
              '本以為會死在這裡，但等恢復意識的時候，船隻就擱淺在島上的沙灘了。\n'
              '是海浪把我帶到這裡來的嗎？本想說還真是幸運...然而，實際上卻糟透了。\n'
              '——漁夫的記憶 4\n\n'
              '【效果】戰鬥開始時，獲得 10 層【護盾】。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    dict(file='Item_relic_bloody_harpoon', id='relic_bloody_harpoon', name='染血的魚叉',
         tags=[C, 'Uncommon', 'Abyss'], price=55,
         desc='開什麼玩笑...那些怪物是——\n'
              '為什麼會變成這樣...？他們究竟犯下了什麼樣的罪行？\n'
              '那個愚蠢的海神祭典，到底造就了什麼？\n'
              '而且無論哪裡都找不到若子...她還平安無事嗎？\n'
              '難道，她也變成了——\n'
              '——漁夫的記憶 6\n\n'
              '【效果】觸發【反擊】時，造成的傷害增加 20%。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    dict(file='Item_relic_group_photo', id='relic_group_photo', name='合照',
         tags=[C, 'Uncommon', 'Abyss'], price=55,
         desc='...不，不可能。\n'
              '不久前還在跟我說電話的她絕不會...！\n'
              '而且，在濃霧的盡頭，我看到了。\n'
              '那個巨大的影子，那就是一切的禍根嗎？\n'
              '我必須得知道真相。\n'
              '——漁夫的記憶 7\n\n'
              '【效果】觸發【反擊】時，使其額外發動兩次。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。\n'
              '※ 美術：放有兄妹年幼合照的相框。'),

    dict(file='Item_relic_greedy_maw', id='relic_greedy_maw', name='貪婪的大口',
         tags=[C, 'Uncommon', 'Abyss', 'EventOnly'], price=55,
         icon='3dfd7166cef92b146af3e7e124662842',
         desc='飢餓是生物的本能。\n'
              '只要是爲了活下來，無論做出什麽都會被原諒。\n'
              '欺騙、屠殺、分解、享用。\n\n'
              '沒錯，那個祭品的少女...也一定會原諒我們的。\n\n'
              '【效果】恢復 HP 時，獲得 1 層【反擊】。\n'
              '※ ⚠️ 效果換過：專案裡原本寫「HP 的恢復量提升 10%」並且已經接上'
              'RelicEffect_HealingReceived_10Percent。附件把那個效果給了「人魚的畫像」，'
              '這一件改成「恢復 HP 時獲得 1 層【反擊】」，所以效果已卸下、等 Romtyui 補新的子類別。\n'
              '※ 美術：類似貪吃鬼身上掉下來的其中一個魚頭。'),

    dict(file='Item_relic_lure_bulb', id='relic_lure_bulb', name='誘惑的餌球',
         tags=[C, 'Rare', 'Abyss'], price=90,
         desc='我不知道 我不想知道\n'
              '我知曉了真相 知曉那最令人作嘔 難以下嚥的 現實\n'
              '雙手在顫抖 胃酸在翻騰 我無法直視 那些血肉模糊的扭曲怪物\n\n'
              '啊啊 我最厭惡的神明 你要是真的存在的話 就讓我再也無法思考這一切吧\n'
              '——漁夫的記憶 8\n\n'
              '【效果】觸發【反擊】時，獲得 3 層【護盾】。\n'
              '※ 附件寫「3（或更多？）層」，暫定 3。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。\n'
              '※ 美術：燈籠魚頭上那個東西。'),

    dict(file='Item_relic_mermaid_flesh', id='relic_mermaid_flesh', name='人魚肉',
         tags=[C, 'Rare', 'Abyss', 'EventOnly'], price=90,
         desc='被剝下的血肉滋養了眷屬，擴散的欲望猶如螺旋般無窮無盡。\n\n'
              '其名為八百比丘尼，乃螺湮的巫女、不滅的血肉，亦是貪食的根源。\n\n'
              '【效果】每次戰鬥開始時減少（少量）SAN，恢復自身 25% 的 HP。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。\n'
              '※ 專案原本寫「消耗 10 點 SAN」，附件寫「減少（少量）SAN」—— 同一件事，取 10。'),

    dict(file='Item_relic_rayen_charm', id='relic_rayen_charm', name='螺湮御守',
         tags=[C, 'Rare', 'Abyss', 'EventOnly'], price=90,
         desc='縫製了琉璃江村所信奉的神明紋樣，據説能為出海捕魚的漁民帶來好運的御守。\n\n'
              '【效果】觸發【反擊】時，改爲對全體敵人發動。\n'
              '※ ⚠️ 效果換過：專案裡原本寫「使其額外發動一次」，附件寫「改爲對全體敵人發動」。\n'
              '　 依指示以附件為準。兩者都還沒有實作。\n'
              '※ 美術：有類似章魚觸手或吸盤圖案的御守。'),

    # ── 熾歌 ──
    dict(file='Item_relic_burnt_score', id='relic_burnt_score', name='燒毀的樂譜',
         tags=[C, 'Common', 'BlazingSong'], price=30,
         desc='（附件未填敘述）\n\n'
              '【效果】回合開始時獲得一張【灰燼（Token）】。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    # ── 混沌 ──
    dict(file='Item_relic_playing_cards', id='relic_playing_cards', name='撲克牌',
         tags=[C, 'Uncommon', 'Chaos'], price=55,
         desc='（附件未填敘述）\n\n'
              '【效果】每場戰鬥開始時，額外抽取 1 張手牌。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。'),

    dict(file='Item_relic_magic_hat', id='relic_magic_hat', name='魔術帽',
         tags=[C, 'Rare', 'Chaos'], price=90,
         desc='「不知道會發生什麼」...這件事本身不就是樂趣嗎？\n'
              '混沌就是最佳的娛樂...事已至此，不如好好享受吧？\n\n'
              '【效果】回合開始時，從「抽取 1 張手牌」、「抽取 2 張手牌」、'
              '「隨機捨棄 1 張手牌」中隨機發動一個效果。\n'
              '※ 戰鬥端未接：需要新的 RelicsEffectData 子類別。\n'
              '　 專案裡原本叫「混沌撲克」，就是這一件 —— 已改名並照附件拆成兩件（另一件是撲克牌）。'),
]

# ══════════════════════════════════════════════════════════════
# 封存 —— 移到 _Archive/Items 並從 ItemDatabase 移除
# ══════════════════════════════════════════════════════════════
ARCHIVE_FILES = [
    # 附件沒有的舊食物
    'Item_fish_odd', 'Item_fish_plain', 'Item_jerky', 'Item_seaweed',
    'Item_hard_bread', 'Item_fish_face',
    # 使用者指示：食物與遺物以外的道具先封存
    'Item_coarse_salt', 'Item_lamp_oil', 'Item_old_rope',
    'Item_Lockpick', 'Item_KeyWarehouse',
    # 附件把它們換掉／拆掉的舊遺物
    'Item_relic_bloody_rod',    # 染血釣竿 → 附件無此件
    'Item_relic_new_rod',       # 嶄新釣竿 → 附件的「釣竿」是另一個效果
    'Item_relic_old_rod',       # 老舊釣竿 → 附件無此件
    'Item_relic_chaos_poker',   # 混沌撲克 → 拆成撲克牌＋魔術帽
]

KEEP_FILES = ['Item_card_ph_1', 'Item_card_ph_2', 'Item_card_ph_3', 'Item_card_ph_4']


def main():
    out = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline='\n')
    os.makedirs(ARCHIVE, exist_ok=True)

    written = []
    for spec in FOOD + CURIO:
        g = write_item(spec)
        written.append((spec['file'], g))
        out.write('  寫入 %-34s %s\n' % (spec['file'], g))

    out.write('\n封存：\n')
    for f in ARCHIVE_FILES:
        for ext in ('.asset', '.asset.meta'):
            src = os.path.join(ITEMS, f + ext)
            dst = os.path.join(ARCHIVE, f + ext)
            if os.path.exists(src):
                if os.path.exists(dst):
                    os.remove(dst)
                os.rename(src, dst)
        out.write('  → _Archive/Items/%s\n' % f)

    # ── ItemDatabase：武器牌 ＋ 新的食物與收藏品 ──
    keep_guids = []
    for f in KEEP_FILES:
        m = os.path.join(ITEMS, f + '.asset.meta')
        keep_guids.append((f, re.search(r'guid: (\w+)', open(m).read()).group(1)))

    entries = keep_guids + written
    db = ROOT + '/Assets/TYN/Core/ItemDatabase.asset'
    txt = open(db, encoding='utf-8').read()
    head = txt.split('  items:', 1)[0]
    lines = ''.join('  - {fileID: 11400000, guid: %s, type: 2}\n' % g for _, g in entries)
    open(db, 'w', encoding='utf-8', newline='\n').write(head + '  items:\n' + lines)
    out.write('\nItemDatabase：%d 筆（武器牌 %d ＋ 食物 %d ＋ 收藏品 %d）\n'
              % (len(entries), len(keep_guids), len(FOOD), len(CURIO)))
    out.flush()


if __name__ == '__main__':
    main()

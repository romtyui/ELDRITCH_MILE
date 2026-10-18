public enum StatusType
{
    //共用區
    Strength,       // 力量：造成傷害增加
    TemporaryStrength,// 臨時力量：造成傷害增加
    Weak,           // 虛弱：造成傷害降低
    Vulnerable,     // 易傷：受到傷害增加
    Frail,          // 脆弱：獲得格擋降低
    Poison,         // 中毒：回合開始受到傷害


    Harden,         // 硬化：回合開始獲得護盾
    Regeneration,   // 自癒：回合開始恢復生命

    Counter,           // 反擊：受到敵方實際傷害時，對傷害來源造成等同反擊層數的傷害
    InitialBlock,   // 初始護盾：出場時獲得等同層數的基礎護盾

    RetainBlock,    // 護盾保留：自身回合開始不清除護盾

    DiscardHandAtNextTurnStart, // 下次玩家回合抽牌後，隨機捨棄等同層數的手牌，結算後清除


    //怪物區

    HermitHunt,     // 寄居狩獵：回合結束依護盾量選擇下一個意圖

    EndlessHunger, // 好餓好餓好餓：玩家回合結束時，自損並獲得力量

    PhaseSwitch,  // 猥裸:型態轉變

    FeedingDesire, // 飢食的渴求
    Overload, // 過負荷

}
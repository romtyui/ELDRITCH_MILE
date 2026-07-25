using System;
using UnityEngine;

[Serializable]
public class TutorialDialogueLine
{
    [Header("Speaker")]
    public DialogueSpeakerData speaker;

    [Tooltip("開啟後，覆蓋角色資料中的預設立繪位置")]
    public bool overridePortraitSide;

    public DialoguePortraitSide portraitSide =
        DialoguePortraitSide.Left;

    [Header("Portrait Style")]
    [Tooltip("這句對話開始時使用的立繪樣式")]
    public string portraitStyleId = "Default";

    [Tooltip("允許在文字中使用 {portrait:StyleId}")]
    public bool allowInlinePortraitChange = true;

    [Header("Text")]
    [TextArea(3, 12)]
    public string text;

    [Header("Typewriter")]
    public bool useTypewriter = true;

    [Min(0.001f)]
    public float typewriterInterval = 0.03f;

    [Tooltip("逐字播放途中點擊，是否直接顯示完整句子")]
    public bool allowClickToCompleteText = true;

    public DialoguePortraitSide GetPortraitSide()
    {
        if (overridePortraitSide)
            return portraitSide;

        if (speaker != null)
            return speaker.defaultPortraitSide;

        return DialoguePortraitSide.Left;
    }

    public string GetInitialPortraitStyleId()
    {
        if (!string.IsNullOrWhiteSpace(portraitStyleId))
            return portraitStyleId.Trim();

        if (speaker != null &&
            !string.IsNullOrWhiteSpace(
                speaker.defaultPortraitStyleId))
        {
            return speaker.defaultPortraitStyleId.Trim();
        }

        return "Default";
    }
}
using UnityEngine;

public enum DialoguePortraitSide
{
    Left,
    Right
}

[CreateAssetMenu(
    fileName = "DialogueSpeaker",
    menuName = "Tutorial/Dialogue Speaker"
)]
public class DialogueSpeakerData : ScriptableObject
{
    [Header("Identity")]
    public string speakerId;

    public string displayName;

    [Header("Portrait")]
    public Sprite portrait;

    public DialoguePortraitSide defaultPortraitSide =
        DialoguePortraitSide.Left;
}
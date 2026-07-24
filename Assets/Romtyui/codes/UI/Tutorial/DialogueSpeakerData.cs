using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialoguePortraitSide
{
    Left,
    Right
}

[Serializable]
public class DialoguePortraitStyle
{
    [Tooltip("樣式 ID，例如 Default、Smile、Angry")]
    public string styleId = "Default";

    [Tooltip("這個樣式對應的立繪")]
    public Sprite portrait;
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

    [Header("Portrait Position")]
    public DialoguePortraitSide defaultPortraitSide =
        DialoguePortraitSide.Left;

    [Header("Portrait Styles")]
    [Tooltip("Dialogue Line 沒有指定樣式時使用")]
    public string defaultPortraitStyleId = "Default";

    [Tooltip("角色可使用的所有立繪樣式")]
    public List<DialoguePortraitStyle> portraitStyles = new();

    public Sprite GetPortrait(string styleId)
    {
        string requestedStyle =
            string.IsNullOrWhiteSpace(styleId)
                ? defaultPortraitStyleId
                : styleId.Trim();

        DialoguePortraitStyle matchedStyle =
            FindPortraitStyle(requestedStyle);

        if (matchedStyle != null &&
            matchedStyle.portrait != null)
        {
            return matchedStyle.portrait;
        }

        DialoguePortraitStyle defaultStyle =
            FindPortraitStyle(defaultPortraitStyleId);

        if (defaultStyle != null &&
            defaultStyle.portrait != null)
        {
            return defaultStyle.portrait;
        }

        for (int i = 0; i < portraitStyles.Count; i++)
        {
            DialoguePortraitStyle style =
                portraitStyles[i];

            if (style != null &&
                style.portrait != null)
            {
                return style.portrait;
            }
        }

        return null;
    }

    public bool HasPortraitStyle(string styleId)
    {
        return FindPortraitStyle(styleId) != null;
    }

    private DialoguePortraitStyle FindPortraitStyle(
        string styleId
    )
    {
        if (portraitStyles == null ||
            string.IsNullOrWhiteSpace(styleId))
        {
            return null;
        }

        string targetId = styleId.Trim();

        for (int i = 0; i < portraitStyles.Count; i++)
        {
            DialoguePortraitStyle style =
                portraitStyles[i];

            if (style == null)
                continue;

            if (string.Equals(
                    style.styleId?.Trim(),
                    targetId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return style;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (portraitStyles == null)
            portraitStyles = new List<DialoguePortraitStyle>();

        for (int i = 0; i < portraitStyles.Count; i++)
        {
            DialoguePortraitStyle style =
                portraitStyles[i];

            if (style == null)
                continue;

            if (string.IsNullOrWhiteSpace(style.styleId))
            {
                style.styleId =
                    $"Style_{i + 1}";
            }
        }
    }
#endif
}
using System;
using UnityEngine;

[Serializable]
public class TooltipEntry
{
    public string title;

    [TextArea(2, 5)]
    public string body;

    public TooltipEntry() { }

    public TooltipEntry(string title, string body)
    {
        this.title = title;
        this.body = body;
    }
}
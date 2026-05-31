using System.Collections.Generic;
using UnityEngine;

public enum NodeType
{
    Start, Combat, Elite, Event, Rest, Shop, Boss
}

[System.Serializable]
public class MapNode
{
    public string id;
    public int layer;
    public NodeType type;
    public float x; // 0~100 百分比
    public float y; // 0~100 百分比
    public List<string> parents  = new List<string>();
    public List<string> children = new List<string>();

    public MapNode(string _id, int _layer, NodeType _type, float _x, float _y)
    {
        id = _id; layer = _layer; type = _type; x = _x; y = _y;
    }
}

public static class NodeTypeDisplay
{
    public static string GetName(NodeType type) => type switch
    {
        NodeType.Start  => "[ START ]",
        NodeType.Combat => "[ COMBAT ]",
        NodeType.Elite  => "[ ELITE ]",
        NodeType.Event  => "[ EVENT ]",
        NodeType.Rest   => "[ REST ]",
        NodeType.Shop   => "[ SHOP ]",
        NodeType.Boss   => "[ BOSS ]",
        _               => "[ ??? ]"
    };

    public static string GetDescription(NodeType type) => type switch
    {
        NodeType.Start  => "The first step into the unknown.\nChoose your starting route and decide your fate.",
        NodeType.Combat => "Danger  > Hull Damage\nCultists intercept the convoy. Take hull damage to break through.",
        NodeType.Elite  => "Danger  > Hull Damage\nReward  > Large Scrap Gain\nA powerful foe stands guard, but their wreckage is full of scrap.",
        NodeType.Event  => "Unknown  > Sanity Risk\nA strange presence preys on your mind,\nor may bestow an unspeakable gift.",
        NodeType.Rest   => "Recovery  > Sanity + Fuel\nAn abandoned fuel station — a moment's rest for the weary.",
        NodeType.Shop   => "Trade  > Scrap Cost\nAn underground black market — rare supplies for scrap.\nChoose wisely.",
        NodeType.Boss   => "Final Stand  > High Risk\nThe guardian at the end awaits.\nLife or death — are you ready?",
        _               => "Unidentified signal..."
    };
}

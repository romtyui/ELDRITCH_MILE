using System.Collections.Generic;
using UnityEngine;

public enum ExplorationNodeType
{
    Event,
    Combat,
    Boss
}

[CreateAssetMenu(fileName = "NewRoomNode", menuName = "Roguelike/MapNode")]
public class MapNodeExplore : ScriptableObject
{
    [Header("房間設定")]
    public string roomName = "未知房間";
    public ExplorationNodeType nodeType = ExplorationNodeType.Combat;
    
    [Tooltip("點擊此節點時要疊加載入的場景名稱 (例如 ExploreScene 或 BattleScene)")]
    public string targetSceneName = "ExploreScene";

    public GameObject roomPrefab;

    [Header("敘事與文本")]
    [TextArea(3, 5)]
    public string entryText = "你進入了一個新的區域...";
    
    [Tooltip("探索完此房間所有物件後彈出的文本。若留空則不提示。")]
    [TextArea(2, 4)]
    public string roomClearText = ""; 
}
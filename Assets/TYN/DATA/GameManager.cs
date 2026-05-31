using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public MapGenerator  mapGenerator;
    public MapUIManager  uiManager;

    [Header("Player Stats")]
    public int hull   = 100;
    public int sanity = 80;
    public int fuel   = 50;
    public int scrap  = 20;

    [Header("Game State")]
    public List<MapNode> allNodes;
    public MapNode currentNode;                        // null = 尚未選擇起點
    public List<string> visitedNodeIds   = new List<string>();
    public List<string> selectableNodeIds = new List<string>();

    private Dictionary<string, MapNode> nodeById = new Dictionary<string, MapNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => StartNewRun();

    public void StartNewRun()
    {
        allNodes = mapGenerator.GenerateMap();

        nodeById.Clear();
        foreach (var n in allNodes) nodeById[n.id] = n;

        // 開局無當前節點，所有 layer-0 節點皆可選
        currentNode = null;
        visitedNodeIds.Clear();
        selectableNodeIds = allNodes.FindAll(n => n.layer == 0).ConvertAll(n => n.id);

        hull = 100; sanity = 80; fuel = 50; scrap = 20;

        uiManager.DrawMap(allNodes, nodeById);
        uiManager.UpdateMapVisuals();
        uiManager.UpdateStatsUI();
        uiManager.AddLog("Engine online. Choose your starting route...");
    }

    public void OnNodeClicked(MapNode targetNode)
    {
        if (!selectableNodeIds.Contains(targetNode.id)) return;

        bool isStartChoice = currentNode == null;

        if (!isStartChoice)
            ProcessNodeEvent(targetNode);

        currentNode = targetNode;
        visitedNodeIds.Add(targetNode.id);
        selectableNodeIds = new List<string>(targetNode.children);

        uiManager.UpdateMapVisuals();
        uiManager.UpdateStatsUI();
        uiManager.FocusOnCurrentNode();

        if (isStartChoice)
        {
            uiManager.AddLog($"Route locked in. Heading into the dark——");
        }
        else
        {
            if (hull <= 0)    TriggerGameOver("Hull completely destroyed. The journey ends here.");
            else if (fuel <= 0)   TriggerGameOver("Out of fuel. The vehicle breaks down in the wasteland.");
            else if (sanity <= 0) TriggerGameOver("Sanity depleted. You are lost in hallucinations forever.");
        }
    }

    public MapNode GetNodeById(string id) => nodeById.TryGetValue(id, out var n) ? n : null;

    private void ProcessNodeEvent(MapNode node)
    {
        fuel -= 5;

        switch (node.type)
        {
            case NodeType.Combat:
                hull -= Random.Range(5, 15);
                uiManager.AddLog("Cultists intercept the vehicle. Hull damaged.");
                break;
            case NodeType.Elite:
                int hullDmg  = Random.Range(10, 25);
                int scrapGain = Random.Range(5, 15);
                hull  -= hullDmg;
                scrap += scrapGain;
                uiManager.AddLog($"Elite encounter. Lost {hullDmg} hull, gained {scrapGain} scrap.");
                break;
            case NodeType.Event:
                int sanityLoss = Random.Range(5, 15);
                sanity -= sanityLoss;
                uiManager.AddLog($"Strange visions emerge. Sanity reduced by {sanityLoss}.");
                break;
            case NodeType.Rest:
                sanity = Mathf.Min(100, sanity + 20);
                fuel  += 15;
                uiManager.AddLog("Found supplies at an abandoned fuel station.");
                break;
            case NodeType.Shop:
                uiManager.AddLog("Black market discovered. Trade scrap for supplies?");
                break;
            case NodeType.Boss:
                hull -= Random.Range(20, 40);
                uiManager.AddLog("Final showdown! Can you survive?");
                break;
            default:
                uiManager.AddLog("Destination reached.");
                break;
        }
    }

    private void TriggerGameOver(string reason)
    {
        selectableNodeIds.Clear();
        uiManager.UpdateMapVisuals();
        uiManager.AddLog($"[GAME OVER] {reason}");
        // TODO: 顯示 Game Over 畫面 / 重新開始按鈕
    }
}

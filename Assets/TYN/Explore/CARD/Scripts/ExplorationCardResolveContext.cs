using UnityEngine;

public class ExplorationCardResolveContext
{
    // 將這裡改為新名稱 CardExplorationManager
    public CardExplorationManager manager; 
    public ExplorationDeck deck;
    public CardInstance card;
    public ExplorationInteractableTarget target;
    public Camera playerCamera;

    public ExplorationCardResolveContext(
        CardExplorationManager manager,
        ExplorationDeck deck,
        CardInstance card,
        ExplorationInteractableTarget target,
        Camera playerCamera
    )
    {
        this.manager = manager;
        this.deck = deck;
        this.card = card;
        this.target = target;
        this.playerCamera = playerCamera;
    }
}
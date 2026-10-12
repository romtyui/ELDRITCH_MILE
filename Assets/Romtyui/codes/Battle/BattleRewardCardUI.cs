using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleRewardCardUI : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("子物件 card_template 上的 CardViewUI。")]
    public CardViewUI cardView;

    [Tooltip("這張牌自己的選擇鎖定框。")]
    public GameObject selectionFrame;

    [Tooltip("獎勵牌需要停用的手牌互動，例如 CardDragUI、CardHoverUI。")]
    public MonoBehaviour[] disableForReward;

    public CardData Data { get; private set; }

    private BattleRewardUI owner;

    public void Bind(BattleRewardUI rewardUI, CardData data)
    {
        owner = rewardUI;
        Data = data;

        if (disableForReward != null)
        {
            for (int i = 0; i < disableForReward.Length; i++)
            {
                MonoBehaviour component = disableForReward[i];
                if (component == null || component == this || component == cardView) continue;
                component.enabled = false;
            }
        }

        if (selectionFrame != null)
        {
            Graphic[] graphics = selectionFrame.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;
        }

        SetSelected(false);
        if (cardView != null && data != null) cardView.Bind(new CardInstance(data));
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null) selectionFrame.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || owner == null) return;
        owner.ClickCard(this);
    }
}
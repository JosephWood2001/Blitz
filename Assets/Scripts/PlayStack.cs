using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlayStack : CardStack
{
    

    [ClientRpc]
    public void RpcInstantiate(Vector2 pos)
    {
        transform.SetParent(GameObject.FindGameObjectWithTag("PlaySpace").transform);
        transform.position = pos;
    }

    public override bool AttemptPlace(Card card)
    {
        if (card.number == 1 && cards.Count == 0)
        {
            card.CanMove = false;
            return true;
        }
        else if (card.color.Equals(cards[cards.Count - 1].GetComponent<Card>().color) && card.number - 1 == cards[cards.Count - 1].GetComponent<Card>().number)
        {
            card.CanMove = false;
            return true;
        }
        else
        {
            return false;
        }
        
        
    }

    protected override void AddCard(Card card)
    {
        base.AddCard(card);
        card.CanMove = false;
    }

}

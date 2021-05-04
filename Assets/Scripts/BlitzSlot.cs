using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class BlitzSlot : CardStack
{

    public Collider2D col;

    public void Instantiate(Card card)
    {
        MoveCardHere(card);

        this.cards[cards.Count - 1].GetComponent<Card>().CanMove = true;

    }

    protected override void AddCard(Card card)
    {
        base.AddCard(card);
        card.CanMove = true;
    }

    public override bool AttemptPlace(Card card)
    {
        if (cards.Count == 0)
        {
            return true;
        }else if (cards[cards.Count - 1].GetComponent<Card>().isMale != card.isMale && cards[cards.Count - 1].GetComponent<Card>().number == card.number + 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    public List<Card> StackGrab(Card card)
    {
        List<Card> stackGrab = new List<Card>();

        for (int i = cards.IndexOf(card.gameObject); i < cards.Count; i++)
        {
            stackGrab.Add(cards[i].GetComponent<Card>());
        }

        if(stackGrab.Count > 1)
        {
            return stackGrab;
        }
        else
        {
            return null;
        }

        
    }
}

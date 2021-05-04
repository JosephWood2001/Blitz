using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlitzDeck : CardStack
{
    public GameObject owner;

    public int CardsLeft
    {
        get
        {
            return cards.Count;
        }
    }

    public void Instantiate(List<Card> cards, GameObject owner)
    {
        this.owner = owner;

        foreach (Card card in cards)
        {
            MoveCardHere(card);
        }

        if (cards.Count != 0)
        {
            this.cards[cards.Count - 1].GetComponent<Card>().CanMove = true;

        }

    }

    protected override void RemoveCard(Card card)
    {
        base.RemoveCard(card);
        if (cards.Count != 0)
        {
            cards[cards.Count - 1].GetComponent<Card>().CanMove = true;
        }
        else
        {
            if (hasAuthority && GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().roundInPlay)
            {
                StartCoroutine("WaitForCardPlacedToBlitz", card);
                
            }
            
        }

    }

    IEnumerator WaitForCardPlacedToBlitz(Card card)
    {
        while (card.parentStack == null)
        {
            Debug.Log("Waiting...");
            yield return null;
        }
        

        Debug.Log("BLITZ!!!");
        owner.GetComponent<ClientPlayer>().CmdBlitz();
    }

}

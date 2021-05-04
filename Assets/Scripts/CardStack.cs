using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class CardStack : NetworkBehaviour
{

    public int showHeight = 15;
    public float heightOffset = 3;

    public List<GameObject> Cards
    {
        get
        {
            return cards;
        }
    }

    protected List<GameObject> cards = new List<GameObject>();

    protected virtual void AddCard(Card card)
    {
        card.parentStack = this;

        card.gameObject.transform.SetParent(gameObject.transform, true);

        cards.Add(card.gameObject);


        card.gameObject.transform.SetSiblingIndex(cards.Count - 1);

        card.targetRot = card.gameObject.transform.parent.rotation;

        SetCardHeights();
    }

    public void MoveCardHere(Card card)
    {
        card.enabled = true;

        if (card.parentStack != null)
        {
            card.parentStack.RemoveCard(card);
        }
        
        AddCard(card);

    }


    protected virtual void RemoveCard(Card card)
    {
        int index = cards.IndexOf(card.gameObject);
        cards.Remove(card.gameObject);
        card.parentStack = null;
        SetCardHeights();
    }

    

    public void MoveCardBackToPile(Card card)
    {
        int index = cards.IndexOf(card.gameObject);
        cards[index].GetComponent<Card>().targetPos = transform.position;
        cards[index].GetComponent<Card>().targetRot = transform.rotation;
        SetCardHeights();
    }

    void SetCardHeights() {
        for(int i = 0; i < Mathf.Min( showHeight, cards.Count ); i++)
        {
            cards[i].GetComponent<Card>().targetPos = transform.position + transform.rotation * (Vector3.up * heightOffset * (Mathf.Min(showHeight, cards.Count) - (i + 1)));
        }

        for (int i = Mathf.Min(showHeight, cards.Count); i < cards.Count; i++)
        {
            cards[i].GetComponent<Card>().targetPos = transform.position;
        }

    }

    public virtual bool AttemptPlace(Card card)
    {
        return false;
    }

    public int CardHeight(Card card)
    {
        return cards.IndexOf(card.gameObject);
    }

    [Command]
    public void CmdRevert(GameObject[] cards)
    {
        RpcRevert(cards);
    }

    [ClientRpc]
    void RpcRevert(GameObject[] cards)
    {
        int i = 0;
        foreach (GameObject card in cards)
        {
            if (this.cards[i] != card)
            {
                card.GetComponent<Card>().enabled = true;

                if (card.GetComponent<Card>().parentStack != null)
                {
                    card.GetComponent<Card>().parentStack.RemoveCard(card.GetComponent<Card>());
                }

                card.GetComponent<Card>().parentStack = this;

                card.transform.SetParent(gameObject.transform, true);
                this.cards.Insert(i, card);

                card.transform.SetSiblingIndex(i);

                card.GetComponent<Card>().targetRot = card.transform.parent.rotation;

            }
            i++;
        }
        SetCardHeights();
    }
}

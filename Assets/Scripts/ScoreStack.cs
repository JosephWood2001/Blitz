using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class ScoreStack : CardStack
{
    private void Start()
    {
        transform.SetParent(GameObject.FindGameObjectWithTag("ScoreStack").transform);
        transform.localPosition = Vector3.zero;
    }

    [Command]
    public void CmdCollect()
    {
        List<GameObject> cardObjs = new List<GameObject>();
        foreach (PlayStack playStack in GameObject.FindGameObjectWithTag("PlaySpace").GetComponentsInChildren<PlayStack>())
        {
            foreach (GameObject cardObj in playStack.Cards)
            {
                cardObjs.Add(cardObj);
            }
        }

        GameObject[] cardObjsArr = new GameObject[cardObjs.Count];
        int i = 0;
        foreach (GameObject cardObj in cardObjs)
        {
            cardObjsArr[i] = cardObj;
            i++;
        }

        RpcMoveCardsHere(cardObjsArr);
    }

    [ClientRpc]
    void RpcMoveCardsHere(GameObject[] cards)
    {
        foreach (GameObject cardObj in cards)
        {
            MoveCardHere(cardObj.GetComponent<Card>());
        }
        
    }

    [Command]
    public void CmdDistributePoints()
    {
        StartCoroutine("Distribute");

    }
    IEnumerator Distribute() // only runs on server, alling it to call RPC's
    {
        yield return new WaitForSeconds(3f);

        Card[] cards = new Card[base.cards.Count];
        int i = base.cards.Count - 1;
        foreach(GameObject card in base.cards)
        {
            cards[i] = card.GetComponent<Card>();
            i--;
        }

        foreach(Card card in cards)
        {
            card.RpcMoveCard(card.owner.GetComponent<ClientPlayer>().deck);
            card.owner.GetComponent<ClientPlayer>().Score += 1;
            yield return new WaitForSeconds(.25f);
        }

        yield return new WaitForSeconds(2f);

        GameObject.FindGameObjectWithTag("NewRound").transform.GetChild(0).gameObject.SetActive(true);

    }

    
}

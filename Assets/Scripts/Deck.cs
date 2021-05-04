using Mirror;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Deck : CardStack
{
    public GameObject owner;

    public string deckName = "NA";
    public Collider2D col;

    public GameObject blitzDeck;

    public int numOfBlitzSlots = 0;

    public GameObject slotPrefab;
    public GameObject[] slots;

    public List<GameObject> inHand = new List<GameObject>();

    public bool shuffled = false;

    public GameObject cardPrefab;

    //only called by server
    public void Instantiate(GameObject owner, string deckName)
    {
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            isHandheld = true;
        }
        else
        {
            isHandheld = false;
        }

        owner.GetComponent<ClientPlayer>().deck = gameObject;
        this.owner = owner;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 1; j < 11; j++)
            {
                GameObject cardObj = Instantiate(cardPrefab, transform);

                string color = "NA";
                bool isMale = false;
                if (i == 0)
                {
                    color = "g";
                    isMale = false;
                }
                else if (i == 1)
                {
                    color = "r";
                    isMale = true;
                }
                else if (i == 2)
                {
                    color = "b";
                    isMale = false;
                }
                else if (i == 3)
                {
                    color = "y";
                    isMale = true;
                }

                NetworkServer.Spawn(cardObj, owner);
                cardObj.GetComponent<Card>().RpcInstantiate(color, isMale, j, owner, gameObject);
                AddNewCard(cardObj.GetComponent<Card>());
            }
        }

        Shuffle();

        GameObject[] inHandArr = new GameObject[inHand.Count];
        for(int i = 0; i < inHand.Count; i++)
        {
            inHandArr[i] = inHand[i];
        }

        blitzDeck = Instantiate(blitzDeck);
        NetworkServer.Spawn(blitzDeck, gameObject);

        numOfBlitzSlots = Mathf.Clamp(7 - GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().playerCount, 3, 5);
        slots = new GameObject[numOfBlitzSlots];
        for(int i = 0; i < numOfBlitzSlots; i++)
        {
            slots[i] = Instantiate(slotPrefab);
            NetworkServer.Spawn(slots[i], gameObject);
        }

        RpcInstantiate(owner, deckName, isHandheld, inHandArr, blitzDeck, slots);

    }

    [ClientRpc]
    public void RpcInstantiate(GameObject owner, string deckName, bool isHandheld, GameObject[] inHand, GameObject blitzDeck, GameObject[] blitzSlots)
    {
        this.owner = owner;

        GameManager gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        Transform parent = null;
        if (gm.clientPlayer.gameObject == owner)
        {
            parent = GameObject.FindGameObjectWithTag("PlayerSpot").transform;
        }
        else
        {
            GameObject[] opponentSpots = GameObject.FindGameObjectsWithTag("Opponent");
            for(int i = 0; i < gm.opponents.Count; i++)
            {
                Debug.Log(owner.name);
                if (gm.opponents[i].gameObject == owner)
                {
                    parent = opponentSpots[i].transform;
                    break;
                }
            }
        }


        this.deckName = deckName;
        name = deckName;
        this.isHandheld = isHandheld;
        owner.GetComponent<ClientPlayer>().deck = gameObject;
        this.blitzDeck = blitzDeck;
        numOfBlitzSlots = blitzSlots.Length;
        slots = blitzSlots;

        transform.SetParent(parent.GetChild(0)); //decks pos
        transform.position = parent.GetChild(0).position;
        transform.rotation = parent.GetChild(0).rotation;

        owner.GetComponent<ClientPlayer>().blitzDeck = blitzDeck;
        this.blitzDeck.transform.SetParent(parent.GetChild(1)); //blitzDeck pos
        this.blitzDeck.transform.position = parent.GetChild(1).position;
        this.blitzDeck.transform.rotation = parent.GetChild(1).rotation;

        for (int i = 0; i < slots.Length; i++)//slots pos
        {
            slots[i].transform.SetParent(parent.GetChild(i + 2));
            slots[i].transform.position = parent.GetChild(i + 2).position;
            slots[i].transform.rotation = parent.GetChild(i + 2).rotation;
        }

        this.inHand = new List<GameObject>();
        this.cards = new List<GameObject>();

        for (int i = 0; i < inHand.Length; i++)
        {
            AddNewCard(inHand[i].GetComponent<Card>());
        }

        List<Card> addToBlitz = new List<Card>(); //---------------- populates blitz

        Cycle(10);

        foreach (GameObject cardObj in cards)
        {
            addToBlitz.Add(cardObj.GetComponent<Card>());
        }

        blitzDeck.GetComponent<BlitzDeck>().Instantiate(addToBlitz, owner); //---------------- populates blitz

        for (int i = 0; i < numOfBlitzSlots; i++)
        {
            Cycle(1);
            slots[i].GetComponent<BlitzSlot>().Instantiate(cards[0].GetComponent<Card>());
        }

        Cycle(3);


    }

    //only to be called by server
    public void NewRound()
    {
        FillHand();
        Shuffle();
        GameObject[] hand = new GameObject[inHand.Count];
        int i = 0;
        foreach (GameObject cardObj in inHand)
        {
            hand[i] = cardObj;
            i++;
        }
        RpcUpdateHand(hand);
        RpcNewRound();
    }
    
    [ClientRpc]
    void RpcNewRound()
    {

        List<Card> addToBlitz = new List<Card>(); //---------------- populates blitz

        Cycle(10);

        foreach (GameObject cardObj in cards)
        {
            addToBlitz.Add(cardObj.GetComponent<Card>());
        }

        blitzDeck.GetComponent<BlitzDeck>().Instantiate(addToBlitz, owner); //---------------- populates blitz

        for (int i = 0; i < numOfBlitzSlots; i++)
        {
            Cycle(1);
            slots[i].GetComponent<BlitzSlot>().Instantiate(cards[0].GetComponent<Card>());
        }

        Cycle(3);


    }


    bool isHandheld;
    float touchTime;
    private void LateUpdate()
    {
        if (hasAuthority && GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().roundInPlay)
        {
            int cursorPhase = 0;//0 is no phase, 1 is down, 2 is moved, 3 is up
            Vector2 cursorPos = Vector2.zero;
            if (isHandheld)
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    cursorPos = touch.position;

                    if (touch.phase == TouchPhase.Began)
                    {
                        cursorPhase = 1;
                    }

                    if (touch.phase == TouchPhase.Moved)
                    {
                        cursorPhase = 2;
                    }

                    if (touch.phase == TouchPhase.Ended)
                    {
                        cursorPhase = 3;
                    }

                }
                else
                {
                    cursorPhase = 0;
                }
            }
            else
            {
                cursorPos = Input.mousePosition;
                cursorPhase = 0;
                if (Input.GetMouseButtonDown(0))
                {

                    cursorPhase = 1;
                }
                else if (Input.GetMouseButton(0))
                {
                    cursorPhase = 2;
                }

                if (Input.GetMouseButtonUp(0))
                {
                    cursorPhase = 3;
                }
            }

            if (cursorPhase == 1)
            {
                Collider2D[] touchedColliders = Physics2D.OverlapPointAll(cursorPos);
                foreach (Collider2D tCol in touchedColliders)
                {
                    if (col == tCol)
                    {
                        touchTime = Time.time;
                        break;
                    }
                }

            }


            if (cursorPhase == 3)
            {

                Collider2D[] touchedColliders = Physics2D.OverlapPointAll(cursorPos);
                foreach (Collider2D tCol in touchedColliders)
                {
                    if (col == tCol)
                    {
                        if (touchTime + .5f > Time.time)
                        {
                            Cycle(3);
                            CmdCycle3();
                        }
                    }
                }

            }
        }
        
    }

    void AddToHand(GameObject cardObj)
    {
        inHand.Add(cardObj);
        cardObj.transform.position = transform.position;
        cardObj.transform.rotation = transform.rotation;
        cardObj.SetActive(false);
    }


    public void AddNewCard(Card card)
    {
        AddToHand(card.gameObject);

    }
    protected override void AddCard(Card card)
    {
        card.gameObject.SetActive(true);
        inHand.Remove(card.gameObject);
        base.AddCard(card);

        if (cards.Count != 1)
        {
            cards[cards.Count - 1].GetComponent<Card>().CanMove = true;
        }

        if (cards.Count != 1)
        {
            cards[cards.Count - 2].GetComponent<Card>().CanMove = false;
        }
    }

    void FillHand()
    {
        foreach(GameObject cardObj in cards)
        {
            AddToHand(cardObj);
        }

        cards = new List<GameObject>();
    }

    void Cycle(int count)
    {
        if (inHand.Count == 0)
        {
            FillHand();
        }

        for(int i = 0; i < count; i++)
        {
            if(inHand.Count > 0)
            {
                AddCard(inHand[0].GetComponent<Card>());
            }
        }
    }

    

    int shuffleCount = 50;
    public void Shuffle()
    {
        for (int i = 0; i < shuffleCount; i++)
        {
            //swaps 2 random cards
            int card1 = UnityEngine.Random.Range(0, inHand.Count);

            int card2 = UnityEngine.Random.Range(0, inHand.Count);

            Swap(card1, card2);
        }

    }

    [ClientRpc]
    void RpcUpdateHand(GameObject[] hand)
    {
        cards = new List<GameObject>();
        inHand = new List<GameObject>();
        foreach (GameObject cardObj in hand)
        {
            AddToHand(cardObj);
        }
    }

    void Swap(int card1, int card2)
    {
        //swaps the order in the hierarchy
        if (card1 < card2)
        {
            inHand[card1].transform.SetSiblingIndex(card2);
            inHand[card2].transform.SetSiblingIndex(card1);
        }
        else if (card1 > card2)
        {
            inHand[card2].transform.SetSiblingIndex(card1);
            inHand[card1].transform.SetSiblingIndex(card2);
        }

        //swaps the order in the list
        GameObject temp = inHand[card1];

        inHand[card1] = inHand[card2];
        inHand[card2] = temp;

        if (card1 == inHand.Count - 1)
        {
            inHand[card2].GetComponent<Card>().CanMove = false;
            inHand[card1].GetComponent<Card>().CanMove = true;
        }
        else if (card2 == inHand.Count - 1)
        {
            inHand[card1].GetComponent<Card>().CanMove = false;
            inHand[card2].GetComponent<Card>().CanMove = true;
        }
    }

    protected override void RemoveCard(Card card)
    {
        base.RemoveCard(card);
        if(cards.Count != 0)
        {
            cards[cards.Count - 1].GetComponent<Card>().CanMove = true;
        }
        
    }

    public override bool AttemptPlace(Card card)
    {
        return false;
    }

    [Command]
    void CmdCycle3()
    {
        RpcCycle3();
    }

    [ClientRpc]
    void RpcCycle3()
    {
        if (!hasAuthority)
        {
            Cycle(3);
        }
        
    }

    public void EmptySlotsAndBlitz(ClientPlayer deductScore)
    {
        foreach (GameObject slot in slots)
        {
            GameObject[] cards = new GameObject[slot.GetComponent<BlitzSlot>().Cards.Count];
            int i = 0;
            foreach (GameObject cardObj in slot.GetComponent<BlitzSlot>().Cards)
            {
                cards[i] = cardObj;
                i++;
            }

            foreach (GameObject cardObj in cards)
            {
                cardObj.GetComponent<Card>().CmdMoveCard(gameObject);
            }
            

        }

        StartCoroutine("DeductScore", deductScore);
    }

    IEnumerator DeductScore(ClientPlayer clientPlayer)
    {
        yield return new WaitForSeconds(1f);
        while (blitzDeck.GetComponent<BlitzDeck>().Cards.Count > 0)
        {
            blitzDeck.GetComponent<BlitzDeck>().Cards[blitzDeck.GetComponent<BlitzDeck>().Cards.Count - 1].GetComponent<Card>().CmdMoveCard(gameObject);
            clientPlayer.Score -= 2;
            yield return new WaitForSeconds(.5f);
        }
        
    }
}

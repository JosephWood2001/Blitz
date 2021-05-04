using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : NetworkBehaviour
{
    public int lastRevertId;

    [SerializeField]
    bool canMove = false;
    public bool CanMove
    {
        get
        {
            return CanMove;
        }

        set
        {
            canMove = value;
        }
    }

    public Vector3 targetPos = Vector3.zero;
    public Quaternion targetRot = Quaternion.identity;

    public string color = "NA";
    public int number = 0;
    public GameObject owner;
    public bool isMale = true; // true is male, false is female

    [Header("refrences")]
    public Image gender1;
    public Image gender2;
    public TextMeshProUGUI[] numberTMPs;

    public Sprite male;
    public Sprite female;

    public CardStack parentStack;

    public GameObject playStackPrefab;

    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    [ClientRpc]
    public void RpcInstantiate(string color, bool isMale, int number, GameObject owner, GameObject parent)
    {
        transform.SetParent(parent.transform);
        this.owner = owner;
        this.color = color;
        this.isMale = isMale;
        this.number = number;
        Instantiate();
    }

    public void Instantiate()
    {
        if (color.Equals("g"))
        {
            GetComponent<Image>().color = Color.green;
        }
        else if (color.Equals("r"))
        {
            GetComponent<Image>().color = Color.red;
        }
        else if (color.Equals("b"))
        {
            GetComponent<Image>().color = Color.cyan;
        }
        else if (color.Equals("y"))
        {
            GetComponent<Image>().color = Color.yellow;
        }

        if (isMale)
        {
            gender1.sprite = male;
            gender2.sprite = male;
        }
        else
        {
            gender1.sprite = female;
            gender2.sprite = female;
        }


        foreach (TextMeshProUGUI numb in numberTMPs)
        {
            numb.text = number.ToString();
        }

        name = owner.GetComponent<ClientPlayer>().playerName + ": " + color + " " + number.ToString();
        
    }

    Collider2D col;
    [SerializeField]
    bool moveAllowed;

    List<Card> stackGrab;

    bool isHandheld;
    private void Start()
    {
        
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            isHandheld = true;
        }
        else
        {
            isHandheld = false;
        }
        //isHandheld = true;
    }
    void Update()
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
            }else if (Input.GetMouseButton(0))
            {
                cursorPhase = 2;
            }

            if (Input.GetMouseButtonUp(0))
            {
                cursorPhase = 3;
            }
        }
        

        if (canMove && hasAuthority && GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().roundInPlay)
        {
            if (cursorPhase == 1)
            {
                Collider2D[] touchedColliders = Physics2D.OverlapPointAll(cursorPos);

                int topMostCardIndex = -1; //the index of the topMost card touched
                int thisCardIndex = -2; // the index of this card, will only change if touched
                foreach (Collider2D tCol in touchedColliders)
                {
                    if (tCol.gameObject.GetComponent<Card>() != null)
                    {
                        int height = tCol.gameObject.GetComponent<Card>().parentStack.CardHeight(tCol.gameObject.GetComponent<Card>());
                        if (height > topMostCardIndex)
                        {
                            topMostCardIndex = height;
                        }
                    }

                    if (col == tCol)
                    {

                        thisCardIndex = parentStack.CardHeight(this);

                    }

                }

                if (thisCardIndex >= topMostCardIndex)//this card is the top card touched
                {
                    moveAllowed = true;
                    if (parentStack.gameObject.GetComponent<BlitzSlot>() != null) //Stack grab
                    {
                        stackGrab = parentStack.gameObject.GetComponent<BlitzSlot>().StackGrab(this);
                    }
                }
            }
             
                
            if (cursorPhase == 2)
            {
                if (moveAllowed)
                {
                    if (stackGrab != null)
                    {
                        int h = 0;
                        foreach(Card card in stackGrab)
                        {
                            card.transform.position = new Vector2(cursorPos.x, cursorPos.y - (h * parentStack.heightOffset));
                            h++;
                        }
                    }
                    else
                    {
                        transform.position = new Vector2(cursorPos.x, cursorPos.y);
                    }
                    
                }
            }

            if (cursorPhase == 3)
            {

                if (moveAllowed)
                {
                    if (stackGrab == null)
                    {
                        if (!AttemptPlace(cursorPos))
                        {
                            if (number == 1)
                            {
                                AttemptNewStack(cursorPos);
                            }
                        }
                        if (parentStack.gameObject.GetComponent<BlitzDeck>())
                        {
                            AttemptBlitzSlotPlace(cursorPos);
                        }
                    }
                    else
                    {
                        if (parentStack.gameObject.GetComponent<BlitzSlot>())
                        {
                            AttemptBlitzSlotPlace(cursorPos);
                                
                        }
                    }
                        

                    parentStack.MoveCardBackToPile(this);
                }

                stackGrab = null;
                moveAllowed = false;

            }            
        }
        if (!moveAllowed)
        {
            if (Vector3.Distance(targetPos, transform.position) < .1f)
            {
                transform.position = targetPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 9f);
            }

            if (Quaternion.Angle(targetRot, transform.rotation) < .1f)
            {
                transform.rotation = targetRot;
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        
    }

    bool AttemptPlace(Vector2 pos)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(pos, Vector2.one, 0);

        if (colliders.Length >= 2)
        {
            foreach (Collider2D collider in colliders)
            {
                if(collider.gameObject != gameObject)
                {
                    if (collider.GetComponent<Card>() != null)
                    {
                        if (collider.GetComponent<Card>().parentStack.AttemptPlace(this))
                        {
                            CmdMoveCard(collider.GetComponent<Card>().parentStack.gameObject);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    bool AttemptBlitzSlotPlace(Vector2 pos)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(pos, Vector2.one, 0);

        if (colliders.Length >= 2)
        {
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject != gameObject)
                if (collider.GetComponent<BlitzSlot>() != null)
                if (collider.GetComponent<BlitzSlot>().hasAuthority)
                {
                    if (stackGrab == null)
                    {
                        if (collider.GetComponent<BlitzSlot>().AttemptPlace(this))
                        {
                            CmdMoveCard(collider.GetComponent<BlitzSlot>().gameObject);
                            return true;
                        }
                    }
                    else
                    {
                        if (collider.GetComponent<BlitzSlot>().AttemptPlace(stackGrab[0]))
                        {
                            foreach (Card card in stackGrab)
                            {
                                card.CmdMoveCard(collider.GetComponent<BlitzSlot>().gameObject);
                            }
                            return true;
                        }
                    }
                }
                
            }
        }

        return false;
    }

    bool AttemptNewStack(Vector2 pos)
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(pos, Vector2.one, 0);

        if (colliders.Length >= 2)
        {
            foreach (Collider2D collider in colliders)
            {
                if (collider.gameObject != gameObject)
                {
                    if (collider.GetComponent<PlaySpace>() != null)
                    {
                        if (collider.GetComponent<PlaySpace>().AttemptPlayStack(this))
                        {
                            CmdNewPlayStack(pos);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    [Command]
    void CmdNewPlayStack(Vector2 pos)
    {
        lastRevertId = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().CreateRevertState(owner.GetComponent<ClientPlayer>());
        if (!GameObject.FindGameObjectWithTag("PlaySpace").GetComponent<PlaySpace>().AttemptPlayStack(this))
        {
            GameManager.RevertState revertState;
            GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().revertStates.TryGetValue(lastRevertId, out revertState);
            revertState.Revert();
        }
        GameObject temp = Instantiate(playStackPrefab);
        NetworkServer.Spawn(temp);
        temp.GetComponent<PlayStack>().RpcInstantiate(pos);
        RpcMoveCard(temp);
    }

    [Command]
    public void CmdMoveCard(GameObject to)
    {
        lastRevertId = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().CreateRevertState(owner.GetComponent<ClientPlayer>());
        if (!to.GetComponent<CardStack>().AttemptPlace(this))
        {
            GameManager.RevertState revertState;
            GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().revertStates.TryGetValue(lastRevertId, out revertState);
            revertState.Revert();
        }
        to.GetComponent<CardStack>().MoveCardHere(this);
        RpcMoveCard(to);
    }

    [ClientRpc]
    public void RpcMoveCard(GameObject to)
    {
        to.GetComponent<CardStack>().MoveCardHere(this);
    }
}

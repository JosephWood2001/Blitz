using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : NetworkBehaviour
{
    public GameObject blitzedAnimation;
    public GameObject startAnimation;
    public GameObject playerBannerPrefab;
    public GameObject[] playerBanners;
    public ClientPlayer clientPlayer;
    public List<ClientPlayer> opponents;
    public GameObject playSpace;
    public GameObject scoreStack;
    

    public int playerCount = 0;
    public int loadedInCount = 0;
    void Start()
    {
        DontDestroyOnLoad(gameObject);

        
    }

    public void LoadGame()
    {

        if (isServer)
        {
            RpcDiscoverPlayers();
            RpcLoadGame();
        }
    }

    

    void StartGame()
    {
        playSpace = Instantiate(playSpace);
        NetworkServer.Spawn(playSpace);
        scoreStack = Instantiate(scoreStack);
        NetworkServer.Spawn(scoreStack, clientPlayer.gameObject);
        NetworkServer.Spawn(scoreStack, clientPlayer.gameObject);
        RpcStartGame();
    }


    [ClientRpc]
    void RpcStartGame()
    {
        roundInPlay = false;
        clientPlayer.StartGame();
        StartCoroutine("NewRoundCountdown");
    }


    [ClientRpc]
    void RpcLoadGame()
    {
        clientPlayer.WaitForLoad();
        
    }

    [ClientRpc]
    void RpcDiscoverPlayers()
    {
        DiscoverPlayers();

    }

    public void DiscoverPlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("ClientPlayer");
        playerCount = players.Length;
        opponents = new List<ClientPlayer>();
        foreach (GameObject player in players)
        {
            if (player.GetComponent<NetworkBehaviour>().hasAuthority)
            {
                clientPlayer = player.GetComponent<ClientPlayer>();
            }
            else
            {
                opponents.Add(player.GetComponent<ClientPlayer>());
            }
        }

        if (isServer && GameObject.FindGameObjectWithTag("PlayerList") != null)
        {
            if (playerCount != GameObject.FindGameObjectWithTag("PlayerList").transform.childCount)
            {
                foreach (GameObject playerBanner in playerBanners)
                {
                    NetworkServer.Destroy(playerBanner);
                }
                int i = 0;
                playerBanners = new GameObject[playerCount];
                foreach (GameObject player in players)
                {
                    playerBanners[i] = Instantiate(playerBannerPrefab);
                    NetworkServer.Spawn(playerBanners[i], player);
                    RpcUpdateBanner(playerBanners[i], player, i);
                    i++;
                }
            }
        }

    }

    [ClientRpc]
    void RpcUpdateBanner(GameObject banner, GameObject player, int index)
    {
        player.GetComponent<ClientPlayer>().banner = banner.GetComponent<TextMeshProUGUI>();
        banner.transform.SetParent(GameObject.FindGameObjectWithTag("PlayerList").transform);
        banner.transform.position = GameObject.FindGameObjectWithTag("PlayerList").transform.position + index * 40 * Vector3.down;
        banner.GetComponent<TextMeshProUGUI>().text = player.GetComponent<ClientPlayer>().PlayerName + " - " + player.GetComponent<ClientPlayer>().isReady.ToString();
    }

    public void LoadedIn()
    {
        loadedInCount++;
        if (loadedInCount >= playerCount)
        {
            RpcSetScores();
            StartGame();
        }
    }

    [ClientRpc]
    void RpcSetScores()
    {
        clientPlayer.ScoreTMP = GameObject.FindGameObjectWithTag("Scores").transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
        int i = 1;
        foreach(ClientPlayer player in opponents)
        {
            player.ScoreTMP = GameObject.FindGameObjectWithTag("Scores").transform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();
            i++;
        }
    }

    [SyncVar]
    public bool roundInPlay = false;

    public void EndRound(string who)
    {
        if (isServer)
        {
            roundInPlay = false;
            RpcBlitzed(who);
            scoreStack.GetComponent<ScoreStack>().CmdCollect();

            
        }
        
        clientPlayer.deck.GetComponent<Deck>().EmptySlotsAndBlitz(clientPlayer);

        if (isServer)
        {
            scoreStack.GetComponent<ScoreStack>().CmdDistributePoints();
        }
        
    }

    [ClientRpc]
    public void RpcBlitzed(string who)
    {
        Debug.Log("test");
        GameObject animation = Instantiate(blitzedAnimation);
        animation.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        animation.transform.localPosition = Vector3.zero;
        animation.GetComponentInChildren<TextMeshProUGUI>().text = who + " Blitzed!";
        Destroy(animation, 3f);
    }

    public void NewRound()
    {

        PlayStack[] playStacks = GameObject.FindGameObjectWithTag("PlaySpace").transform.GetComponentsInChildren<PlayStack>();
        foreach (PlayStack playStack in playStacks)
        {
            NetworkServer.Destroy(playStack.gameObject);
        }

        clientPlayer.deck.GetComponent<Deck>().NewRound();

        foreach(ClientPlayer player in opponents)
        {
            player.deck.GetComponent<Deck>().NewRound();
        }

        RpcNewRoundCountDown();
    }
    [ClientRpc]
    void RpcNewRoundCountDown()
    {
        StartCoroutine("NewRoundCountdown");
    }

    IEnumerator NewRoundCountdown()
    {
        GameObject animation = Instantiate(startAnimation);
        animation.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform);
        animation.transform.localPosition = Vector3.zero;
        Destroy(animation, 7.5f);
        yield return new WaitForSeconds(6f);
        Debug.Log("Go");
        roundInPlay = true;
    }



    //only ran on client with authority

    int placeId = 0;

    public int PlaceId
    {
        get
        {
            return placeId++;
        }
    }

    public Dictionary<int, RevertState> revertStates = new Dictionary<int, RevertState>();

    //returns the revert state id
    public int CreateRevertState(ClientPlayer player)
    {
        int temp = PlaceId;
        revertStates.Add(temp, new RevertState(player.deck.GetComponent<Deck>()));
        return temp;
    }

    public class RevertState
    {
        public Deck deckRef;
        public GameObject[] inDeck;
        public GameObject[] inHand;
        public BlitzSlot[] blitzSlotsRef;
        public GameObject[][] blitzSlotsCards;
        public BlitzDeck blitzDeckRef;
        public GameObject[] blitzDeckCards;

        public RevertState(Deck deck)
        {
            this.deckRef = deck;
            blitzDeckRef = deck.blitzDeck.GetComponent<BlitzDeck>();
            blitzSlotsRef = new BlitzSlot[deck.slots.Length];
            blitzSlotsCards = new GameObject[deck.slots.Length][];
            for (int i = 0; i < blitzSlotsRef.Length; i++)
            {
                blitzSlotsRef[i] = deck.slots[i].GetComponent<BlitzSlot>();
                blitzSlotsCards[i] = CloneRefs(deck.slots[i].GetComponent<BlitzSlot>().Cards);
            }

            inDeck = CloneRefs(deckRef.Cards);
            inHand = CloneRefs(deckRef.inHand);

            blitzDeckCards = CloneRefs(blitzDeckRef.Cards);

        }

        GameObject[] CloneRefs(List<GameObject> gameObjects)
        {
            GameObject[] clone = new GameObject[gameObjects.Count];
            int i = 0;
            foreach (GameObject gameObject in gameObjects)
            {
                clone[i] = gameObject;
                i++;
            }

            return clone;
        }

        public void Revert()
        {
            deckRef.CmdRevert(inDeck);
            blitzDeckRef.CmdRevert(blitzDeckCards);
            int i = 0;
            foreach (BlitzSlot slot in blitzSlotsRef)
            {
                slot.CmdRevert(blitzSlotsCards[i]);
                i++;
            }
        }

    }

}

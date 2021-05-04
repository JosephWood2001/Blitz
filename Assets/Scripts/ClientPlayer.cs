using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using TMPro;

public class ClientPlayer : NetworkBehaviour
{

    int score = 0;
    public int Score
    {
        get
        {
            return score;
        }

        set
        {
            if (!hasAuthority)//if no authority, then must be server to call this legally
            {
                RpcUpdateScore(value);
            }
            else
            {
                CmdUpdateScore(value);
            }

        }
    }
    [Command]
    public void CmdUpdateScore(int value)
    {
        score = value;
        if (ScoreTMP != null)
            ScoreTMP.text = PlayerName + ":" + score.ToString();
        RpcUpdateScore(value);
    }

    [ClientRpc]
    public void RpcUpdateScore(int value)
    {
        score = value;
        if (ScoreTMP != null)
            ScoreTMP.text = PlayerName + ":" + score.ToString();
    }

    TextMeshProUGUI scoreTMP;
    public TextMeshProUGUI ScoreTMP
    {
        get
        {
            return scoreTMP;
        }

        set
        {
            scoreTMP = value;
            if (ScoreTMP != null)
                ScoreTMP.text = PlayerName + ":" + score.ToString();
        }
    }

    public TextMeshProUGUI banner;
    [SyncVar(hook = nameof(UpdateBannerString))]
    public string playerName = "None";
    public string PlayerName
    {
        get
        {
            return playerName;
        }
        set
        {
            CmdSetName(value);
        }
    }

    public void UpdateBannerString(string oldValue, string newValue)
    {
        UpdateBanner();
    }

    public void UpdateBannerBool(bool oldValue, bool newValue)
    {
        UpdateBanner();
    }

    public void UpdateBanner()
    {
        if (banner != null)
        {
            banner.text = PlayerName + " - " + isReady.ToString();
        }
    }

    [Command]
    void CmdSetName(string name)
    {
        playerName = name;
    }

    public GameObject deck;
    public GameObject blitzDeck;
    public GameObject blitzSpot;
    GameObject[] blitzSlots;
    public GameObject card;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        if (isServer && hasAuthority)
        {
            isReady = true;
        }
    }

    public void WaitForLoad()
    {
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        StartCoroutine("WaitToSetMenuToGame");
    }
    IEnumerator WaitToSetMenuToGame()
    {

        while (GameObject.FindGameObjectWithTag("Canvas") == null)
        {
            yield return null;
        }

        CmdLoadedIn();


    }

    public void StartGame()
    {

        Score = 0;
        CmdDeckSetup();

    }

    [Command]
    void CmdDeckSetup()
    {
        deck = Instantiate(deck);
        NetworkServer.Spawn(deck, gameObject);
        deck.GetComponent<Deck>().Instantiate(gameObject, Random.Range(0, 1000).ToString());
    }

    [Command]
    void CmdLoadedIn()
    {
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().LoadedIn();
    }
    [SyncVar(hook = nameof(UpdateBannerBool))]
    public bool isReady = false;
    [Command]
    public void CmdReady()
    {
        if (!isReady)
        {
            isReady = true;
        }

    }

    [Command]
    public void CmdBlitz()
    {
        RpcBlitz();
    }

    [ClientRpc]
    void RpcBlitz()
    {
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().EndRound(playerName);
    }


}

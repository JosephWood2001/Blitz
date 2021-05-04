using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class BeginGame : NetworkBehaviour
{

    public GameObject gameManager;

    public TextMeshProUGUI playerName;

    private void Start()
    {
        if (isServer)
        {
            gameManager = Instantiate(gameManager);
            NetworkServer.Spawn(gameManager);
            gameManager.GetComponent<GameManager>().DiscoverPlayers();
        }
        else
        {
            GetComponentInChildren<TextMeshProUGUI>().text = "Not Ready";
        }
        
    }
    float i = 0;
    private void FixedUpdate()
    {
        if (i % 100 == 0)
        {
            SlowUpdate();
        }
        i++;
    }

    void SlowUpdate()
    {
        gameManager.GetComponent<GameManager>().DiscoverPlayers();
        if (playerName.text.Length > 1)
        {
            if (playerName.text.Length > 5)
            {
                gameManager.GetComponent<GameManager>().clientPlayer.PlayerName = playerName.text.Substring(0,5);
            }
            else
            {
                gameManager.GetComponent<GameManager>().clientPlayer.PlayerName = playerName.text;
            }
            
        }
        
    }

    public void StartGame()
    {
        if (!isServer)
        {
            GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
            gameManager.GetComponent<GameManager>().DiscoverPlayers();
            SlowUpdate();
            gameManager.GetComponent<GameManager>().clientPlayer.GetComponent<ClientPlayer>().CmdReady();
        }
        else
        {
            SlowUpdate();
            foreach (ClientPlayer player in gameManager.GetComponent<GameManager>().opponents)
            {
                if (!player.isReady)
                {
                    goto breakOut;
                }
            }
            gameManager.GetComponent<GameManager>().LoadGame();
            breakOut:
                int dummy; //useless, only to give breakout something to do
        }
        
    }
}

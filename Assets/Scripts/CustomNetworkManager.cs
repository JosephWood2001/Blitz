using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class CustomNetworkManager : NetworkManager
{
    bool host;
    
    public void HostGame()
    {
        host = true;
        base.StartHost();
    }

    public void JoinLocalGame()
    {
        host = false;
        base.StartClient();
    }

    public void JoinGame(string address) //"192.168.0.108:7777"
    {
        host = false;
        base.StartClient(new System.Uri("tcp4://" + address));
    }

    public void Stop()
    {
        if (host)
        {
            StopHost();
        }
        else
        {
            StopClient();
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostJoinButton : MonoBehaviour
{
    public void Host()
    {
        GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<CustomNetworkManager>().HostGame();
    }

    public void Join()
    {
        GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<CustomNetworkManager>().JoinLocalGame();
    }
}

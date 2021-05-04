using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class ExitGame : MonoBehaviour
{
    public void ExitTheGame()
    {
        GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<CustomNetworkManager>().Stop();
    }
}

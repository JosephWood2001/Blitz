using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewRoundButton : MonoBehaviour
{
    public void NewRound()
    {
        Debug.Log("Starting New Round");
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>().NewRound();
        gameObject.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Join : MonoBehaviour
{
    public CustomNetworkManager CNM;
    public TMP_InputField text;
    public void JoinGame()
    {
        CNM.JoinGame(text.text);
    }
}

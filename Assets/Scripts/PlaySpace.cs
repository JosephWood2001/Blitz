using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class PlaySpace : NetworkBehaviour
{
    public Collider2D col;

    public PlayStack playStackPrefab;

    private void Start()
    {
        this.transform.SetParent(GameObject.FindGameObjectWithTag("Canvas").transform, false);
        transform.SetSiblingIndex(2);
    }

    public bool AttemptPlayStack(Card card)
    {
        List<Collider2D> results = new List<Collider2D>();
        
        Physics2D.OverlapCollider(card.gameObject.GetComponent<Collider2D>(), new ContactFilter2D().NoFilter(), results);

        foreach (Collider2D collider in results)
        {
            if (collider.gameObject.GetComponent<PlayStack>() != null)
            {
                return false;
            }
        }

        if (card.number == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
        
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChangeCardText : MonoBehaviour
{
    public GameObject CardStats;
    public TMP_Text CardName;
    public TMP_Text CardDescription;
    Card card;
    public static ChangeCardText instance;
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        card = FindObjectOfType<Card>();
        instantiateStats(card.td.Name, card.td.Description);
    }
    
    void instantiateStats(string textName, string textDescription)
    {
        ///TODO: Cambiar texto de stats
        CardName.text = textName;
        CardDescription.text = textDescription;
        ///Que se elimine el stat al salir del hover
        ///Que solo se instancie 1 statcard
    }
}

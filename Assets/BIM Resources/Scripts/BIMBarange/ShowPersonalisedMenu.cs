using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowPersonalisedMenu : MonoBehaviour
{
    public List<GameObject> lecteurItems;
    public List<GameObject> calculateurItems;
    public List<GameObject> modelisateurItems;
    public List<GameObject> tutorialItems;

    // Start is called before the first frame update
    void Start()
    {
        string playerName = PlayerPrefs.GetString("PlayerName", $"Player {NetworkManager.Instance.Runner.LocalPlayer.PlayerId}");

        if (playerName == "Lecteur")
        {
            foreach (GameObject item in lecteurItems)
            {

                item.SetActive(true);
                if (item.name == "Arreter l'enregistrement")
                    Debug.Log("[Emery] Lecteur, tu n'as pas le droit de voir cet item, je vais le cacher pour toi.");
                //    item.SetActive(false);
                
            }
        }
        else if (playerName == "Calculateur")
        {
            foreach (GameObject item in calculateurItems)
            {
                item.SetActive(true);
            }
        }
        else if (playerName == "Modelisateur")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }
        }
        else if(playerName == "Blue")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }

        }
        else if (playerName == "Red")
        {
            foreach (GameObject item in modelisateurItems)
            {
                item.SetActive(true);
            }
        }

    }


}

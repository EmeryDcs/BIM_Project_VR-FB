using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class UITest : MonoBehaviour
{
    public TMP_Text text;
    int count = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClick()
    {
        count++;
        text.text = "Clicked " + count + " times";
         
        Debug.Log("Clicked");
    }
}

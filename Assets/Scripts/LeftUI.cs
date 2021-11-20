using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeftUI : MonoBehaviour
{
    public Text cellUI;

    // Update is called once per frame
    void Update()
    {
        cellUI.text =
            "On Site Material"+ "\r\n"+ "Initial Unit" + "\r\n"+ "Excavated Material";
    }
}

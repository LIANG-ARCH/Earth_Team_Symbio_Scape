using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayUI : MonoBehaviour
{
    public Text calculation;
    void Update()
    {
        calculation.text = "Cubic Lattice Scale" + "\r\n" + "Column number : " + Env.Instance.colNum + "\r\n" 
            + "Row number : " + Env.Instance.rowNum + "\r\n" + "High number : " + Env.Instance.highNum + "\r\n" + "\r\n"
            + "Iteration : " + Env.Instance.currentRound.ToString() + "\r\n" + "Unit number : " + Env.Instance.initNum+"\r\n"+"\r\n"
            + "Original site" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n";


    }
}



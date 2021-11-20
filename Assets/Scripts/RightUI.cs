using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RightUI : MonoBehaviour
{
    public Text gridUI;

    // Update is called once per frame
    void Update()
    {
        gridUI.text =
            "Additive And Subtractive Strategy" + "\r\n"+ "Total on site material : " +Env.Instance.totalMaterial+ "\r\n"
            + "Current material excavated : " +Env.Instance.cellNum+ "\r\n"+ "Current on site material : " + Env.Instance.restMaterial+ "\r\n" + "Structural porosity : " +Env.Instance.overcrowdedNum+ "\r\n"
            + "Material efficiency :  " + Env.Instance.p +  "%" + "\r\n"+ "Structural stability : " + "\r\n" + "\r\n"  + "Excavated site" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n"
            + "Reconstructed structure on the ground : " + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "\r\n" + "Parameter"+ "\r\n"+"Temperature : " + "\r\n" + "Sunlight : " + "\r\n"
            + "Wind : " + "\r\n" + "Material property : " + "\r\n"
            ;
           


    }
}

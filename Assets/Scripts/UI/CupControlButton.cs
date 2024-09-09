using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CupControlButton : MonoBehaviour
{
    public Camera mainCam;
    public Camera effactCam;
    public CupComponent cup;

    bool isAttachedEvent = false;

    private void Start()
    {
    }

    public void RotateCup()
    {
        float currentRotation = GetComponent<Slider>().value * cup.maxSlopeValue;
        cup.SetRotation(currentRotation);
        mainCam.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        effactCam.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        cup.glassBody.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        
    }

    public void FillCup()
    {
        if(isAttachedEvent == false && transform.name == "Slider_Filling") 
        {
            cup.OverLiquidEvent += FixFillingTestSlider;
            isAttachedEvent = true;
        }
        cup.FillIn(GetComponent<Slider>().value);
    }

    public void FixFillingTestSlider(float value)
    {
        GetComponent<Slider>().value = value;
    }

}

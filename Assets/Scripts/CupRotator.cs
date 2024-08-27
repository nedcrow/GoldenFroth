using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CupRotator : MonoBehaviour
{
    public Camera mainCam;
    public Camera effactCam;
    public CupComponent cup;

    public void RotateCup()
    {
        float currentRotation = GetComponent<Slider>().value * cup.maxSlopeValue;
        cup.SetRotation(currentRotation);
        mainCam.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        effactCam.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        cup.glassBody.transform.localRotation = Quaternion.Euler(0, 0, currentRotation);
        
    }

}

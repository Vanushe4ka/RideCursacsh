using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class CarUIController : MonoBehaviour
{
    [SerializeField] SomeMeter speedometer;
    [SerializeField] SomeMeter takchometer;
    [SerializeField] Text transmissionText;
    [SerializeField] Text DebugText;
    public void Init(float maxSpeed, float maxTakco)
    {
        speedometer.Init(maxSpeed);
        takchometer.Init(maxTakco,true);
    }
    public void UpdateValues(float speed, float takho)
    {
        speedometer.UpdateValue(speed);
        takchometer.UpdateValue(takho);
    }
    public void UpdateTransmission(int i)
    {
        switch (i)
        {
            case 0:
                transmissionText.text = "R";
                break;
            case 1:
                transmissionText.text = "N";
                break;
            default:
                transmissionText.text = (i-1).ToString();
                break;
        }
    }
    public void Debug(string str)
    {
        DebugText.text = str;
    }
}

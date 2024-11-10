using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SomeMeter : MonoBehaviour
{
    float maxValue;
    bool isSmooses = false;
    float value;
    [SerializeField] Vector2 minMaxAngle;
    [SerializeField] Transform arrow;
    public void Init(float maxValue, bool isSmooses = false)
    {
        this.maxValue = maxValue;
        this.isSmooses = isSmooses;
    }
    public void UpdateValue(float value)
    {
        float t = value / maxValue;
        float angle = Mathf.Lerp(minMaxAngle.x, minMaxAngle.y, t);
        if (isSmooses)
        {
            StopAllCoroutines();
            StartCoroutine(ChangeValue(angle));
        }
        else
        {
            arrow.rotation = Quaternion.Euler(0, 0, angle);
        }
        
    }
    IEnumerator ChangeValue(float angle)
    {
        while (arrow.rotation != Quaternion.Euler(0, 0, angle))
        {
            arrow.rotation = Quaternion.Lerp(arrow.rotation, Quaternion.Euler(0, 0, angle), 0.25f);
            yield return null;
        }
    }
}

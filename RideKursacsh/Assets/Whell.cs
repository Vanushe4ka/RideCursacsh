using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WheelFrictionCurveData
{
    public float extremumSlip;
    public float extremumValue;
    public float asymptoteSlip;
    public float asymptoteValue;
    public float stiffness;

    // Преобразовать эти параметры в WheelFrictionCurve
    public WheelFrictionCurve ToWheelFrictionCurve()
    {
        return new WheelFrictionCurve
        {
            extremumSlip = this.extremumSlip,
            extremumValue = this.extremumValue,
            asymptoteSlip = this.asymptoteSlip,
            asymptoteValue = this.asymptoteValue,
            stiffness = this.stiffness
        };
    }

    // Загрузить параметры из WheelFrictionCurve
    public void FromWheelFrictionCurve(WheelFrictionCurve curve)
    {
        this.extremumSlip = curve.extremumSlip;
        this.extremumValue = curve.extremumValue;
        this.asymptoteSlip = curve.asymptoteSlip;
        this.asymptoteValue = curve.asymptoteValue;
        this.stiffness = curve.stiffness;
    }
}
public class Whell : MonoBehaviour
{
    int prevIndex = 0;
    public WheelFrictionCurveData[] forwardFrictionCurveOfTexture;
    public WheelFrictionCurveData[] sidewaysFrictionCurveOfTexture;
    [SerializeField] WheelCollider wheelCollider;

    [SerializeField] float streatingInDirt;
    public void FixedUpdate()
    {
        if (!GameController.isCanUpdate) { return; }
        int textureIndex = GameController.Instance().GetTextureIndexAtPoint(transform.position);
        if (textureIndex != prevIndex)
        {
            prevIndex = textureIndex;
            wheelCollider.forwardFriction = forwardFrictionCurveOfTexture[textureIndex].ToWheelFrictionCurve();
            wheelCollider.sidewaysFriction = sidewaysFrictionCurveOfTexture[textureIndex].ToWheelFrictionCurve();
        }
        if (textureIndex == 1)
        {
            float noise = Mathf.PerlinNoise(Time.time * 0.5f, 0) * 2f - 1f; // Случайное значение от -1 до 1
            float correctionFactor = Mathf.Lerp(1f, 0.8f, Mathf.Abs(noise)); // Корректировка сцепления

            WheelFrictionCurve currentSidewaysFriction = wheelCollider.sidewaysFriction;
            WheelFrictionCurve currentForwardFriction = wheelCollider.forwardFriction;
            currentSidewaysFriction.stiffness = sidewaysFrictionCurveOfTexture[textureIndex].stiffness * correctionFactor;
            currentForwardFriction.stiffness = forwardFrictionCurveOfTexture[textureIndex].stiffness * correctionFactor;
            wheelCollider.sidewaysFriction = currentSidewaysFriction;
            wheelCollider.forwardFriction = currentForwardFriction;

            wheelCollider.steerAngle += Random.Range(-streatingInDirt, streatingInDirt);
        }
    }
}

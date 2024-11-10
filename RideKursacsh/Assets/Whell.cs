using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Whell : MonoBehaviour
{
    int baseSurfaceAdhesion = 1;
    [SerializeField] float rotateAngle;
    public bool isFront = false; // Это поле нужно для того, чтобы различать передние и задние колёса

    public float frictionCoefficient = 0.8f; // Коэффициент трения (можно настроить)

    // Получаем силу тяги
    public float GetForce(float velocity)
    {
        return velocity / baseSurfaceAdhesion;
    }

    // Получаем момент силы, создаваемый поворотом колеса
    public float GetTurningMoment(float velocity)
    {
        // Момент зависит от угла поворота колеса и скорости
        float angleInRadians = Mathf.Deg2Rad * rotateAngle;
        float turningMoment = Mathf.Sin(angleInRadians) * frictionCoefficient * velocity;
        return turningMoment;
    }

    // Функция поворота колеса
    public void Rotate(float t)
    {
        t = (t + 1) / 2;  // Преобразуем значение в диапазон [0, 1]
        transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(-rotateAngle, rotateAngle, t), 0);
    }
}

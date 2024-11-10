using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Whell : MonoBehaviour
{
    int baseSurfaceAdhesion = 1;
    [SerializeField] float rotateAngle;
    public bool isFront = false; // ��� ���� ����� ��� ����, ����� ��������� �������� � ������ �����

    public float frictionCoefficient = 0.8f; // ����������� ������ (����� ���������)

    // �������� ���� ����
    public float GetForce(float velocity)
    {
        return velocity / baseSurfaceAdhesion;
    }

    // �������� ������ ����, ����������� ��������� ������
    public float GetTurningMoment(float velocity)
    {
        // ������ ������� �� ���� �������� ������ � ��������
        float angleInRadians = Mathf.Deg2Rad * rotateAngle;
        float turningMoment = Mathf.Sin(angleInRadians) * frictionCoefficient * velocity;
        return turningMoment;
    }

    // ������� �������� ������
    public void Rotate(float t)
    {
        t = (t + 1) / 2;  // ����������� �������� � �������� [0, 1]
        transform.localRotation = Quaternion.Euler(0, Mathf.Lerp(-rotateAngle, rotateAngle, t), 0);
    }
}

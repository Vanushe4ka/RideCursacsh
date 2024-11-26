using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AIController : MonoBehaviour
{
    public Car car;
    [SerializeField] float maxSpeed = 50f; // ������������ ��������
    [SerializeField] float turnSensitivity = 1f; // ���������������� � ���������
    [SerializeField] float slowDownRadius = 5f; // ������, ��� �� ����������� ����� ������

    public void SetValues(float maxSpeed, float sensitivity, float slowDownRad)
    {
        this.maxSpeed = maxSpeed;
        this.turnSensitivity = sensitivity;
        this.slowDownRadius = slowDownRad;
    }

    List<Vector3> path; // ������ ����� ������
    int currentPathIndex = 0; // ������� �����
    float maxDistanceToPoint;

    bool isRestartingEngine = false; // ��� �������������� ���������� �������

    void Start()
    {
        path = GameController.Instance().CheckAIPath();
        maxDistanceToPoint = GameController.Instance().maxDistanceForAIPointToCar();

        if (car == null)
        {
            Debug.LogError("AIController requires a car to control.");
            enabled = false;
        }
    }

    void Update()
    {
        // ��������� ��������� ���������
        HandleEngine();

        // ������� �� ������ ������ ���� ��������� ��������
        if (car.isEngineRun)
        {
            FollowPath();
        }
    }

    void HandleEngine()
    {
        if (!car.isEngineRun && !isRestartingEngine)
        {
            StartCoroutine(RestartEngineRoutine());
        }
        else
        {
            float relativeRPM = car.GetAverageWheelRPM() / car.MaxRPM;
            if (relativeRPM > 0.5f)
            {
                car.ChangeTransmission(1);
            }
            if (relativeRPM < 0.2f && car.currentGear > 2)
            {
                car.ChangeTransmission(-1);
            }
        }
    }

    IEnumerator RestartEngineRoutine()
    {
        isRestartingEngine = true;

        // ������ ����������� ��������
        car.SetTransmission(1);

        // �������� ������� ���������
        car.RestartEngine();

        // ����, ���� ��������� ���������
        yield return new WaitForSeconds(1f);

        // ������ �������� ������ (������ 2)
        if (car.isEngineRun)
        {
            car.SetTransmission(2);
        }

        isRestartingEngine = false;
    }

    void FollowPath()
    {
        if (path == null || path.Count == 0) return;

        // ������� ����
        Vector3 targetPoint = path[currentPathIndex];
        Vector3 directionToTarget = targetPoint - car.transform.position;

        // ������������ �� ��������� �����
        if (directionToTarget.magnitude <= maxDistanceToPoint)
        {
            currentPathIndex = (currentPathIndex + 1) % path.Count;
            targetPoint = path[currentPathIndex];
            directionToTarget = targetPoint - car.transform.position;
        }

        // ������������ ���� ��������
        Vector3 localTarget = car.transform.InverseTransformPoint(targetPoint);
        float steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1, 1);
        car.RotateWhell(steerInput * turnSensitivity);

        // ����������� ��������
        float distanceToTarget = directionToTarget.magnitude;
        float targetSpeed = Mathf.Lerp(10f, maxSpeed, Mathf.Clamp01(distanceToTarget / slowDownRadius));
        float currentSpeed = car.GetComponent<Rigidbody>().velocity.magnitude;

        if (currentSpeed < targetSpeed)
        {
            car.ChangeGas(1f);
        }
        else
        {
            car.ChangeGas(0f);
        }

        // ���������� � ������� ���������
        if (Mathf.Abs(steerInput) > 0.7f && currentSpeed > maxSpeed * 0.5f)
        {
            car.ChangeGas(0f);
            car.HandleBreak(true);
        }
        else
        {
            car.HandleBreak(false);
        }
    }
}

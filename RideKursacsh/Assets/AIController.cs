using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AIController : MonoBehaviour
{
    public Car car;
    [SerializeField] float maxSpeed = 50f; // Максимальная скорость
    [SerializeField] float turnSensitivity = 1f; // Чувствительность к поворотам
    [SerializeField] float slowDownRadius = 5f; // Радиус, где ИИ замедляется перед точкой

    public void SetValues(float maxSpeed, float sensitivity, float slowDownRad)
    {
        this.maxSpeed = maxSpeed;
        this.turnSensitivity = sensitivity;
        this.slowDownRadius = slowDownRad;
    }

    List<Vector3> path; // Список точек трассы
    int currentPathIndex = 0; // Текущая точка
    float maxDistanceToPoint;

    bool isRestartingEngine = false; // Для предотвращения повторного запуска

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
        // Проверяем состояние двигателя
        HandleEngine();

        // Следуем по трассе только если двигатель работает
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

        // Ставим нейтральную передачу
        car.SetTransmission(1);

        // Пытаемся завести двигатель
        car.RestartEngine();

        // Ждем, пока двигатель заведется
        yield return new WaitForSeconds(1f);

        // Ставим передачу вперед (обычно 2)
        if (car.isEngineRun)
        {
            car.SetTransmission(2);
        }

        isRestartingEngine = false;
    }

    void FollowPath()
    {
        if (path == null || path.Count == 0) return;

        // Текущая цель
        Vector3 targetPoint = path[currentPathIndex];
        Vector3 directionToTarget = targetPoint - car.transform.position;

        // Переключение на следующую точку
        if (directionToTarget.magnitude <= maxDistanceToPoint)
        {
            currentPathIndex = (currentPathIndex + 1) % path.Count;
            targetPoint = path[currentPathIndex];
            directionToTarget = targetPoint - car.transform.position;
        }

        // Рассчитываем угол поворота
        Vector3 localTarget = car.transform.InverseTransformPoint(targetPoint);
        float steerInput = Mathf.Clamp(localTarget.x / localTarget.magnitude, -1, 1);
        car.RotateWhell(steerInput * turnSensitivity);

        // Регулировка скорости
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

        // Торможение в сложных ситуациях
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

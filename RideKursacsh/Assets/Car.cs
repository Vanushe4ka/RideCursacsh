using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] WheelCollider frontLeftWheel;
    [SerializeField] WheelCollider frontRightWheel;
    [SerializeField] WheelCollider rearLeftWheel;
    [SerializeField] WheelCollider rearRightWheel;
    [SerializeField] WheelCollider engineWhel; 

    // Параметры для моделей колес (визуальные)
    [SerializeField] Transform frontLeftModel;
    [SerializeField] Transform frontRightModel;
    [SerializeField] Transform rearLeftModel;
    [SerializeField] Transform rearRightModel;
    [SerializeField] Rigidbody rb;

    // Параметры управления
    public float motorForce = 1500f;
    public float steeringAngle = 30f;
    public float brakeForce = 3000f;

    float steerInput;
    float motorInput;
    [SerializeField] bool isEngineRun= false;

    [SerializeField] CarUIController SMTM;
    [SerializeField] int currentGear;
    [SerializeField] float[] gearRatios;
    [SerializeField] AnimationCurve PowerFromNRE;
    public float engineTorque;
    public float accelerationCurve;
    public int MaxRPM;

    public float engineResistance = 5f; // значение сопротивления

    public float starterForce;

    float initialDrag;
    private void Start()
    {
        initialDrag = rb.drag;
        if (SMTM != null)
        {
            SMTM.Init(125 / 3.6f, MaxRPM);
            SMTM.UpdateTransmission(currentGear);
        }
    }
    public void RestartEngine()
    {
        StartCoroutine(RestartEngCorutne());
    }
    public void BreakEngine()
    {
        isEngineRun = false;
        engineWhel.motorTorque = 0;
        frontLeftWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
    }
    IEnumerator RestartEngCorutne()
    {
        if (currentGear == 1)
        {
            engineWhel.motorTorque = starterForce;
        }
        else
        {
            frontLeftWheel.motorTorque = starterForce;
            rearLeftWheel.motorTorque = starterForce;
        }
        yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        engineWhel.motorTorque = 0;
        frontLeftWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
        isEngineRun = true;
    }
    public void ChangeGas(float input)
    {
        motorInput = input;
    }
    public void RotateWhell(float input)
    {
        steerInput = input;
    }
    
    public void ChangeTransmission(int i)
    {
        currentGear = Mathf.Clamp(currentGear + i, 0, gearRatios.Length-1);
        SMTM.UpdateTransmission(currentGear);
        
    }
    private float RestoreTorque(WheelCollider w)
    {
        float wheelRPMBeforeShift = w.rpm;
        float targetAngularVelocity = (wheelRPMBeforeShift / 60f) * 2f * Mathf.PI; // Переводим RPM в рад/с
        float inertia = 0.5f * w.mass * Mathf.Pow(w.radius, 2); // Момент инерции колеса
        return targetAngularVelocity * inertia / Time.fixedDeltaTime;
    }
    private void FixedUpdate()
    {
        if (isEngineRun)
        {
            HandleMotor();
        }
        else
        {
            BreakEngine();
        }

        HandleSteering();
        UpdateWheelModels();
        HandleUI();
    }
    float GetAverageWheelRPM()
    {
        //if (!isEngineRun) { return 0; }
        if (currentGear == 1)
        {
            return engineWhel.rpm;
        }
        float averageRPM = (frontRightWheel.rpm + frontLeftWheel.rpm + rearLeftWheel.rpm + rearRightWheel.rpm) / 4;

        return averageRPM / gearRatios[currentGear];
    }
    public static float FindFirstTimeForValue(AnimationCurve curve, float targetValue, float tolerance = 0.01f, float step = 0.01f)
    {
        // Получаем начальное и конечное время из ключей кривой
        float startTime = curve.keys[0].time;
        float endTime = curve.keys[curve.length - 1].time;

        for (float t = startTime; t <= endTime; t += step)
        {
            float value = curve.Evaluate(t);

            // Проверяем, находится ли значение в пределах допустимой погрешности
            if (Mathf.Abs(value - targetValue) <= tolerance)
            {
                return t; // Возвращаем первое найденное время
            }
        }

        return 1; // Если значение не найдено
    }
    void HandleUI()
    {
        if (SMTM != null)
        {
            SMTM.UpdateValues(rb.velocity.magnitude, GetAverageWheelRPM());
        }
    }
    void HandleSteering()
    {
        frontLeftWheel.steerAngle = steerInput * steeringAngle;
        frontRightWheel.steerAngle = steerInput * steeringAngle;
    }
    public void HandleBreak(bool isBreaking)
    {
        if (isBreaking)
        {
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;
        }
        else
        {
            rearLeftWheel.brakeTorque = 0;
            rearRightWheel.brakeTorque = 0;
        }
    }
    void HandleMotor()
    {
        // Передача силы на колеса
        float throttle = Mathf.Clamp(motorInput, 0f, 1f) + 0.15f; // Только вперед
        float currentTorque = (engineTorque * gearRatios[currentGear]) * throttle;
        
        if (currentGear == 1)
        {
            engineWhel.motorTorque = currentTorque;
            engineWhel.motorTorque -= engineWhel.rpm * engineResistance * Time.deltaTime;
        }
        else
        {
            frontLeftWheel.motorTorque = currentTorque;
            frontLeftWheel.motorTorque -= frontLeftWheel.rpm * engineResistance * Mathf.Abs(currentGear - 1) * Time.deltaTime;
            frontRightWheel.motorTorque = currentTorque;
            frontRightWheel.motorTorque -= frontRightWheel.rpm * engineResistance * Mathf.Abs(currentGear - 1) * Time.deltaTime;
        }

        
    }

    void UpdateWheelModels()
    {
        UpdateWheelPosition(frontLeftWheel, frontLeftModel);
        UpdateWheelPosition(frontRightWheel, frontRightModel);
        UpdateWheelPosition(rearLeftWheel, rearLeftModel);
        UpdateWheelPosition(rearRightWheel, rearRightModel);
    }

    void UpdateWheelPosition(WheelCollider collider, Transform model)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        model.position = pos;
        model.rotation = rot;
    }



}

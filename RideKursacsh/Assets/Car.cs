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

    // ��������� ��� ������� ����� (����������)
    [SerializeField] Transform frontLeftModel;
    [SerializeField] Transform frontRightModel;
    [SerializeField] Transform rearLeftModel;
    [SerializeField] Transform rearRightModel;
    [SerializeField] Rigidbody rb;

    // ��������� ����������
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
    public int MaxRPM;

    public float engineResistance = 5f; // �������� �������������

    public float starterForce;

    IEnumerator RestartCorutine;
    bool isStarting = false;
    bool isSwitchFromN = false;
    [SerializeField] float idleTorqleCoef;
    private void Start()
    {
        if (SMTM != null)
        {
            SMTM.Init(125 / 3.6f, MaxRPM);
            SMTM.UpdateTransmission(currentGear);
        }
    }
    public void RestartEngine()
    {
        if (RestartCorutine != null) { StopCoroutine(RestartCorutine); }
        StartCoroutine(RestartCorutine = RestartEngCorutne());
    }
    public void BreakEngine()
    {
        isEngineRun = false;
        //engineWhel.motorTorque = 0;
        //frontLeftWheel.motorTorque = 0;
        //frontRightWheel.motorTorque = 0;

        //engineWhel.brakeTorque = baseBrakeForce();
        //frontLeftWheel.brakeTorque = baseBrakeForce();
        //frontRightWheel.brakeTorque = baseBrakeForce();
    }
    
    //float baseBrakeForce()
    //{
    //    return isEngineRun ? 0 : engineResistance;
    //}
    IEnumerator RestartEngCorutne()
    {
        isStarting = true;
        engineWhel.brakeTorque = 0;
        frontLeftWheel.brakeTorque = 0;
        frontRightWheel.brakeTorque = 0;
        if (currentGear == 1)
        {
            engineWhel.motorTorque = starterForce / 10;
        }
        else
        {
            frontLeftWheel.motorTorque = starterForce * gearRatios[currentGear];
            rearLeftWheel.motorTorque = starterForce * gearRatios[currentGear];
        }
        yield return new WaitForSeconds(Random.Range(0.25f, 0.5f));
        engineWhel.motorTorque = 0;
        frontLeftWheel.motorTorque = 0;
        rearLeftWheel.motorTorque = 0;
        isStarting = false;
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
        if (currentGear == 1) { isSwitchFromN = true; }
        currentGear = Mathf.Clamp(currentGear + i, 0, gearRatios.Length-1);
        SMTM.UpdateTransmission(currentGear);
        
    }
    private void FixedUpdate()
    {
        if (!isStarting)
        {

            HandleMotor();
        }
        //else if (!isStarting && (engineWhel.motorTorque != 0 || frontLeftWheel.motorTorque != 0 || frontRightWheel.motorTorque != 0))
        //{
        //    BreakEngine();
        //    Debug.Log("BreakPrinuditelno");
        //}
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
   
    void HandleUI()
    {
        if (SMTM != null)
        {
            SMTM.UpdateValues(rb.velocity.magnitude, (isEngineRun || isStarting ? GetAverageWheelRPM() : 0) );
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
        // �������� ���� �� ������
        float throttle = Mathf.Clamp((isEngineRun? motorInput * PowerFromNRE.Evaluate(GetAverageWheelRPM()/MaxRPM): 0) , 0f, 1f) + (isEngineRun ? idleTorqleCoef : 0); // ������ ������
        float currentTorque = (engineTorque * gearRatios[currentGear]) * throttle;

        if (isSwitchFromN)
        {
            if (currentGear == 0 || currentGear == 2)
            {
                currentTorque += engineWhel.motorTorque * engineTorque;
                if (GetAverageWheelRPM() / MaxRPM > 0.15f)
                {
                    isSwitchFromN = false;
                }
            }
            else
            {
                isSwitchFromN = false;
            }
        }

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

        if (!isStarting && currentGear != 1 && !(isSwitchFromN && (currentGear == 0 || currentGear == 2)) &&  GetAverageWheelRPM() / MaxRPM < 0.1f) { BreakEngine(); }
        
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

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
    public bool isEngineRun= false;

    public int currentGear;
    [SerializeField] float[] gearRatios;
    [SerializeField] AnimationCurve PowerFromNRE;
    public float engineTorque;
    public int MaxRPM;

    public float engineResistance = 5f; // значение сопротивления

    public float starterForce;

    bool isStarting = false;
    [SerializeField] bool isSwitchFromN = false;
    [SerializeField] float idleTorqleCoef;
    const int flipTime = 6;
    float flipTimer = 6;

    [SerializeField] CarUIController SMTM;
    [Header ("Audio")]
    [SerializeField] AudioSource engineAudio;
    [SerializeField] float enginePitchMul;
    [SerializeField] float enginePitchPlus;

    Vector3 initPosition;

    public int completedLaps;
    public int checkPointIndex = 0;

    public bool isStartGame = false;

    private void Start()
    {
        initPosition = transform.position;
        if (SMTM != null)
        {
            SMTM.Init(125 / 3.6f, MaxRPM);
            SMTM.UpdateTransmission(currentGear);
        }
        GameController.Instance().cars.Add(this);
    }
    void FallToWater()
    {
        checkPointIndex = 0;
        transform.position = initPosition;
        transform.rotation = Quaternion.identity;
        BreakEngine();
        rb.velocity = Vector3.zero;

    }
    public void RestartEngine()
    {
        if (!isEngineRun && !isStarting)
        {
            StartCoroutine(RestartEngCorutne());
        }
    }
    public void BreakEngine()
    {
        isEngineRun = false;
        isSwitchFromN = false;
    }
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
            frontLeftWheel.motorTorque = starterForce * Mathf.Clamp( gearRatios[currentGear], -1, 1);
            frontLeftWheel.motorTorque = starterForce * Mathf.Clamp(gearRatios[currentGear], -1, 1);
        }
        yield return new WaitForSeconds(Random.Range(0.4f, 0.75f));
        engineWhel.motorTorque = 0;
        frontLeftWheel.motorTorque = 0;
        frontLeftWheel.motorTorque = 0;
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
        if (!isStartGame) { return; }
        if (currentGear == 1) { isSwitchFromN = true; }
        currentGear = Mathf.Clamp(currentGear + i, 0, gearRatios.Length-1);
        if (SMTM != null)
        {
            SMTM.UpdateTransmission(currentGear);
        }
        
    }
    public void SetTransmission(int i)
    {
        if (!isStartGame) { return; }
        if (currentGear == 1) { isSwitchFromN = true; }
        currentGear = Mathf.Clamp(i, 0, gearRatios.Length - 1);
        if (SMTM != null)
        {
            SMTM.UpdateTransmission(currentGear);
        }

    }
    private void Update()
    {
        if (transform.position.y < GameController.Instance().MinY())
        {
            FallToWater();
        }
    }
    private void FixedUpdate()
    {
        if (!isStarting)
        {

            HandleMotor();
        }
        HandleSteering();
        UpdateWheelModels();
        HandleUI();
        HandleFlip();
        HandleAudio();
    }
    void HandleFlip()
    {
        if (Mathf.Abs(Mathf.Abs(transform.rotation.eulerAngles.x) - 180) < 10 || Mathf.Abs(Mathf.Abs(transform.rotation.eulerAngles.z) - 180) < 10)
        {
            flipTimer -= Time.fixedDeltaTime;
        }
        else
        {
            flipTimer = flipTime;
        }
        if (flipTimer <= 0)
        {
            FlipOnWheels();
        }
    }
   
    public float GetAverageWheelRPM()
    {
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
        // Передача силы на колеса
        float throttle = Mathf.Clamp((isEngineRun? motorInput * PowerFromNRE.Evaluate(GetAverageWheelRPM()/MaxRPM): 0) , 0f, 1f) + (isEngineRun ? idleTorqleCoef : 0); // Только вперед
        float currentTorque = (engineTorque * gearRatios[currentGear]) * throttle;

        if (isSwitchFromN )
        {
            if ((currentGear == 0 || currentGear == 2) && isEngineRun)
            {
                currentTorque += Mathf.Min(100, Mathf.Abs(engineWhel.motorTorque)) * engineTorque * Mathf.Clamp(currentGear-1,-1,1);
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
            engineWhel.motorTorque -= (engineWhel.rpm / MaxRPM) * engineResistance;
        }
        else
        {
            frontLeftWheel.motorTorque = currentTorque;
            frontLeftWheel.motorTorque -= (frontLeftWheel.rpm / MaxRPM) * engineResistance * Mathf.Abs(currentGear - 1) ;
            frontRightWheel.motorTorque = currentTorque;
            frontRightWheel.motorTorque -= (frontRightWheel.rpm / MaxRPM) * engineResistance * Mathf.Abs(currentGear - 1) ;
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

    public void FlipOnWheels()
    {
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
    public void HandleAudio()
    {
        if (isEngineRun || isStarting)
        {
            if (!engineAudio.isPlaying) { engineAudio.Play(); }
            float pitch = enginePitchPlus + (GetAverageWheelRPM() / MaxRPM) * enginePitchMul;
            engineAudio.pitch = pitch;
            engineAudio.volume = pitch;
        }
        else
        {
            if (engineAudio.isPlaying) { engineAudio.Stop(); }
        }
    }
}

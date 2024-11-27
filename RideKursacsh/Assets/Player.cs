using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Car car;
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform cameraTarget;
    [SerializeField] float camSpeed;
    [SerializeField] float camRotSpeed;

    [SerializeField] Text completedLapText;
    [SerializeField] Slider lapProgressSlider;
    List<Vector3> checkPoints;
    void Start()
    {
        if (car == null) 
        {
            Debug.LogError("Player have not a car");
            Destroy(gameObject); 
        }
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
       
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        
        HanleCamera();
    }
    void Update()
    {
        float vert = Input.GetAxisRaw("Vertical");
        car.ChangeGas(vert);
        float hor = Input.GetAxis("Horizontal");
        car.RotateWhell(hor);
        if (Input.GetKeyDown(KeyCode.Q)) { car.ChangeTransmission(-1); }
        if (Input.GetKeyDown(KeyCode.E)) { car.ChangeTransmission(1); }
        if (Input.GetKeyDown(KeyCode.R)) { car.RestartEngine(); }
        if (Input.GetKeyDown(KeyCode.K)) { car.BreakEngine(); }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            car.HandleBreak(true);
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            car.HandleBreak(false);
        }
        HandlePlayerUI();
    }
    public void HanleCamera()
    {
        cameraTransform.position = Vector3.Lerp(cameraTransform.position,new Vector3(cameraTarget.position.x,Mathf.Max(0.5f,cameraTarget.position.y), cameraTarget.position.z), camSpeed);
        cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, cameraTarget.rotation, camRotSpeed);
    }
    void HandlePlayerUI()
    {
        completedLapText.text = car.completedLaps.ToString();
        float progressValue = GameController.Instance().CalcDistanceFromStart(car) / GameController.Instance().allLapDistance;
        lapProgressSlider.value = progressValue;
    }
    
}

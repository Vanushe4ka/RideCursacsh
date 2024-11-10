using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] Car car;
    void Start()
    {
        if (car == null) 
        {
            Debug.LogError("Player have not a car");
            Destroy(gameObject); 
        }
    }

    // Update is called once per frame
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
    }
}

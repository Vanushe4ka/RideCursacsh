using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AILearner : MonoBehaviour
{
    [SerializeField] AIController AIPrefab;
    [SerializeField] Transform startPoint;
    [SerializeField] int AICount;
    [SerializeField] AIController[] aiControllers;
    [SerializeField] Vector2 maxSpeedRange;
    [SerializeField] Vector2 turnSensitivityRange;
    [SerializeField] Vector2 slowDownRadiusRange;
    bool isLearn = true;
    void Start()
    {
        aiControllers = new AIController[AICount];
        for (int i = 0; i < AICount; i++)
        {
            aiControllers[i] = Instantiate(AIPrefab, startPoint.position, Quaternion.identity).GetComponent<AIController>();
            aiControllers[i].SetValues(Random.Range(maxSpeedRange.x, maxSpeedRange.y), Random.Range(turnSensitivityRange.x, turnSensitivityRange.y), Random.Range(slowDownRadiusRange.x, slowDownRadiusRange.y));
        }
        for (int i = 0; i < AICount; i++)
        {
            //GameController.Instance().cars.Add(aiControllers[i].car);
            aiControllers[i].enabled = true;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (isLearn)
        {
            for (int i = 0; i < aiControllers.Length; i++)
            {
                if (aiControllers[i].car.completedLaps == 3)
                {
                    StopLearning();
                    break;
                }
            }
        }
    }
    void StopLearning()
    {
        isLearn = false;
        for (int i = 0; i < aiControllers.Length; i++)
        {
            aiControllers[i].enabled = false;
            aiControllers[i].car.BreakEngine();
        }
    }
}

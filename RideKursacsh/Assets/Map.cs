using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Vector3> playerCheckPoints;
    public float maxDistFromCheckPointToCar;
    public List<Vector3> AIPath;
    public float maxDistFromAIPathPointToCar;
    public Terrain terrain;
}

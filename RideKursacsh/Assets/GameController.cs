using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update
    static GameController _instance;
    [SerializeField] float minY;
    [SerializeField] Map currentMap;
    public List<Car> cars;
    [SerializeField] bool drawAIPath;
    [SerializeField] Text countdownText;
    Terrain terrain;
    TerrainData terrainData;
    public float MinY()
    {
        return minY;
    }
    public List<Vector3> CheckPoints()
    {
        return currentMap.playerCheckPoints;
    }
    public List<Vector3> CheckAIPath()
    {
        return currentMap.AIPath;
    }
    public float maxDistanceForAIPointToCar()
    {
        return currentMap.maxDistFromAIPathPointToCar;
    }
    private void OnDrawGizmos()
    {
        
        if (currentMap != null)
        {
            if (drawAIPath)
            {
                if (currentMap.AIPath != null && currentMap.AIPath.Count > 2)
                {
                    Gizmos.color = Color.green;
                    for (int i = 0; i < currentMap.AIPath.Count; i++)
                    {
                        Vector3 currentPoint = currentMap.AIPath[i];
                        Vector3 nextPoint = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count]; // ��������� ����� (� ������ ���������)
                        Gizmos.DrawSphere(currentPoint, currentMap.maxDistFromAIPathPointToCar);
                        Gizmos.DrawLine(currentPoint, nextPoint);
                    }
                }
            }
            else
            {
                if (currentMap.playerCheckPoints != null && currentMap.playerCheckPoints.Count > 2)
                {
                    Gizmos.color = Color.red;
                    for (int i = 0; i < currentMap.playerCheckPoints.Count; i++)
                    {
                        Vector3 currentPoint = currentMap.playerCheckPoints[i];
                        Vector3 nextPoint = currentMap.playerCheckPoints[(i + 1) % currentMap.playerCheckPoints.Count]; // ��������� ����� (� ������ ���������)
                        Gizmos.DrawSphere(currentPoint, currentMap.maxDistFromCheckPointToCar);
                        Gizmos.DrawLine(currentPoint, nextPoint);
                    }
                }
            }
            
        }
        
    }
    public static GameController Instance()
    {
        return _instance;
    }
    void Awake()
    {
        if (_instance != null && this != _instance)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {
        terrain = currentMap.terrain;
        terrainData = terrain.terrainData;
        StartCoroutine(StartCountdown());
    }
    // Update is called once per frame
    public IEnumerator StartCountdown()
    {
        Vector3 startScale = Vector3.zero; // ��������� ������ (�������)
        Vector3 endScale = Vector3.one;   // �������� ������ (�� ��������� 1,1,1)
        float scaleDuration = 0.25f;

        string[] countdownMessages = { "3", "2", "1", "GO!" };

        foreach (string message in countdownMessages)
        {
            // ���������� �����
            countdownText.text = message;

            // ����� �������� � ����
            countdownText.transform.localScale = startScale;

            // �������� ���������� ��������
            float elapsedTime = 0f;
            while (elapsedTime < scaleDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleDuration;
                countdownText.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            // ���������, ��� ������� ���������� �����
            countdownText.transform.localScale = endScale;

            // �������� ����� ��������� ��������� (1 �������)
            yield return new WaitForSeconds(1f);
        }

        // �������� ����� ����� ���������� �������
        countdownText.text = "";
        StartGameForCars();
    }
    void StartGameForCars()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            cars[i].isStartGame = true;
        }
    }
    void Update()
    {
        CarsCheckPointsHandle();
    }
    void CarsCheckPointsHandle()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            if (Vector3.Distance(cars[i].transform.position,currentMap.playerCheckPoints[cars[i].checkPointIndex]) <= currentMap.maxDistFromCheckPointToCar)
            {
                cars[i].checkPointIndex++;
                if (cars[i].checkPointIndex >= currentMap.playerCheckPoints.Count)
                {
                    cars[i].checkPointIndex = 0;
                    cars[i].completedLaps++;
                }
            }
        }
    }
    public int GetTextureIndexAtPoint(Vector3 worldPosition)
    {
        
        Vector3 terrainPosition = worldPosition - terrain.transform.position;

        // ��������� ��������������� ���������� (�� 0 �� 1) ������ ��������
        float normalizedX = Mathf.Clamp01(terrainPosition.x / terrainData.size.x);
        float normalizedZ = Mathf.Clamp01(terrainPosition.z / terrainData.size.z);

        // ����������� ��������������� ���������� � ���������� ������� �����-�������
        int mapX = Mathf.FloorToInt(normalizedX * terrainData.alphamapWidth);
        int mapZ = Mathf.FloorToInt(normalizedZ * terrainData.alphamapHeight);

        // �������� ���� ������� � �����
        float[,,] alphaMaps = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        // ������� ������ �������� � ������������ �����
        int maxIndex = 0;
        float maxWeight = 0f;

        for (int i = 0; i < alphaMaps.GetLength(2); i++)
        {
            if (alphaMaps[0, 0, i] > maxWeight)
            {
                maxWeight = alphaMaps[0, 0, i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}

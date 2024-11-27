using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

using UnityEngine.SceneManagement;

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

    public bool isGameStarted = false;
    [SerializeField] float cameraSpeed;
    [SerializeField] float cameraRotSpeed;
    [SerializeField] float cameraHeigth;

    private List<float> segmentLengths;
    private float totalPathLength;
    private Dictionary<int, List<Vector2>> arcLengthTable = new Dictionary<int, List<Vector2>>();
    private const int samplesPerSegment = 100; // Количество выборок на сегмент
    private float normalizedProgress = 0f; // Нормализованный прогресс по всему пути
    [SerializeField] float lookAheadDistance;

    [SerializeField] Player player;
    [SerializeField] AIController[] aIControllers;

    public int completedLapForWin = 3;
    [SerializeField] Text completedLapsText;
    [SerializeField] Text endTable;
    [SerializeField] CanvasGroup endBurronsCanvasGroup;
    [SerializeField] GameObject playerUI;

    [SerializeField] GameObject pausePanel;
    bool isPaused = false;

    public float allLapDistance { get; private set; }
    public static bool isCanUpdate{ get; private set; }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    public void StartTheGame()
    {
        isGameStarted = true;
        player.enabled = true;
        for (int i = 0; i < aIControllers.Length; i++)
        {
            aIControllers[i].enabled = true;
        }
        StartCoroutine(StartCountdown());
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
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
                        Vector3 nextPoint = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count]; // Следующая точка (с учетом замыкания)
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
                        Vector3 nextPoint = currentMap.playerCheckPoints[(i + 1) % currentMap.playerCheckPoints.Count]; // Следующая точка (с учетом замыкания)
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
            isCanUpdate = true;
            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {
        terrain = currentMap.terrain;
        terrainData = terrain.terrainData;
        CalculateSegmentLengths();
        PrecomputeArcLengthTables();
        for (int i = 0; i < currentMap.playerCheckPoints.Count - 1; i++)
        {
            allLapDistance += Vector3.Distance(currentMap.playerCheckPoints[i], currentMap.playerCheckPoints[i + 1]);
        }


    }
    public float CalcDistanceFromStart(Car car)
    {
        float dist = 0;
        for (int i = 0; i < car.checkPointIndex && i < currentMap.playerCheckPoints.Count - 1; i++)
        {
            dist += Vector3.Distance(currentMap.playerCheckPoints[i], currentMap.playerCheckPoints[i + 1]);
        }
        dist -= Vector3.Distance(car.transform.position, currentMap.playerCheckPoints[car.checkPointIndex]);
        return dist;
    }
    float CalcLapProgressForCar(Car car)
    {
        return CalcDistanceFromStart(car) / allLapDistance;
    }
    // Update is called once per frame
    public IEnumerator StartCountdown()
    {
        Vector3 startScale = Vector3.zero; // Начальный размер (нулевой)
        Vector3 endScale = Vector3.one;   // Конечный размер (по умолчанию 1,1,1)
        float scaleDuration = 0.25f;

        string[] countdownMessages = { "3", "2", "1", "GO!" };

        foreach (string message in countdownMessages)
        {
            // Установить текст
            countdownText.text = message;

            // Сброс масштаба к нулю
            countdownText.transform.localScale = startScale;

            // Анимация увеличения масштаба
            float elapsedTime = 0f;
            while (elapsedTime < scaleDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / scaleDuration;
                countdownText.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            // Убедиться, что масштаб установлен точно
            countdownText.transform.localScale = endScale;

            // Ожидание перед следующей итерацией (1 секунда)
            yield return new WaitForSeconds(1f);
        }

        // Очистить текст после завершения отсчета
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
        if (!isCanUpdate) { return; }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
        if (isGameStarted)
        {
            CarsCheckPointsHandle();
            UpdateCopletedlapsText();
            CheckEndTheGame();
        }
        else
        {
            MoveCameraBeforeStartGame();
        }
    }
    void CheckEndTheGame()
    {
        if (player.car.completedLaps >= completedLapForWin)
        {
            EndTheGame();
        }
    }
    void MoveCameraBeforeStartGame()
    {
        if (currentMap.AIPath.Count < 2) return;

        // Увеличиваем нормализованный прогресс вдоль всего пути
        normalizedProgress += (cameraSpeed / totalPathLength) * Time.deltaTime;
        if (normalizedProgress > 1f)
            normalizedProgress -= 1f; // Зацикливание

        // Находим текущий сегмент и локальный `t` через таблицу длины дуги
        float segmentStartProgress = 0f;
        int currentSegmentIndex = 0;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segmentNormalizedLength = segmentLengths[i] / totalPathLength;
            if (normalizedProgress <= segmentStartProgress + segmentNormalizedLength)
            {
                currentSegmentIndex = i;
                float localProgress = (normalizedProgress - segmentStartProgress) / segmentNormalizedLength;

                // Получаем точное значение `t` через длину дуги
                float adjustedT = GetTFromArcLength(currentSegmentIndex, localProgress);

                // Определяем точки сплайна
                Vector3 p0 = currentMap.AIPath[(i - 1 + currentMap.AIPath.Count) % currentMap.AIPath.Count];
                Vector3 p1 = currentMap.AIPath[i];
                Vector3 p2 = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count];
                Vector3 p3 = currentMap.AIPath[(i + 2) % currentMap.AIPath.Count];

                // Добавляем высоту
                p0 += Vector3.up * cameraHeigth;
                p1 += Vector3.up * cameraHeigth;
                p2 += Vector3.up * cameraHeigth;
                p3 += Vector3.up * cameraHeigth;

                // Интерполяция позиции камеры с использованием Catmull-Rom
                Vector3 targetPosition = CatmullRom(p0, p1, p2, p3, adjustedT);

                // Вычисляем точку немного впереди для направления
                float lookAheadProgress = normalizedProgress + (cameraSpeed / totalPathLength) * lookAheadDistance;
                if (lookAheadProgress > 1f)
                    lookAheadProgress -= 1f;

                // Находим следующую точку для взгляда
                Vector3 lookAheadPosition = GetPositionOnCurve(lookAheadProgress);

                // Двигаем камеру
                Transform camTransform = Camera.main.transform;
                camTransform.position = targetPosition;

                // Плавно поворачиваем камеру к точке впереди
                Vector3 direction = (lookAheadPosition - targetPosition).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    camTransform.rotation = Quaternion.Slerp(camTransform.rotation, targetRotation, cameraRotSpeed * Time.deltaTime);
                }
                break;
            }
            segmentStartProgress += segmentNormalizedLength;
        }
    }

    // Получаем точку на кривой по нормализованному прогрессу
    Vector3 GetPositionOnCurve(float normalizedProgress)
    {
        float segmentStartProgress = 0f;

        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segmentNormalizedLength = segmentLengths[i] / totalPathLength;
            if (normalizedProgress <= segmentStartProgress + segmentNormalizedLength)
            {
                float localProgress = (normalizedProgress - segmentStartProgress) / segmentNormalizedLength;
                float adjustedT = GetTFromArcLength(i, localProgress);

                Vector3 p0 = currentMap.AIPath[(i - 1 + currentMap.AIPath.Count) % currentMap.AIPath.Count];
                Vector3 p1 = currentMap.AIPath[i];
                Vector3 p2 = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count];
                Vector3 p3 = currentMap.AIPath[(i + 2) % currentMap.AIPath.Count];


                return CatmullRom(p0, p1, p2, p3, adjustedT);
            }
            segmentStartProgress += segmentNormalizedLength;
        }

        // На случай ошибки возвращаем текущую позицию
        return Camera.main.transform.position;
    }

    void CalculateSegmentLengths()
    {
        segmentLengths = new List<float>();
        totalPathLength = 0f;

        for (int i = 0; i < currentMap.AIPath.Count; i++)
        {
            Vector3 p1 = currentMap.AIPath[i];
            Vector3 p2 = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count];
            float length = Vector3.Distance(p1, p2);
            segmentLengths.Add(length);
            totalPathLength += length;
        }
    }

    void PrecomputeArcLengthTables()
    {
        arcLengthTable.Clear();

        for (int i = 0; i < currentMap.AIPath.Count; i++)
        {
            Vector3 p0 = currentMap.AIPath[(i - 1 + currentMap.AIPath.Count) % currentMap.AIPath.Count];
            Vector3 p1 = currentMap.AIPath[i];
            Vector3 p2 = currentMap.AIPath[(i + 1) % currentMap.AIPath.Count];
            Vector3 p3 = currentMap.AIPath[(i + 2) % currentMap.AIPath.Count];

            List<Vector2> table = new List<Vector2>();
            float cumulativeLength = 0f;
            Vector3 previousPoint = p1;

            for (int j = 0; j <= samplesPerSegment; j++)
            {
                float t = j / (float)samplesPerSegment;
                Vector3 currentPoint = CatmullRom(p0, p1, p2, p3, t);

                if (j > 0)
                    cumulativeLength += Vector3.Distance(previousPoint, currentPoint);

                table.Add(new Vector2(t, cumulativeLength));
                previousPoint = currentPoint;
            }

            arcLengthTable[i] = table;
        }
    }

    float GetTFromArcLength(int segmentIndex, float progress)
    {
        if (!arcLengthTable.ContainsKey(segmentIndex)) return progress;

        List<Vector2> table = arcLengthTable[segmentIndex];
        float targetLength = progress * table[^1].y;

        for (int i = 0; i < table.Count - 1; i++)
        {
            if (table[i].y <= targetLength && table[i + 1].y >= targetLength)
            {
                float t1 = table[i].x;
                float t2 = table[i + 1].x;
                float l1 = table[i].y;
                float l2 = table[i + 1].y;

                return Mathf.Lerp(t1, t2, (targetLength - l1) / (l2 - l1));
            }
        }
        return 1f; // В случае ошибки возвращаем 1
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
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
    void UpdateCopletedlapsText()
    {
        cars.Sort((car1, car2) =>
        {
            // Сравнение по completedLaps
            int lapComparison = car2.completedLaps.CompareTo(car1.completedLaps); // Обратный порядок

            // Если completedLaps равны, используем CalcLapProgressForCar
            if (lapComparison != 0) return lapComparison;

            // Сравнение по результату CalcLapProgressForCar
            float progress1 = CalcLapProgressForCar(car1);
            float progress2 = CalcLapProgressForCar(car2);

            return progress2.CompareTo(progress1); // Обратный порядок
        });
        completedLapsText.text = "";
        for (int i = 0; i < cars.Count; i++)
        {
            completedLapsText.text += $"{i+1}: {cars[i].Name}\n";
        }
    }
    public int GetTextureIndexAtPoint(Vector3 worldPosition)
    {
        
        Vector3 terrainPosition = worldPosition - terrain.transform.position;

        // Вычисляем нормализованные координаты (от 0 до 1) внутри террейна
        float normalizedX = Mathf.Clamp01(terrainPosition.x / terrainData.size.x);
        float normalizedZ = Mathf.Clamp01(terrainPosition.z / terrainData.size.z);

        // Преобразуем нормализованные координаты в координаты массива альфа-каналов
        int mapX = Mathf.FloorToInt(normalizedX * terrainData.alphamapWidth);
        int mapZ = Mathf.FloorToInt(normalizedZ * terrainData.alphamapHeight);

        // Получаем веса текстур в точке
        float[,,] alphaMaps = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        // Находим индекс текстуры с максимальным весом
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
    void EndTheGame()
    {
        normalizedProgress = 0;
        isGameStarted = false;
        player.enabled = false;
        playerUI.SetActive(false);
        for (int i = 0; i < aIControllers.Length; i++)
        {
            aIControllers[i].enabled = false;
        }
        for (int i = 0; i < cars.Count; i++)
        {
            cars[i].BreakEngine();
        }
        StartCoroutine( MoveEndTable());
    }

    IEnumerator MoveEndTable()
    {
        string endText = "Таблица победителей\n\n" + completedLapsText.text;
        completedLapsText.gameObject.SetActive(false);
        endTable.gameObject.SetActive(true);
        endTable.text = "";
        float time = 1f;
        float timer = 0;
        endBurronsCanvasGroup.alpha = 0;
        endBurronsCanvasGroup.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        while (timer < time)
        {
            timer += Time.deltaTime;
            float t = timer / time;
            endTable.text = endText.Substring(0, (int)(endText.Length * t));

            endBurronsCanvasGroup.alpha = t;
            yield return null;
        }
        endTable.text = endText;
    }
    public void Replay()
    {
        isGameStarted = false;
        player.enabled = false;
        playerUI.SetActive(false);
        for (int i = 0; i < aIControllers.Length; i++)
        {
            aIControllers[i].enabled = false;
        }
        for (int i = 0; i < cars.Count; i++)
        {
            cars[i].BreakEngine();
        }
        StartCoroutine(ReplayCorutine());
    }
    public IEnumerator ReplayCorutine()
    {
        Time.timeScale = 1;
        isCanUpdate = false;
        pausePanel.SetActive(false);
        player.enabled = false;
        for (int i = 0; i < cars.Count; i++)
        {
            Destroy(cars[i]);
        }
        Quaternion cameraRoot = Camera.main.transform.rotation;
        _instance = null;
        SceneManager.LoadSceneAsync(0);
        while(_instance == null)
        {
            yield return null;
        }
        if (!isGameStarted)
        {
            Camera.main.transform.rotation = cameraRoot;
            _instance.normalizedProgress = normalizedProgress;
        }
        
        Destroy(gameObject);
    }
    public void Pause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1;
            if (isGameStarted) 
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        pausePanel.SetActive(isPaused);
    }
}

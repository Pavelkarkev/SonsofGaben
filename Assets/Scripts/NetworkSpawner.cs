using UnityEngine;
using Unity.Netcode;
using System.Text;
using System.Collections.Generic;

public class NetworkFlexibleSpawner : MonoBehaviour
{
    public static NetworkFlexibleSpawner Instance { get; private set; }

    [Header("Префабы Персонажей")]
    [SerializeField] private GameObject killerPrefab;
    [SerializeField] private GameObject survivorPrefab;

    [Header("Точки Спавна Фракций")]
    [SerializeField] private Transform killerSpawnPoint;       // Одна точка для маньяка
    [SerializeField] private Transform[] survivorSpawnPoints;   // Массив точек для выживших

    [Header("Статические Префабы Карты")]
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private Vector3[] doorSpawnPositions;
    [SerializeField] private GameObject generatorPrefab;
    [SerializeField] private Vector3[] generatorSpawnPositions;

    // Серверная таблица: ClientId -> Выбранная роль ("Killer" или "Survivor")
    private Dictionary<ulong, string> serverClientRoles = new Dictionary<ulong, string>();

    // Серверный список индексов точек спавна выживших, которые уже заняты
    private List<int> usedSurvivorSpawnIndices = new List<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Перехватываем одобрение подключения на сервере
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // Если это Хост, он спавнит двери и генераторы один раз при старте
            if (NetworkManager.Singleton.IsServer)
            {
                SpawnStaticObjects();
            }
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false; // Отключаем автоспавн дефолтных префабов Netcode
    }

    private void OnClientConnected(ulong clientId)
    {
        // Ждем, пока клиент выберет роль через GameplayLobbyManager.
        // Сам спавн теперь будет вызываться из лобби-менеджера по команде.
        Debug.Log($"[Спавнер] Игрок {clientId} подключился к сцене. Ожидание выбора роли...");
    }

    /// <summary>
    /// Этот метод будет вызывать сервер (Хост), когда игрок окончательно определился с ролью в лобби
    /// </summary>
    public void SpawnPlayerWithRole(ulong clientId, string role)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Сохраняем роль в таблицу
        serverClientRoles[clientId] = role;

        GameObject prefabToSpawn = (role == "Killer") ? killerPrefab : survivorPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[Спавнер] Префаб для роли {role} не привязан в инспекторе!");
            return;
        }

        Vector3 spawnPosition = Vector3.zero;
        Quaternion spawnRotation = Quaternion.identity;

        if (role == "Killer")
        {
            if (killerSpawnPoint != null)
            {
                spawnPosition = killerSpawnPoint.position;
                spawnRotation = killerSpawnPoint.rotation;
            }
            else
            {
                Debug.LogWarning("[Спавнер] Точка спавна Киллера не задана! Спавн в центре карты.");
            }
        }
        else // Для Выжившего
        {
            int selectedPointIndex = GetAvailableSurvivorSpawnIndex();
            if (selectedPointIndex != -1 && selectedPointIndex < survivorSpawnPoints.Length)
            {
                spawnPosition = survivorSpawnPoints[selectedPointIndex].position;
                spawnRotation = survivorSpawnPoints[selectedPointIndex].rotation;
                usedSurvivorSpawnIndices.Add(selectedPointIndex); // Блокируем точку
                Debug.Log($"[Спавнер] Точка спавна #{selectedPointIndex} успешно заблокирована за игроком {clientId}.");
            }
            else
            {
                Debug.LogError("[Спавнер] Нет доступных или свободных точек спавна для выжившего!");
                // Спавним в дефолтной первой точке, если всё совсем плохо
                if (survivorSpawnPoints.Length > 0) spawnPosition = survivorSpawnPoints[0].position;
            }
        }

        // Физический спавн объекта на сервере
        GameObject playerInstance = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
        if (role == "Killer") playerInstance.tag = "Killer";

        // Сетевая регистрация объекта в Netcode
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
            Debug.Log($"[УСПЕХ] Игрок {clientId} ({role}) успешно заспавнен на своей точке.");
        }
        else
        {
            Debug.LogError($"[ОШИБКА] На префабе {prefabToSpawn.name} отсутствует NetworkObject!");
        }
    }

    private int GetAvailableSurvivorSpawnIndex()
    {
        // Проходимся по всем точкам и ищем первую, которой нет в списке использованных
        for (int i = 0; i < survivorSpawnPoints.Length; i++)
        {
            if (!usedSurvivorSpawnIndices.Contains(i))
            {
                return i;
            }
        }
        return -1; // Свободных точек нет
    }

    private void SpawnStaticObjects()
    {
        if (doorPrefab != null && doorSpawnPositions != null)
        {
            foreach (Vector3 pos in doorSpawnPositions)
            {
                GameObject doorInstance = Instantiate(doorPrefab, pos, Quaternion.identity);
                doorInstance.GetComponent<NetworkObject>().Spawn();
            }
        }

        if (generatorPrefab != null && generatorSpawnPositions != null)
        {
            foreach (Vector3 pos in generatorSpawnPositions)
            {
                GameObject generatorInstance = Instantiate(generatorPrefab, pos, Quaternion.identity);
                generatorInstance.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            if (NetworkManager.Singleton.ConnectionApprovalCallback == ApprovalCheck)
            {
                NetworkManager.Singleton.ConnectionApprovalCallback = null;
            }
        }
    }
}
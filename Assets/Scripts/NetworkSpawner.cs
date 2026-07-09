using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;

public class NetworkFlexibleSpawner : MonoBehaviour
{
    [Header("UI Кнопки Выбора Роли")]
    [SerializeField] private Button selectKillerButton;
    [SerializeField] private Button selectSurvivorButton;

    [Header("Префабы")]
    [SerializeField] private GameObject killer_prefab;
    [SerializeField] private GameObject Survivor_prefab;

    [Header("Doors")]
    [SerializeField] private GameObject doorPrefab;
    [SerializeField] private Vector3[] doorSpawnPositions;

    [Header("Generators")]
    [SerializeField] private GameObject generatorPrefab;
    [SerializeField] private Vector3[] generatorSpawnPositions;

    // Серверная таблица для хранения соответствия: ClientId -> Выбранная роль
    private Dictionary<ulong, string> clientRolesTable = new Dictionary<ulong, string>();

    private void Start()
    {
        selectKillerButton.onClick.RemoveAllListeners();
        selectSurvivorButton.onClick.RemoveAllListeners();

        // Обе кнопки теперь ведут на универсальный метод подключения
        selectKillerButton.onClick.AddListener(() => ConnectWithRole("Killer"));
        selectSurvivorButton.onClick.AddListener(() => ConnectWithRole("Survivor"));

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void ConnectWithRole(string role)
    {
        Debug.Log($"[ВЫБОР] Выбрана роль: {role}. Подготовка сетевых данных...");

        // Кодируем роль в байты и записываем в ConnectionData клиента перед отправкой
        byte[] payload = Encoding.UTF8.GetBytes(role);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        // Проверяем, запущена ли уже сетевая сессия на этом ПК
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            // Мы первые, кто нажал кнопку в этой сессии -> Запускаем HOST
            Debug.Log($"[СПАВНЕР] Создание новой сессии. Запуск HOST для роли: {role}");
            NetworkManager.Singleton.StartHost();
            SpawnStaticObjects();
        }
        else
        {
            // Сессия уже активна (работает Хост в первом окне) -> Запускаем CLIENT
            Debug.Log($"[СПАВНЕР] Обнаружена активная сессия. Запуск CLIENT для роли: {role}");
            NetworkManager.Singleton.StartClient();
        }

        DeactivateMenu();
    }

    private void SpawnStaticObjects()
    {
        Debug.Log("[СЕРВЕР] Спавн статических объектов карты (двери, генераторы)...");

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

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Одобряем подключение входящему игроку
        response.Approved = true;

        // Отключаем автоматический спавн дефолтных префабов Netcode, чтобы управлять им вручную
        response.CreatePlayerObject = false;

        // Считываем полезную нагрузку (payload) с ролью от подключающегося клиента
        string clientRole = Encoding.UTF8.GetString(request.Payload);
        if (string.IsNullOrEmpty(clientRole)) clientRole = "Survivor"; // Подстраховка

        Debug.Log($"[APPROVAL] Игрок {request.ClientNetworkId} запросил роль: {clientRole}. Подключение одобрено.");

        // Сохраняем информацию о роли игрока в таблицу сервера
        if (!clientRolesTable.ContainsKey(request.ClientNetworkId))
        {
            clientRolesTable.Add(request.ClientNetworkId, clientRole);
        }
        else
        {
            clientRolesTable[request.ClientNetworkId] = clientRole;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Только Сервер/Хост имеет право спавнить сетевые объекты на сцене
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[СЕРВЕР] Игрок {clientId} успешно авторизован в сети. Начинаем создание персонажа...");

        // Пытаемся достать роль зашедшего игрока из серверной таблицы
        string roleToSpawn = "Survivor";
        if (clientRolesTable.ContainsKey(clientId))
        {
            roleToSpawn = clientRolesTable[clientId];
        }
        else
        {
            // Если хост подключается самым первым, он может проскочить ApprovalCheck локально.
            // В таком случае берем данные из его собственного ConnectionData
            byte[] localPayload = NetworkManager.Singleton.NetworkConfig.ConnectionData;
            if (localPayload != null && localPayload.Length > 0)
            {
                roleToSpawn = Encoding.UTF8.GetString(localPayload);
            }
        }

        Debug.Log($"[СЕРВЕР] Для Игрока {clientId} создается префаб роли: {roleToSpawn}");

        // Выбираем нужный префаб на основе роли
        GameObject prefabToSpawn = (roleToSpawn == "Killer") ? killer_prefab : Survivor_prefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[ОШИБКА] Префаб для роли {roleToSpawn} не привязан в инспекторе спавнера!");
            return;
        }

        // Спавним объект префаба на сервере
        GameObject playerInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

        if (roleToSpawn == "Killer") playerInstance.tag = "Killer";

        // Регистрируем объект в сети и передаем права владения (Ownership) конкретному clientId
        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnAsPlayerObject(clientId);
            Debug.Log($"[УСПЕХ] Игрок {clientId} ({roleToSpawn}) успешно заспавнен на сцене.");
        }
        else
        {
            Debug.LogError($"[ОШИБКА] На префабе {prefabToSpawn.name} отсутствует обязательный компонент NetworkObject!");
        }
    }

    private void DeactivateMenu()
    {
        if (selectKillerButton != null) selectKillerButton.gameObject.SetActive(false);
        if (selectSurvivorButton != null) selectSurvivorButton.gameObject.SetActive(false);
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
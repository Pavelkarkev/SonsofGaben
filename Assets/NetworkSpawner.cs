using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Text;

public class NetworkFlexibleSpawner : MonoBehaviour
{
    [Header("UI Кнопки Выбора Роли")]
    [SerializeField] private Button selectKillerButton;
    [SerializeField] private Button selectSurvivorButton;

    [Header("Префабы")]
    [SerializeField] private GameObject killer_prefab;
    [SerializeField] private GameObject Survivor_prefab;

    private string chosenRole = "Survivor";

    private void Start()
    {
        selectKillerButton.onClick.AddListener(() => SetRoleAndConnect("Killer"));
        selectSurvivorButton.onClick.AddListener(() => SetRoleAndConnect("Survivor"));

        // Регистрируем одобрение подключения на сервере
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void SetRoleAndConnect(string role)
    {
        chosenRole = role;

        // Упаковываем строку с ролью в байты, чтобы отправить серверу при коннекте
        byte[] payload = Encoding.UTF8.GetBytes(role);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        // В зависимости от того, первый ли мы игрок, запускаем Host или Client
        // Для локальных тестов: первый запускает Host, остальные Client
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            // Если сеть еще не запущена, проверяем: если мы хотим быть хостом
            // В реальной игре тут будет просто StartClient() для выделенного сервера,
            // но для тестов на одном ПК делаем проверку:
            if (GameObject.FindAnyObjectByType<NetworkObject>() == null)
            {
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
        }

        DeactivateMenu();
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Этот метод выполняется НА СЕРВЕРЕ, когда кто-то пытается войти
        response.Approved = true;
        response.CreatePlayerObject = false; // Отключаем автоспавн дефолтного игрока

        // Читаем, какую роль запросил клиент
        string clientRole = Encoding.UTF8.GetString(request.Payload);

        // Сохраняем информацию о роли (можно передать дальше в логику спавна)
        // Для простоты используем кастомный спавн в OnClientConnected
    }

    private void OnClientConnected(ulong clientId)
    {
        // Спавнить имеет право только сервер
        if (!NetworkManager.Singleton.IsServer) return;

        // Получаем payload подключившегося клиента
        byte[] payload = NetworkManager.Singleton.DisconnectReason == "" ?
            NetworkManager.Singleton.NetworkConfig.ConnectionData : null;

        // Если это сам Хост, берем его локально выбранную роль
        string roleToSpawn = chosenRole;

        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            var connectionData = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            // В реальном проекте payload достается из сессии, для теста определим по очереди:
            // Если маньяка еще нет — спавним маньяка, иначе выжившего (временный хак для теста)
            roleToSpawn = (GameObject.FindWithTag("Killer") == null && chosenRole == "Killer") ? "Killer" : "Survivor";
        }

        GameObject prefabToSpawn = (roleToSpawn == "Killer") ? killer_prefab : Survivor_prefab;

        GameObject playerInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

        // Важно: даем тег, чтобы сервер мог считать количество Маньяков на сцене
        if (roleToSpawn == "Killer") playerInstance.tag = "Killer";

        playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }

    private void DeactivateMenu()
    {
        selectKillerButton.gameObject.SetActive(false);
        selectSurvivorButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }
}
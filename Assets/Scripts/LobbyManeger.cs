using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("UI Buttons")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button chooseKillerButton;
    [SerializeField] private Button chooseSurvivorButton;

    // Словарь для хранения ролей игроков: Key = ClientId, Value = Роль (0 - не выбрана, 1 - Выживший, 2 - Киллер)
    private Dictionary<ulong, int> playerRoles = new Dictionary<ulong, int>();

    // Переменная для синхронизации ID игрока, который стал киллером (-1 значит место свободно)
    private NetworkVariable<long> killerClientId = new NetworkVariable<long>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Назначаем функции на кнопки главного меню
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);

        // Назначаем функции на кнопки выбора ролей
        chooseKillerButton.onClick.AddListener(() => RequestRoleServerRpc(2));
        chooseSurvivorButton.onClick.AddListener(() => RequestRoleServerRpc(1));

        // Следим за изменением переменной киллера, чтобы обновлять интерфейс (например, делать кнопку серой)
        killerClientId.OnValueChanged += OnKillerChanged;
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        HideConnectionUI();
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        HideConnectionUI();
    }

    private void HideConnectionUI()
    {
        hostButton.gameObject.SetActive(false);
        clientButton.gameObject.SetActive(false);
    }

    private void OnKillerChanged(long previousValue, long newValue)
    {
        // Если кто-то уже занял роль киллера (newValue != -1), выключаем кнопку для всех, у кого роль еще не выбрана
        if (newValue != -1)
        {
            chooseKillerButton.interactable = false;
            Debug.Log($"Роль Киллера занята игроком с ID: {newValue}");
        }
        else
        {
            chooseKillerButton.interactable = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRoleServerRpc(int requestedRole, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (requestedRole == 2) // Если игрок хочет быть Киллером
        {
            // Проверяем на сервере, свободна ли роль
            if (killerClientId.Value == -1)
            {
                killerClientId.Value = (long)clientId;
                playerRoles[clientId] = 2;
                TargetNotifyRoleResultClientRpc(true, 2, rpcParams.Receive.SenderClientId);
            }
            else
            {
                // Если уже занято, отправляем отказ конкретному клиенту
                TargetNotifyRoleResultClientRpc(false, 2, rpcParams.Receive.SenderClientId);
            }
        }
        else if (requestedRole == 1) // Если игрок хочет быть Выжившим
        {
            // Выживших может быть сколько угодно
            if (killerClientId.Value == (long)clientId)
            {
                // Если этот игрок раньше был киллером, а теперь передумал — освобождаем место киллера
                killerClientId.Value = -1;
            }
            playerRoles[clientId] = 1;
            TargetNotifyRoleResultClientRpc(true, 1, rpcParams.Receive.SenderClientId);
        }
    }

    [ClientRpc]
    private void TargetNotifyRoleResultClientRpc(bool success, int role, ulong targetClientId)
    {
        // Выполняется на каждом клиенте, но обрабатываем только для того, кто отправлял запрос
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        if (success)
        {
            string roleName = role == 2 ? "Киллер" : "Выживший";
            Debug.Log($"Успешно выбрана роль: {roleName}");

            // Здесь можно подсветить выбранную кнопку или заблокировать меню выбора
            chooseSurvivorButton.interactable = (role != 1);
        }
        else
        {
            Debug.LogWarning("Не удалось выбрать роль! Место Киллера уже занято другим игроком.");
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayLobbyManager : NetworkBehaviour
{
    public static GameplayLobbyManager Instance { get; private set; }

    [Header("Lobby UI Panel")]
    [SerializeField] private GameObject lobbyCanvasPanel; // Панель лобби, перекрывающая экран

    [Header("Role Buttons")]
    [SerializeField] private Button chooseKillerButton;
    [SerializeField] private Button chooseSurvivorButton;

    [Header("Ready System")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI countdownText;

    // Синхронизируемые сетевые переменные
    private NetworkVariable<long> killerClientId = new NetworkVariable<long>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> readyPlayersCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Dictionary<ulong, bool> serverReadyStates = new Dictionary<ulong, bool>();

    // Глобальный флаг: началась ли сама игра (пока false — все стоят на месте)
    public bool IsGameStarted { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        chooseKillerButton.onClick.AddListener(() => RequestRoleServerRpc(2));
        chooseSurvivorButton.onClick.AddListener(() => RequestRoleServerRpc(1));
        readyButton.onClick.AddListener(ToggleReadyState);

        killerClientId.OnValueChanged += OnKillerChanged;
        countdownText.gameObject.SetActive(false);

        // Лобби поверх экрана всегда активно при загрузке сцены
        lobbyCanvasPanel.SetActive(true);
    }

    private void OnKillerChanged(long previousValue, long newValue)
    {
        // Блокируем кнопку маньяка для всех, если кто-то его уже взял
        chooseKillerButton.interactable = (newValue == -1);
    }

    private void ToggleReadyState()
    {
        TextMeshProUGUI btnText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText.text == "Ready!")
        {
            btnText.text = "Not Ready";
            SetReadyServerRpc(false);
        }
        else
        {
            btnText.text = "Ready!";
            SetReadyServerRpc(true);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRoleServerRpc(int requestedRole, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (requestedRole == 2 && killerClientId.Value == -1)
        {
            killerClientId.Value = (long)clientId;

            // Передаем команду на спавн Киллера
            NetworkFlexibleSpawner.Instance.SpawnPlayerWithRole(clientId, "Killer");
        }
        else if (requestedRole == 1)
        {
            if (killerClientId.Value == (long)clientId) killerClientId.Value = -1;

            // Передаем команду на спавн Выжившего
            NetworkFlexibleSpawner.Instance.SpawnPlayerWithRole(clientId, "Survivor");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        serverReadyStates[clientId] = isReady;

        int currentReady = 0;
        foreach (var state in serverReadyStates.Values)
        {
            if (state) currentReady++;
        }
        readyPlayersCount.Value = currentReady;

        int totalPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        // Если все подключенные на данный момент игроки нажали готов
        if (currentReady == totalPlayers && totalPlayers > 0)
        {
            StartCoroutine(StartCountdownCoroutine());
        }
        else
        {
            StopAllCoroutines();
            HideCountdownClientRpc();
        }
    }

    private IEnumerator StartCountdownCoroutine()
    {
        int timer = 5;
        while (timer > 0)
        {
            UpdateCountdownClientRpc(timer);
            yield return new WaitForSeconds(1f);
            timer--;
        }
        StartMatchClientRpc();
    }

    [ClientRpc]
    private void UpdateCountdownClientRpc(int secondsRemaining)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = $"Матч начнется через: {secondsRemaining}";
    }

    [ClientRpc]
    private void HideCountdownClientRpc()
    {
        countdownText.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void StartMatchClientRpc()
    {
        IsGameStarted = true;
        countdownText.gameObject.SetActive(false);
        lobbyCanvasPanel.SetActive(false); // Убираем интерфейс выбора ролей, открывая чистую игру
        Debug.Log("[Лобби] Все готовы! Управление персонажами разблокировано.");
    }

    public override void OnDestroy()
    {
        if (killerClientId != null) killerClientId.OnValueChanged -= OnKillerChanged;
        base.OnDestroy();
    }
}
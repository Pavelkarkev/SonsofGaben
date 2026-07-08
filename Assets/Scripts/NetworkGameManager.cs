using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float matchDuration = 300f; // 5 минут
    [SerializeField] private int totalGeneratorsToWin = 3;

    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>();
    private NetworkVariable<int> repairedGeneratorsCount = new NetworkVariable<int>(0);
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);

    public float TimeRemaining => timeRemaining.Value;
    public int RepairedGenerators => repairedGeneratorsCount.Value;
    public int TotalGeneratorsNeeded => totalGeneratorsToWin;
    public bool IsGameActive => isGameActive.Value;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = matchDuration;
            isGameActive.Value = true;
        }
    }

    private void Update()
    {
        if (!IsServer || !isGameActive.Value) return;

        if (timeRemaining.Value > 0)
        {
            timeRemaining.Value -= Time.deltaTime;
        }
        else
        {
            timeRemaining.Value = 0;
            EndGame(false); // Время вышло -> Выжившие проиграли
        }
    }

    public void GeneratorRepaired()
    {
        if (!IsServer) return;

        repairedGeneratorsCount.Value++;

        if (repairedGeneratorsCount.Value >= totalGeneratorsToWin)
        {
            EndGame(true); // Все генераторы починены -> Выжившие победили
        }
    }

    private void EndGame(bool survivorsWon)
    {
        isGameActive.Value = false;

        if (survivorsWon)
        {
            Debug.Log("Выжившие починили генераторы и победили!");
        }
        else
        {
            Debug.Log("Время вышло! Маньяк победил!");
        }

        EndGameClientRpc(survivorsWon);
    }

    [ClientRpc]
    private void EndGameClientRpc(bool survivorsWon)
    {
        // Здесь позже заставим UI показывать экран победы/поражения
    }
}
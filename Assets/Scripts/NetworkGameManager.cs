using UnityEngine;
using Unity.Netcode;
using TMPro;

public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private float matchDuration = 300f; // 5 минут
    [SerializeField] private int totalGeneratorsToWin = 3; 

    [Header("UI Elements (Опционально)")]
    [SerializeField] private TextMeshProUGUI timerText;       // Сюда перетащи текст таймера
    [SerializeField] private TextMeshProUGUI objectiveText;   // Сюда текст задачи

    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(); 
    private NetworkVariable<int> repairedGeneratorsCount = new NetworkVariable<int>(0); 
    private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false); 

    // Новая переменная: запитаны ли уже ворота
    private NetworkVariable<bool> areGatesPowered = new NetworkVariable<bool>(false);

    public float TimeRemaining => timeRemaining.Value; 
    public int RepairedGenerators => repairedGeneratorsCount.Value; 
    public int TotalGeneratorsNeeded => totalGeneratorsToWin; 
    public bool IsGameActive => isGameActive.Value; 
    public bool AreGatesPowered => areGatesPowered.Value;

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
            areGatesPowered.Value = false;
        }

        // Подписываемся на обновление сетевых переменных, чтобы клиенты сразу видели изменения в UI
        repairedGeneratorsCount.OnValueChanged += OnStatsChanged;
        timeRemaining.OnValueChanged += OnTimerChanged;
        areGatesPowered.OnValueChanged += OnGatesStateChanged;

        // Первичный апдейт интерфейса при подключении
        UpdateObjectiveUI();
    }

    public override void OnNetworkDespawn()
    {
        if (repairedGeneratorsCount != null) repairedGeneratorsCount.OnValueChanged -= OnStatsChanged;
        if (timeRemaining != null) timeRemaining.OnValueChanged -= OnTimerChanged;
        if (areGatesPowered != null) areGatesPowered.OnValueChanged -= OnGatesStateChanged;
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

        if (areGatesPowered.Value) return; // Если уже всё починено, игнорируем лишние волны

        repairedGeneratorsCount.Value++; 

        if (repairedGeneratorsCount.Value >= totalGeneratorsToWin) 
        {
            areGatesPowered.Value = true;
            Debug.Log("[СЕРВЕР] Все генераторы заведены! Питание подано на сетевые шлюзы.");

            // Вместо мгновенного EndGame(true) даем выжившим фазу побега через двери
            NotifyGatesPoweredClientRpc();
        }
    }

    // Этот метод теперь будут вызывать триггеры Кнопки ворот или Зоны побега, когда Выживший успешно сбежит
    public void SurvivorEscaped()
    {
        if (!IsServer) return;

        // Для простоты: первый сбежавший завершает матч победой выживших.
        // (Позже сюда можно прикрутить проверку: если сбежали все оставшиеся в живых)
        EndGame(true);
    }

    private void EndGame(bool survivorsWon)
    {
        isGameActive.Value = false; 

        if (survivorsWon) 
        {
            Debug.Log("Выжившие победили!"); 
        }
        else 
        {
            Debug.Log("Время вышло или все мертвы! Маньяк победил!"); 
        }

        EndGameClientRpc(survivorsWon); 
    }

    [ClientRpc]
    private void EndGameClientRpc(bool survivorsWon)
    {
        if (objectiveText != null)
        {
            objectiveText.text = survivorsWon
                ? "<color=green>ПОБЕДА ВЫЖИВШИХ!</color>"
                : "<color=red>МАНЬЯК ОДЕРЖАЛ ПОБЕДУ!</color>";
        }

        // Блокируем управление или показываем панель конца игры
    }

    [ClientRpc]
    private void NotifyGatesPoweredClientRpc()
    {
        Debug.Log("[КЛИЕНТ] Внимание! Питание ворот восстановлено! Бегите к выходу!");
    }

    // Обновление UI Текста Задач при изменении счетчика генераторов
    private void OnStatsChanged(int prev, int next) => UpdateObjectiveUI();
    private void OnGatesStateChanged(bool prev, bool next) => UpdateObjectiveUI();

    private void UpdateObjectiveUI()
    {
        if (objectiveText == null) return;

        if (areGatesPowered.Value)
        {
            objectiveText.text = "<color=green>Шлюзы запитаны! Откройте двери и сбегите!</color>";
        }
        else
        {
            int left = totalGeneratorsToWin - repairedGeneratorsCount.Value;
            objectiveText.text = $"Осталось взломать терминалов: {left}";
        }
    }

    // Обновление UI Текста Таймера (вызывается автоматически при изменении времени на сервере)
    private void OnTimerChanged(float prev, float next)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(next / 60f);
        int seconds = Mathf.FloorToInt(next % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
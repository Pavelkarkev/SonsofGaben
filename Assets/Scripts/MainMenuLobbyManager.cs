using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Unity.Netcode;
using TMPro;

public class MainMenuAuthAndNetworkHandler : MonoBehaviour
{
    [Header("Network Core")]
    [SerializeField] private NetworkManager networkManager;

    [Header("Main Menu Trigger")]
    // Перетащи сюда свою кнопку "Play" из главного меню
    [SerializeField] private Button playButton;

    [Header("UI Panels")]
    [SerializeField] private GameObject connectionPanel; // Твоя панель с кнопками Host/Join
    [SerializeField] private TextMeshProUGUI statusText;   // Текст состояния

    [Header("Network Buttons")]
    [SerializeField] private Button createServerButton;
    [SerializeField] private Button joinServerButton;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    private string backendUrl = "http://127.0.0.1:8000/api/auth";
    private string credentialsFilePath;

    private string localUsername = "";
    private string localPassword = "";
    private string jwtToken = "";

    // Флаги состояния
    private bool isAuthenticated = false;
    private bool isPlayButtonClicked = false;

    private void Start()
    {
        credentialsFilePath = Path.Combine(Application.persistentDataPath, "user_profile.dat");

        // Вешаем слушатель на кнопку Play
        playButton.onClick.AddListener(OnPlayButtonPressed);

        createServerButton.onClick.AddListener(StartHostAndLoadGameplay);
        joinServerButton.onClick.AddListener(StartClientConnection);

        // ВАЖНО: Изначально ВСЁ скрыто. На экране только твоя кнопка Play
        connectionPanel.SetActive(false);
        statusText.gameObject.SetActive(false);

        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }

        // Запускаем авторизацию в тихом фоновом режиме, никому не мешая
        StartCoroutine(AutomaticAuthRoutine());
    }

    private void OnPlayButtonPressed()
    {
        isPlayButtonClicked = true;

        // Скрываем кнопку Play (или всё стартовое меню), так как процесс пошел
        playButton.gameObject.SetActive(false);

        if (isAuthenticated)
        {
            // Если бэкенд уже авторизовал нас к этому моменту — сразу открываем Host/Join
            connectionPanel.SetActive(true);
        }
        else
        {
            // Если бэкенд еще не ответил, пишем статус ожидания
            statusText.gameObject.SetActive(true);
            statusText.text = "Подключение к игровым серверам...";
        }
    }

    private IEnumerator AutomaticAuthRoutine()
    {
        if (File.Exists(credentialsFilePath))
        {
            LoadLocalCredentials();
            yield return StartCoroutine(SendAuthRequest("/login", false));
        }
        else
        {
            GenerateRandomCredentials();
            SaveLocalCredentials();
            yield return StartCoroutine(SendAuthRequest("/register", true));
        }
    }

    private void GenerateRandomCredentials()
    {
        int randomId = UnityEngine.Random.Range(1000, 9999);
        localUsername = $"Player_{randomId}";
        localPassword = Guid.NewGuid().ToString().Replace("-", "");
    }

    private void SaveLocalCredentials()
    {
        string data = $"{localUsername}\n{localPassword}";
        File.WriteAllText(credentialsFilePath, data, Encoding.UTF8);
    }

    private void LoadLocalCredentials()
    {
        string[] lines = File.ReadAllLines(credentialsFilePath, Encoding.UTF8);
        if (lines.Length >= 2)
        {
            localUsername = lines[0].Trim();
            localPassword = lines[1].Trim();
        }
    }

    private IEnumerator SendAuthRequest(string endpoint, bool isRegistration)
    {
        string jsonPayload = $"{{\"username\":\"{localUsername}\",\"password\":\"{localPassword}\"}}";

        using (UnityWebRequest request = new UnityWebRequest(backendUrl + endpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.downloadHandler.text.Contains("access_token"))
                {
                    jwtToken = request.downloadHandler.text.Split('"')[3];
                    Debug.Log($"[Бэкенд] Успешный вход профиля {localUsername}");

                    isAuthenticated = true;

                    // Если игрок К ЭТОМУ МОМЕНТУ уже нажал кнопку Play
                    if (isPlayButtonClicked)
                    {
                        statusText.text = $"Добро пожаловать, {localUsername}!";
                        yield return new WaitForSeconds(100f);
                        statusText.gameObject.SetActive(false);

                        // Показываем меню выбора сети
                        connectionPanel.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.LogError($"Ошибка бэкенда {endpoint}: " + request.downloadHandler.text);
                if (!isRegistration && File.Exists(credentialsFilePath))
                {
                    File.Delete(credentialsFilePath);
                }

                if (isPlayButtonClicked)
                {
                    statusText.gameObject.SetActive(true);
                    statusText.text = "<color=red>Ошибка авторизации! Проверьте сервер.</color>";
                }
            }
        }
    }

    private void StartHostAndLoadGameplay()
    {
        if (networkManager == null) return;

        if (networkManager.StartHost())
        {
            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.LoadScene(gameplaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }

    private void StartClientConnection()
    {
        if (networkManager == null) return;

        if (networkManager.StartClient())
        {
            connectionPanel.SetActive(false);
        }
    }
}
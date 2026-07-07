using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Text;
using UnityEngine.SceneManagement;

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
        selectKillerButton.onClick.RemoveAllListeners();
        selectSurvivorButton.onClick.RemoveAllListeners();

        selectKillerButton.onClick.AddListener(() => SetRoleAndConnect("Killer"));
        selectSurvivorButton.onClick.AddListener(() => SetRoleAndConnect("Survivor"));

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void SetRoleAndConnect(string role)
    {
        chosenRole = role;

        if (NetworkManager.Singleton == null) return;

        byte[] payload = Encoding.UTF8.GetBytes(role);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
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
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        string roleToSpawn = chosenRole;

        if (clientId != NetworkManager.Singleton.LocalClientId)
        {
            roleToSpawn = (GameObject.FindWithTag("Killer") == null && chosenRole == "Killer") ? "Killer" : "Survivor";
        }

        GameObject prefabToSpawn = (roleToSpawn == "Killer") ? killer_prefab : Survivor_prefab;

        if (prefabToSpawn == null) return;

        GameObject playerInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

        if (roleToSpawn == "Killer") playerInstance.tag = "Killer";

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.SpawnWithOwnership(clientId);
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
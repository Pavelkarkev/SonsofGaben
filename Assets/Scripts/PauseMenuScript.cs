using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuScript : MonoBehaviour
{
    [Header("UI Панель")]
    public GameObject pausePanel;

    [Header("Скрипт движения игрока")]
    public NetworkPlayerMovement playerMovementScript;

    private Rigidbody2D playerRb;
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        isPaused = false;

        if (playerMovementScript == null) TryFindLocalPlayer();

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;

            if (playerRb == null) playerRb = playerMovementScript.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

    void Pause()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        isPaused = true;

        if (playerMovementScript == null) TryFindLocalPlayer();

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;

            if (playerRb == null) playerRb = playerMovementScript.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.bodyType = RigidbodyType2D.Static;
            }
        }
    }

    private void TryFindLocalPlayer()
    {
        NetworkPlayerMovement[] players = FindObjectsByType<NetworkPlayerMovement>();
        foreach (NetworkPlayerMovement p in players)
        {
            if (p.IsOwner)
            {
                playerMovementScript = p;
                playerRb = p.GetComponent<Rigidbody2D>();
                break;
            }
        }
    }

    public void QuitToMenu()
    {
        if (playerMovementScript != null)
        {
            var rb = playerMovementScript.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
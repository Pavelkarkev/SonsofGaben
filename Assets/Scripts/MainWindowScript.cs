using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; 
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Звук")]
    public AudioMixer audioMixer; 

    [Header("Панели меню")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject hostgamepanel;
    [Header("Тексты кнопок (для изменения)")]
    public TextMeshProUGUI startText;
    public TextMeshProUGUI settingsText;

    public void SetVolume(float volume)
    {
        if (volume <= 0)
        {
            audioMixer.SetFloat("MasterVolume", -80f);
        }
        else
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        }
    }

    public void ClickStart()
    {
        mainMenuPanel.SetActive(false);
        hostgamepanel.SetActive(true);
    }
    public void ClickCloseHostGame()
    {
        mainMenuPanel.SetActive(true);
        hostgamepanel.SetActive(false);
    }
    public void ClickOpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public void ClickExit()
    {
        Debug.Log("Игрок нажал на Выход");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ClickCloseSettings()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }
    
}
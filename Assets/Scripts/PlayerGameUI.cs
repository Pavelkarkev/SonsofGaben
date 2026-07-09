using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private Image progressBar;
    [SerializeField] private GameObject interactionPanel;

    private PlayerInteraction playerInteraction;

    public void Initialize(PlayerInteraction interaction)
    {
        playerInteraction = interaction;
    }

    private void Update()
    {
        if (playerInteraction == null)
        {
            if (interactionPanel != null) interactionPanel.SetActive(false);
            return;
        }

        UpdateInteractionUI();
    }

    private void UpdateInteractionUI()
    {
        NetworkGenerator closestGenerator = playerInteraction.CurrentGenerator;

        if (closestGenerator != null && !closestGenerator.IsRepaired)
        {
            if (interactionPanel != null) interactionPanel.SetActive(true);
            if (interactionText != null) interactionText.text = "Press [E] to Hack Terminal";
            if (progressBar != null)
            {
                progressBar.fillAmount = closestGenerator.CurrentProgress / 100f;
            }
            return;
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}
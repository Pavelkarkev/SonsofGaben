using Unity.Netcode;
using UnityEngine;

public class NetworkGenerator : NetworkBehaviour
{
    [Header("Generator Settings")]
    [SerializeField] private float maxProgress = 100f;
    [SerializeField] private Sprite repairedSprite;

    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private NetworkVariable<float> currentProgress = new NetworkVariable<float>(0f);
    private NetworkVariable<bool> isRepaired = new NetworkVariable<bool>(false);

    public bool IsRepaired => isRepaired.Value;
    public float CurrentProgress => currentProgress.Value;

    public override void OnNetworkSpawn()
    {
        isRepaired.OnValueChanged += OnGeneratorStateChanged;
        if (isRepaired.Value)
        {
            UpdateVisuals();
        }
    }

    public override void OnNetworkDespawn()
    {
        isRepaired.OnValueChanged -= OnGeneratorStateChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddProgressServerRpc(float amount)
    {
        if (isRepaired.Value) return;

        currentProgress.Value = Mathf.Clamp(currentProgress.Value + amount, 0f, maxProgress);

        if (currentProgress.Value >= maxProgress)
        {
            isRepaired.Value = true;
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.GeneratorRepaired();
            }
        }
    }

    private void OnGeneratorStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null && repairedSprite != null)
        {
            spriteRenderer.sprite = repairedSprite;
        }
    }
}
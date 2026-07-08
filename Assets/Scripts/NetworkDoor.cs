using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class NetworkDoor : NetworkBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private float lockDuration = 15f;
    [SerializeField] private float breakDuration = 5f;

    [Header("Components")]
    [SerializeField] private Collider2D physicalCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ShadowCaster2D shadowCaster;

    [Header("Visuals (Optional)")]
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(false);
    private NetworkVariable<float> lastTimeOpened = new NetworkVariable<float>(-99f);
    private NetworkVariable<bool> isBeingBroken = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        isOpen.OnValueChanged += OnDoorStateChanged;
        UpdateDoorVisuals(isOpen.Value);
    }

    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnDoorStateChanged;
    }

    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        UpdateDoorVisuals(newValue);
    }

    private void UpdateDoorVisuals(bool open)
    {
        if (physicalCollider != null)
        {
            physicalCollider.enabled = !open;
        }

        if (spriteRenderer != null)
        {
            if (open && openSprite != null) spriteRenderer.sprite = openSprite;
            if (!open && closedSprite != null) spriteRenderer.sprite = closedSprite;

            Color c = spriteRenderer.color;
            c.a = open ? 0.3f : 1f;
            spriteRenderer.color = c;
        }
        if (shadowCaster != null)
        {
            shadowCaster.enabled = !open;
        }
    }

    public bool IsOpen => isOpen.Value;
    public bool IsBeingBroken => isBeingBroken.Value;
    public float BreakDuration => breakDuration;

    public bool IsLockedForKiller()
    {
        if (isOpen.Value) return false;
        return (Time.time - lastTimeOpened.Value) < lockDuration;
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractSurvivorServerRpc()
    {
        if (isBeingBroken.Value) return;

        isOpen.Value = !isOpen.Value;

        if (isOpen.Value)
        {
            lastTimeOpened.Value = Time.time;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartBreakingServerRpc()
    {
        if (isOpen.Value || !IsLockedForKiller() || isBeingBroken.Value) return;
        isBeingBroken.Value = true;
        StartCoroutine(BreakDoorRoutine());
    }

    [ServerRpc(RequireOwnership = false)]
    public void StopBreakingServerRpc()
    {
        if (!isBeingBroken.Value) return;
        StopAllCoroutines();
        isBeingBroken.Value = false;
    }

    private IEnumerator BreakDoorRoutine()
    {
        yield return new WaitForSeconds(breakDuration);
        isOpen.Value = true;
        isBeingBroken.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void InteractKillerNormalServerRpc()
    {
        if (isBeingBroken.Value || IsLockedForKiller()) return;
        isOpen.Value = !isOpen.Value;
    }
}
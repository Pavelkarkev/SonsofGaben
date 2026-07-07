using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask doorLayer;
    [SerializeField] private bool isKiller = false;

    private NetworkDoor currentDoor;
    private bool isHoldingInteract = false;
    private float holdTimer = 0f;

    private void Update()
    {
        if (!IsOwner) return;

        FindClosestDoor();

        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            OnInteractPressed();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            OnInteractReleased();
        }

        if (isHoldingInteract)
        {
            HandleInteractionHold();
        }
    }

    private void FindClosestDoor()
    {
        Collider2D[] doors = Physics2D.OverlapCircleAll(transform.position, interactionRadius, doorLayer);

        if (doors.Length == 0)
        {
            if (currentDoor != null && isHoldingInteract) OnInteractReleased();
            currentDoor = null;
            return;
        }

        float minDistance = float.MaxValue;
        NetworkDoor closestDoor = null;

        foreach (var col in doors)
        {
            if (col.TryGetComponent<NetworkDoor>(out var door))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestDoor = door;
                }
            }
        }

        if (currentDoor != closestDoor && isHoldingInteract)
        {
            OnInteractReleased();
        }

        currentDoor = closestDoor;
    }

    private void OnInteractPressed()
    {
        if (currentDoor == null) return;

        if (!isKiller)
        {
            currentDoor.InteractSurvivorServerRpc();
        }
        else
        {
            if (currentDoor.IsOpen)
            {
                currentDoor.InteractKillerNormalServerRpc();
            }
            else
            {
                if (currentDoor.IsLockedForKiller())
                {
                    isHoldingInteract = true;
                    holdTimer = 0f;
                    currentDoor.StartBreakingServerRpc();
                    Debug.Log("Маньяк начал вскрывать дверь...");
                }
                else
                {
                    currentDoor.InteractKillerNormalServerRpc();
                }
            }
        }
    }

    private void OnInteractReleased()
    {
        if (!isKiller || !isHoldingInteract) return;

        isHoldingInteract = false;
        holdTimer = 0f;

        if (currentDoor != null)
        {
            currentDoor.StopBreakingServerRpc();
            Debug.Log("Маньяк перестал вскрывать дверь.");
        }
    }

    private void HandleInteractionHold()
    {
        if (currentDoor == null || !currentDoor.IsBeingBroken)
        {
            isHoldingInteract = false;
            holdTimer = 0f;
            return;
        }

        holdTimer += Time.deltaTime;

        if (holdTimer >= currentDoor.BreakDuration)
        {
            isHoldingInteract = false;
            holdTimer = 0f;
            Debug.Log("Маньяк успешно выломал дверь!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
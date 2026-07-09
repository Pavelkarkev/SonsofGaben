using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask doorLayer;
    [SerializeField] private LayerMask generatorLayer;
    [SerializeField] private bool isKiller = false;

    private NetworkDoor currentDoor;
    private NetworkGenerator currentGenerator;
    private bool isHoldingInteract = false;
    private float holdTimer = 0f;
    private bool isInsideMinigame = false;

    public NetworkGenerator CurrentGenerator => currentGenerator;

    private void Update()
    {
        if (!IsOwner || isInsideMinigame) return;

        if (!isKiller)
        {
            FindClosestGenerator();
        }
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

    private void FindClosestGenerator()
    {
        Collider2D[] generators = Physics2D.OverlapCircleAll(transform.position, interactionRadius, generatorLayer);

        if (generators.Length == 0)
        {
            currentGenerator = null;
            return;
        }

        float minDistance = float.MaxValue;
        NetworkGenerator closestGen = null;

        foreach (var col in generators)
        {
            if (col.TryGetComponent<NetworkGenerator>(out var gen))
            {
                if (gen.IsRepaired) continue;
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestGen = gen;
                }
            }
        }

        currentGenerator = closestGen;
    }

    private void OnInteractPressed()
    {
        if (!isKiller)
        {
            if (currentGenerator != null)
            {
                StartGeneratorMinigame();
            }
            else if (currentDoor != null)
            {
                currentDoor.InteractSurvivorServerRpc();
            }
        }
        else
        {
            if (currentDoor == null) return;

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
                }
                else
                {
                    currentDoor.InteractKillerNormalServerRpc();
                }
            }
        }
    }

    private void StartGeneratorMinigame()
    {
        TerminalWorldMinigame minigameUI = FindAnyObjectByType<TerminalWorldMinigame>(FindObjectsInactive.Include);
        if (minigameUI != null)
        {
            isInsideMinigame = true;
            isHoldingInteract = false;

            var movement = GetComponent<NetworkPlayerMovement>();
            if (movement != null) movement.enabled = false;

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            minigameUI.gameObject.SetActive(true);
            minigameUI.StartMinigame(currentGenerator, () =>
            {
                isInsideMinigame = false;
                if (movement != null) movement.enabled = true;
            });
        }
    }

    private void OnInteractReleased()
    {
        if (isInsideMinigame) return;

        isHoldingInteract = false;
        holdTimer = 0f;

        if (isKiller && currentDoor != null)
        {
            currentDoor.StopBreakingServerRpc();
        }
    }

    private void HandleInteractionHold()
    {
        if (!isKiller) return;

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
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
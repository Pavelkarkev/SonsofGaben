using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SurvivorDash : NetworkBehaviour
{
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashCooldown = 4f;

    private Rigidbody2D rb;
    private NetworkPlayerMovement movementScript;
    private bool isDashing = false;
    private bool canDash = true;
    private float cooldownTimer = 0f;

    public float DashCooldown => dashCooldown;
    public float CurrentCooldownTimer => cooldownTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        movementScript = GetComponent<NetworkPlayerMovement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!canDash)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canDash = true;
            }
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && canDash && !isDashing)
        {
            Vector2 dashDirection = GetDashDirection();
            if (dashDirection != Vector2.zero)
            {
                StartCoroutine(PerformDash(dashDirection));
            }
        }
    }

    private Vector2 GetDashDirection()
    {
        if (movementScript != null)
        {
            Vector2 input = movementScript.GetInputVector();
            if (input != Vector2.zero) return input;
        }
        return transform.up;
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;
        cooldownTimer = dashCooldown;

        float elapsedTime = 0f;
        while (elapsedTime < dashDuration)
        {
            rb.linearVelocity = direction * dashSpeed;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    public bool IsMovementControlledByDash()
    {
        return isDashing;
    }
}
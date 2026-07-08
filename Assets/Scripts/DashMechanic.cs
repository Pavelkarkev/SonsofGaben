using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class SurvivorDash : NetworkBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float postDashSpeedMultiplier = 1.35f;
    [SerializeField] private float postDashDuration = 0.4f;
    [SerializeField] private float dashCooldown = 1.5f;

    private NetworkPlayerMovement movementScript;
    private bool isDashing;
    private bool isSlowingDown;
    private bool canDash = true;

    public float DashCooldown => dashCooldown;
    public float CurrentCooldownTimer { get; private set; } 

    private void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        movementScript = GetComponent<NetworkPlayerMovement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.shiftKey.wasPressedThisFrame && canDash && !isDashing)
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
        Vector2 currentInput = movementScript != null ? movementScript.GetInputVector() : Vector2.zero;

        if (currentInput != Vector2.zero)
        {
            return currentInput;
        }

        if (Mouse.current != null)
        {
            Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
            mouseScreenPosition.z = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            return ((Vector2)mouseWorldPosition - (Vector2)transform.position).normalized;
        }

        return Vector2.zero;
    }

    private IEnumerator PerformDash(Vector2 direction)
    {
        canDash = false;
        isDashing = true;

        rb.linearVelocity = direction * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        isSlowingDown = true;

        float currentSpeed = dashSpeed;
        float targetSpeed = movementScript != null ? rb.linearVelocity.magnitude * postDashSpeedMultiplier : dashSpeed * 0.4f;
        float elapsed = 0f;

        while (elapsed < postDashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / postDashDuration;

            currentSpeed = Mathf.Lerp(dashSpeed, targetSpeed, progress);
            Vector2 currentInput = movementScript != null ? movementScript.GetInputVector() : Vector2.zero;

            if (currentInput != Vector2.zero)
            {
                rb.linearVelocity = currentInput * currentSpeed;
            }
            else
            {
                rb.linearVelocity = direction * currentSpeed;
            }

            yield return null;
        }

        isSlowingDown = false;

        CurrentCooldownTimer = dashCooldown;
        while (CurrentCooldownTimer > 0f)
        {
            CurrentCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        CurrentCooldownTimer = 0f;

        canDash = true;
    }

    public bool IsMovementControlledByDash()
    {
        return isDashing || isSlowingDown;
    }
}
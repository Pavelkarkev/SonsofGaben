using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillerAttack : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private int damageAmount = 40;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackAngle = 70f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Layer Setup")]
    [SerializeField] private LayerMask survivorLayer;

    private Animator animator;
    private bool isCooldown = false;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isCooldown)
        {
            StartCoroutine(PerformAttackCooldown());
            TriggerAttackAnimation();
            RequestAttackServerRpc();
        }
    }

    private void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
    }

    private IEnumerator PerformAttackCooldown()
    {
        isCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isCooldown = false;
    }

    [ServerRpc]
    private void RequestAttackServerRpc()
    {
        Collider2D[] hitSurvivors = Physics2D.OverlapCircleAll(transform.position, attackRange, survivorLayer);
        Vector2 forwardDirection = transform.up;

        foreach (Collider2D survivor in hitSurvivors)
        {
            Vector2 directionToSurvivor = (survivor.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(forwardDirection, directionToSurvivor);

            if (angle <= attackAngle / 2f)
            {
                Debug.DrawLine(transform.position,survivor.transform.position, Color.white, 0.5f);
                Debug.Log($"<color=red>[УДАР]</color> Попадание по цели: {survivor.gameObject.name}!");
                if (survivor.TryGetComponent<Health>(out var health))
                {
                    health.TakeDamage(damageAmount);
                }
            }
        }
    }
    private void OnDrawGizmos()
    {

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 forward = transform.up;
        Vector3 leftBoundary = Quaternion.Euler(0, 0, attackAngle / 2f) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -attackAngle / 2f) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * attackRange);
    }
}
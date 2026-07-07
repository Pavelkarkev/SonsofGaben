using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} is dead.");
    }

    // ÑÄÅËÀËÈ ÌÅÒÎÄ PUBLIC È ÈÑÏÐÀÂÈËÈ .Value Ñ ÁÎËÜØÎÉ ÁÓÊÂÛ!
    public void Heal(int healValue)
    {
        if (!IsServer) return;

        // Åñëè õï óæå íà ìàêñèìóìå, âûõîäèì
        if (currentHealth.Value >= maxHealth)
        {
            return;
        }

        currentHealth.Value += healValue;

        if (currentHealth.Value > maxHealth)
        {
            currentHealth.Value = maxHealth;
        }
    }
}
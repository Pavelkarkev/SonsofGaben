using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class KillerSonarAbility : NetworkBehaviour
{
    [SerializeField] private float cooldownDuration = 25f;
    [SerializeField] private float revealDuration = 4f;

    private bool canUseSonar = true;
    private float cooldownTimer = 0f;
    private bool isSonarActive = false;
    private List<Transform> detectedSurvivors = new List<Transform>();

    public float CooldownDuration => cooldownDuration;
    public float CurrentCooldownTimer => cooldownTimer;
    public bool IsSonarActive => isSonarActive;
    public List<Transform> DetectedSurvivors => detectedSurvivors;

    public static event Action OnSonarActivatedGlobal;

    private void Update()
    {
        if (!IsOwner) return;

        if (!canUseSonar)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canUseSonar = true;
            }
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && canUseSonar)
        {
            RequestSonarServerRpc();
        }
    }

    [ServerRpc]
    private void RequestSonarServerRpc()
    {
        if (!canUseSonar) return;

        NotifySonarActivationClientRpc();
        StartCoroutine(ServerCooldownRoutine());
    }

    [ClientRpc]
    private void NotifySonarActivationClientRpc()
    {
        OnSonarActivatedGlobal?.Invoke();

        if (IsOwner)
        {
            StartCoroutine(LocalRevealRoutine());
        }
    }

    private IEnumerator ServerCooldownRoutine()
    {
        canUseSonar = false;
        yield return new WaitForSeconds(cooldownDuration);
    }

    private IEnumerator LocalRevealRoutine()
    {
        isSonarActive = true;
        cooldownTimer = cooldownDuration;

        FindSurvivors();

        yield return new WaitForSeconds(revealDuration);

        isSonarActive = false;
        detectedSurvivors.Clear();
    }

    private void FindSurvivors()
    {
        detectedSurvivors.Clear();
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject p in players)
        {
            if (p != gameObject && p.GetComponent<SurvivorDash>() != null)
            {
                detectedSurvivors.Add(p.transform);
            }
        }
    }
}
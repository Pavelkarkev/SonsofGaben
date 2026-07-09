using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TerminalWorldMinigame : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshPro terminalTextDisplay;
    [SerializeField] private TextMeshPro paperTextDisplay;

    [Header("Minigame Settings")]
    [SerializeField]
    private string[] commandPool = {
        "sudo systemctl restart power",
        "clear_cache --force",
        "init_generator_core",
        "override_security_protocol",
        "bypass_matrix_auth",
        "connect_grid_05",
        "sys.reboot_core",
        "net.allocate_ip"
    };
    [SerializeField] private int maxLines = 8;
    [SerializeField] private float progressRewardPerCommand = 25f;

    private NetworkGenerator targetGenerator;
    private Action onCompleteCallback;
    private bool isActive = false;

    private List<string> activeCommandsList = new List<string>();
    private int currentStepIndex = 0;

    private string currentInputBuffer = "";
    private List<string> terminalHistory = new List<string>();

    private const string TerminalPrefix = "user@sys_gen:~$ ";

    public void StartMinigame(NetworkGenerator generator, Action onComplete)
    {
        targetGenerator = generator;
        onCompleteCallback = onComplete;
        isActive = true;

        terminalHistory.Clear();
        currentInputBuffer = "";
        currentStepIndex = 0;

        terminalHistory.Add("Welcome to CoreOS v1.4.26...");
        terminalHistory.Add("Execute the sequential tasks from the manual.");
        terminalHistory.Add("");

        GeneratePaperSequence();
        UpdateTerminalDisplay();

        Keyboard.current.onTextInput += OnTextInputReceived;
    }

    private void Update()
    {
        if (!isActive || Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitMinigame();
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ExecuteCurrentInput();
            return;
        }

        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            if (currentInputBuffer.Length > 0)
            {
                currentInputBuffer = currentInputBuffer.Substring(0, currentInputBuffer.Length - 1);
                UpdateTerminalDisplay();
            }
        }
    }

    private void OnTextInputReceived(char ch)
    {
        if (!isActive) return;

        if (ch == '\r' || ch == '\n' || ch == '\b' || ch == '\x1b')
        {
            return;
        }

        currentInputBuffer += ch;
        UpdateTerminalDisplay();
    }

    private void GeneratePaperSequence()
    {
        activeCommandsList.Clear();
        List<string> poolCopy = new List<string>(commandPool);

        for (int i = 0; i < 4; i++)
        {
            if (poolCopy.Count == 0) break;
            int randomIndex = UnityEngine.Random.Range(0, poolCopy.Count);
            activeCommandsList.Add(poolCopy[randomIndex]);
            poolCopy.RemoveAt(randomIndex);
        }

        UpdatePaperDisplay();
    }

    private void UpdatePaperDisplay()
    {
        if (paperTextDisplay == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MANUAL SECTOR:");

        for (int i = 0; i < activeCommandsList.Count; i++)
        {
            if (i < currentStepIndex)
            {
                sb.AppendLine($"<color=black>[OK] {i + 1}. {activeCommandsList[i]}</color>");
            }
            else if (i == currentStepIndex)
            {
                sb.AppendLine($"<color=black>> {i + 1}. {activeCommandsList[i]} *</color>");
            }
            else
            {
                sb.AppendLine($"<color=black>{i + 1}. {activeCommandsList[i]}</color>");
            }
        }

        paperTextDisplay.text = sb.ToString();
    }

    private void ExecuteCurrentInput()
    {
        if (currentStepIndex >= activeCommandsList.Count) return;

        string fullCommandLine = TerminalPrefix + currentInputBuffer;
        terminalHistory.Add(fullCommandLine);

        string targetCommand = activeCommandsList[currentStepIndex];

        if (currentInputBuffer.Trim() == targetCommand.Trim())
        {
            terminalHistory.Add("<color=green>[SUCCESS] Operation verified.</color>");
            terminalHistory.Add("");

            if (targetGenerator != null)
            {
                targetGenerator.AddProgressServerRpc(progressRewardPerCommand);
            }

            currentStepIndex++;
            UpdatePaperDisplay();

            if (currentStepIndex >= activeCommandsList.Count)
            {
                terminalHistory.Add("<color=cyan>[SYSTEM] ALL CORE TASKS COMPLETED. GRID STABLE.</color>");
                UpdateTerminalDisplay();
                Invoke(nameof(ExitMinigame), 1.5f);
                return;
            }
        }
        else
        {
            terminalHistory.Add("<color=red>[ERROR] Command execution failed. Invalid token.</color>");
            terminalHistory.Add("");
        }

        currentInputBuffer = "";

        if (terminalHistory.Count > maxLines)
        {
            terminalHistory.RemoveRange(0, terminalHistory.Count - maxLines);
        }

        UpdateTerminalDisplay();
    }

    private void UpdateTerminalDisplay()
    {
        if (terminalTextDisplay == null) return;

        StringBuilder sb = new StringBuilder();
        foreach (string line in terminalHistory)
        {
            sb.AppendLine(line);
        }

        if (currentStepIndex < activeCommandsList.Count)
        {
            sb.Append(TerminalPrefix + currentInputBuffer + "<color=#00FF00>_</color>");
        }

        terminalTextDisplay.text = sb.ToString();
    }

    private void ExitMinigame()
    {
        isActive = false;
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInputReceived;
        }
        if (terminalTextDisplay != null) terminalTextDisplay.text = "TERMINAL STANDBY...";
        onCompleteCallback?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnTextInputReceived;
        }
    }
}
using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("2行のみ表示されるターゲットテキスト表示用")]
    public TextMeshProUGUI targetTextDisplay;
    public TMP_InputField inputField;
    [Tooltip("総入力数、精度、進行状況を表示するテキスト")] // Tooltip修正
    public TextMeshProUGUI statsTextDisplay;
    [Tooltip("残り時間を表示するテキスト")]
    public TextMeshProUGUI timerTextDisplay;

    [Header("Game Settings")]
    [TextArea(3, 5)]
    [Tooltip("改行を含む長いテキストを設定可能")]
    public string targetString = "";

    [Tooltip("制限時間（秒）。0の場合は無制限。")]
    public float timeLimitSeconds = 60f;

    // --- 内部変数 ---
    private float startTime;
    private bool isTypingStarted = false;
    private bool isTypingFinished = false;

    private float currentWPM = 0f; // WPM計算は内部的に残りますが表示はされません
    private int totalKeystrokes = 0;     // 総打鍵数
    private int incorrectKeystrokes = 0; // ミスタイプ数

    // 行管理用変数
    private List<string> targetLines = new List<string>();
    private int currentLineIndex = 0;
    private int indexInCurrentLine = 0;

    // 二重入力防止用
    private int lastInputFrame = -1;

    void Start()
    {
        if (targetTextDisplay != null)
        {
            targetTextDisplay.alignment = TextAlignmentOptions.TopLeft;
            targetTextDisplay.overflowMode = TextOverflowModes.Masking;
            targetTextDisplay.enableWordWrapping = false;
        }
        if (statsTextDisplay == null) Debug.LogWarning("[DEBUG] InputManager: statsTextDisplay がアサインされていません！");
        if (timerTextDisplay == null) Debug.LogWarning("[DEBUG] InputManager: timerTextDisplay がアサインされていません！");

        UpdateStatsUI();
        UpdateTimerUI(timeLimitSeconds);
    }

    void Update()
    {
        if (isTypingStarted && !isTypingFinished)
        {
            float timeElapsed = Time.time - startTime;

            if (timeLimitSeconds > 0 && timeElapsed >= timeLimitSeconds)
            {
                Debug.Log("[DEBUG] InputManager: 時間切れ！");
                FinishTyping(false);
                UpdateTimerUI(0f);
            }
            else
            {
                CalculateLiveWPM(timeElapsed); // WPM計算は継続（将来のため）
                UpdateTimerUI(timeLimitSeconds - timeElapsed);
            }
        }

        UpdateStatsUI();
    }

    public void SetTargetText(string newText)
    {
        targetString = newText;
        InitializeGame();
    }

    private void InitializeGame()
    {
        if (string.IsNullOrEmpty(targetString))
        {
            targetLines.Clear();
            UpdateStatsUI();
            UpdateTimerUI(timeLimitSeconds);
            if (targetTextDisplay != null) targetTextDisplay.text = "";
            return;
        }

        string normalizedText = targetString.Replace("\r\n", "\n").Replace('\r', '\n');
        targetLines = new List<string>(normalizedText.Split(new[] { '\n' }, StringSplitOptions.None));

        currentLineIndex = 0;
        indexInCurrentLine = 0;

        if (inputField != null) inputField.text = "";

        totalKeystrokes = 0;
        incorrectKeystrokes = 0;
        currentWPM = 0f;
        isTypingStarted = false;
        isTypingFinished = false;
        lastInputFrame = -1;
        UpdateTargetTextHighlight();
        UpdateStatsUI();
        UpdateTimerUI(timeLimitSeconds);
    }

    public void ProcessInput(char inputChar)
    {
        if (Time.frameCount == lastInputFrame) return;
        lastInputFrame = Time.frameCount;

        if (isTypingFinished) return;
        if (targetLines.Count == 0) return;

        if (!isTypingStarted)
        {
            startTime = Time.time;
            isTypingStarted = true;
            Debug.Log("[DEBUG] InputManager: タイピング開始");
        }

        totalKeystrokes++; // ここで総入力数をカウントしています

        string currentLineStr = targetLines[currentLineIndex];

        if (indexInCurrentLine >= currentLineStr.Length)
        {
            MoveToNextLine();
            UpdateTargetTextHighlight();
            return;
        }

        char expectedChar = currentLineStr[indexInCurrentLine];

        if (inputChar == expectedChar)
        {
            indexInCurrentLine++;
            if (indexInCurrentLine >= currentLineStr.Length)
            {
                MoveToNextLine();
            }
        }
        else
        {
            incorrectKeystrokes++;
        }

        UpdateTargetTextHighlight();

        if (!isTypingFinished) CalculateLiveWPM(Time.time - startTime);
        UpdateStatsUI();
    }

    private void MoveToNextLine()
    {
        currentLineIndex++;
        indexInCurrentLine = 0;

        if (currentLineIndex >= targetLines.Count)
        {
            currentLineIndex = targetLines.Count - 1;
            indexInCurrentLine = targetLines[currentLineIndex].Length;
            FinishTyping(true);
        }
    }

    private void FinishTyping(bool isCompleted)
    {
        isTypingFinished = true;
        CalculateLiveWPM(Time.time - startTime);

        string resultMsg = isCompleted ? "完走!" : "時間切れ!";
        Debug.Log($"[DEBUG] InputManager: {resultMsg} Total Keys: {totalKeystrokes}");

        UpdateStatsUI();
        UpdateTargetTextHighlight();
    }

    private void CalculateLiveWPM(float timeElapsed)
    {
        if (timeElapsed <= 0.0001f || totalKeystrokes == 0)
        {
            currentWPM = 0f;
            return;
        }
        float words = totalKeystrokes / 5.0f;
        float minutes = timeElapsed / 60.0f;
        currentWPM = words / minutes;
    }

    public float GetAccuracyPercentage()
    {
        if (totalKeystrokes == 0) return 100f;
        return (float)(totalKeystrokes - incorrectKeystrokes) / totalKeystrokes * 100f;
    }

    // ▼▼▼ 変更箇所：表示内容をWPMから総入力数に変更 ▼▼▼
    private void UpdateStatsUI()
    {
        if (statsTextDisplay != null)
        {
            if (string.IsNullOrEmpty(targetString))
            {
                statsTextDisplay.text = "No Text...";
                return;
            }

            string statusHeader = isTypingFinished ? (currentLineIndex == targetLines.Count - 1 && indexInCurrentLine == targetLines[currentLineIndex].Length ? "[FINISHED]" : "[TIME UP]") : "";

            // ここを変更しました
            string statusStr = $"{statusHeader}\n" +
                               $"Progress: Line {currentLineIndex + 1} / {targetLines.Count}\n" +
                               $"Total Keys: {totalKeystrokes}\n" + // WPM -> Total Keys
                               $"Accuracy: {GetAccuracyPercentage():F1}%";
            statsTextDisplay.text = statusStr;
        }
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    private void UpdateTimerUI(float remainingTime)
    {
        if (timerTextDisplay != null)
        {
            if (timeLimitSeconds <= 0)
            {
                timerTextDisplay.text = "Time: No Limit";
            }
            else
            {
                remainingTime = Mathf.Max(0f, remainingTime);
                timerTextDisplay.text = $"Time: {remainingTime:F1}s";
            }
        }
    }

    private void UpdateTargetTextHighlight()
    {
        if (targetTextDisplay == null || targetLines.Count == 0) return;

        string currentLineStr = targetLines[currentLineIndex];

        string completedPart = currentLineStr.Substring(0, indexInCurrentLine);
        string line1Display = $"<color=green>{completedPart}</color>";

        if (indexInCurrentLine < currentLineStr.Length)
        {
            char nextChar = currentLineStr[indexInCurrentLine];
            string nextCharStr = isTypingFinished ? $"{nextChar}" : $"<color=#FFA500><u><b>{nextChar}</b></u></color>";
            string remainingPart = currentLineStr.Substring(indexInCurrentLine + 1);
            line1Display += nextCharStr + remainingPart;
        }

        string line2Display = "";
        if (currentLineIndex + 1 < targetLines.Count)
        {
            line2Display = "\n" + targetLines[currentLineIndex + 1];
        }

        targetTextDisplay.text = line1Display + line2Display;
    }

    public void Space() { ProcessInput(' '); }
    public void AppendCharacter(string text) { if (!string.IsNullOrEmpty(text)) ProcessInput(text[0]); }
    public void Backspace() { }
}
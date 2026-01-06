using UnityEngine;
using TMPro;
using System;

public class InputManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI targetTextDisplay;
    public TMP_InputField inputField;
    [Tooltip("WPM、精度、進行状況を表示するテキスト")]
    public TextMeshProUGUI statsTextDisplay;

    [Header("Game Settings")]
    [TextArea(3, 5)]
    public string targetString = "";

    private int currentIndex = 0; // 現在の進行位置（何文字目まで打ったか）
    private float startTime;
    private bool isTypingStarted = false;
    private bool isTypingFinished = false;

    private float currentWPM = 0f;
    private int totalKeystrokes = 0;     // 総打鍵数
    private int incorrectKeystrokes = 0; // ミスタイプ数

    // 二重入力防止用の変数
    private int lastInputFrame = -1;

    void Start()
    {
        if (targetTextDisplay != null) targetTextDisplay.text = "";
        if (statsTextDisplay == null) Debug.LogWarning("[DEBUG] InputManager: statsTextDisplay がアサインされていません！");
        UpdateStatsUI();
    }

    void Update()
    {
        if (isTypingStarted && !isTypingFinished)
        {
            CalculateLiveWPM();
        }
        UpdateStatsUI();
    }

    public void SetTargetText(string newText)
    {
        // Debug.Log($"[DEBUG] InputManager: テキストが設定されました: {newText}");
        targetString = newText;
        InitializeGame();
    }

    private void InitializeGame()
    {
        if (string.IsNullOrEmpty(targetString))
        {
            UpdateStatsUI();
            return;
        }
        if (targetTextDisplay != null) targetTextDisplay.text = targetString;
        if (inputField != null) inputField.text = "";

        currentIndex = 0;
        totalKeystrokes = 0;
        incorrectKeystrokes = 0;
        currentWPM = 0f;
        isTypingStarted = false;
        isTypingFinished = false;
        lastInputFrame = -1;
        UpdateTargetTextHighlight();
        UpdateStatsUI();
    }

    public void ProcessInput(char inputChar)
    {
        if (Time.frameCount == lastInputFrame) return;
        lastInputFrame = Time.frameCount;

        if (isTypingFinished) return;
        if (string.IsNullOrEmpty(targetString)) return;

        if (!isTypingStarted)
        {
            startTime = Time.time;
            isTypingStarted = true;
            // Debug.Log("[DEBUG] InputManager: タイピング開始");
        }

        totalKeystrokes++;

        if (currentIndex < targetString.Length)
        {
            // 正誤判定を行う
            bool isCorrect = (inputChar == targetString[currentIndex]);

            if (isCorrect)
            {
                // 正解の場合の処理（ログ出しなど）
                // Debug.Log("Correct!");
            }
            else
            {
                // 不正解の場合の処理
                incorrectKeystrokes++;
                Debug.Log($"Mistake! Expected: '{targetString[currentIndex]}', Input: '{inputChar}'");
            }

            // ▼▼▼ 変更点：正誤に関わらず、必ず次の文字へ進める ▼▼▼
            currentIndex++;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            // 完了判定
            if (currentIndex >= targetString.Length)
            {
                FinishTyping();
            }
        }

        UpdateTargetTextHighlight();
        if (!isTypingFinished) CalculateLiveWPM();
        UpdateStatsUI();
    }

    private void FinishTyping()
    {
        isTypingFinished = true;
        CalculateLiveWPM();
        Debug.Log($"[DEBUG] InputManager: タイピング完了! Final WPM: {currentWPM:F0}, Accuracy: {GetAccuracyPercentage():F1}%");
        UpdateStatsUI();
        UpdateTargetTextHighlight();
    }

    private void CalculateLiveWPM()
    {
        float timeElapsed = Time.time - startTime;
        if (timeElapsed <= 0.0001f || totalKeystrokes == 0)
        {
            currentWPM = 0f;
            return;
        }
        // Gross WPM計算
        float words = totalKeystrokes / 5.0f;
        float minutes = timeElapsed / 60.0f;
        currentWPM = words / minutes;
    }

    public float GetAccuracyPercentage()
    {
        if (totalKeystrokes == 0) return 100f;
        return (float)(totalKeystrokes - incorrectKeystrokes) / totalKeystrokes * 100f;
    }

    private void UpdateStatsUI()
    {
        if (statsTextDisplay != null)
        {
            if (string.IsNullOrEmpty(targetString))
            {
                statsTextDisplay.text = "No Text...";
                return;
            }
            int totalChars = targetString.Length;
            int currentProgress = currentIndex;

            string statusStr = $"Progress: {currentProgress} / {totalChars}\n" +
                               $"WPM: {currentWPM:F0}\n" +
                               $"Accuracy: {GetAccuracyPercentage():F1}% (Errors: {incorrectKeystrokes})";

            statsTextDisplay.text = statusStr;
        }
    }

    private void UpdateTargetTextHighlight()
    {
        if (targetTextDisplay == null || string.IsNullOrEmpty(targetString)) return;

        // 完了部分は緑
        string completedPart = targetString.Substring(0, currentIndex);
        // エラーで進んだ場合も「完了」扱いになるので、全て緑色で表示します。
        // もしエラー箇所を赤くしたい場合は、ここで一文字ずつ判定して色を変える複雑な処理が必要です。
        string completedStr = $"<color=green>{completedPart}</color>";

        string nextCharStr = "";
        if (currentIndex < targetString.Length)
        {
            char nextChar = targetString[currentIndex];
            // 次の文字はオレンジで強調
            nextCharStr = $"<color=#FFA500><u><b>{nextChar}</b></u></color>";
        }

        string remainingPart = "";
        if (currentIndex + 1 < targetString.Length)
        {
            remainingPart = targetString.Substring(currentIndex + 1);
        }
        targetTextDisplay.text = completedStr + nextCharStr + remainingPart;
    }

    // 互換性メソッド
    public void Space() { ProcessInput(' '); }
    public void AppendCharacter(string text) { if (!string.IsNullOrEmpty(text)) ProcessInput(text[0]); }
    public void Backspace() { /* エラーでも進む仕様なのでバックスペースは無効化が妥当 */ }
}
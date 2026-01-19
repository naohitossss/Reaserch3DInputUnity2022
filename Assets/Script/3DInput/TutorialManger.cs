using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("External References")]
    public InputController inputController;
    public JapaneseFlickController japaneseController;
    public InputManager inputManager;

    [Header("Target Settings")]
    [TextArea(3, 10)]
    public string targetSentence = "Hello World\nThis is next line.";

    [Header("UI References (Text)")]
    public TextMeshProUGUI targetCharText;

    [Header("UI References (Images)")]
    public Image firstGestureImage;
    public Image secondGestureImage;

    [Header("Resources (Sprites)")]
    public Sprite[] directionSprites;
    public Sprite spaceGestureSprite;

    private List<char> charList = new List<char>();
    private int currentIndex = 0;

    private struct GestureCombo
    {
        public int middleDir;
        public int secondDir;
        public bool isSpace;
    }
    private Dictionary<char, GestureCombo> charToGestureMap = new Dictionary<char, GestureCombo>();

    void Start()
    {
        if (inputController == null) inputController = FindObjectOfType<InputController>();
        if (japaneseController == null) japaneseController = FindObjectOfType<JapaneseFlickController>();
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();

        if (inputManager != null && !string.IsNullOrEmpty(targetSentence))
        {
            inputManager.SetTargetText(targetSentence);
        }

        SplitSentence();

        if (inputController != null && inputController.KeyLayout != null)
        {
            BuildReverseMap(inputController.KeyLayout);
        }

        // 開始時に最初の有効な文字まで自動スキップ
        SkipInvalidChars();
        UpdateGuideDisplay();
        
        if (inputController != null) inputController.OnCharacterInputted += OnHandleInput;
        if (japaneseController != null) japaneseController.OnCharacterInputted += OnHandleInput;
    }

    void OnDestroy()
    {
        if (inputController != null) inputController.OnCharacterInputted -= OnHandleInput;
        if (japaneseController != null) japaneseController.OnCharacterInputted -= OnHandleInput;
    }

    private void OnHandleInput(string inputtedString)
    {
        if (string.IsNullOrEmpty(inputtedString) || currentIndex >= charList.Count) return;
        
        char inputChar = inputtedString[0];
        char targetChar = charList[currentIndex];

        // 統計報告：有効な文字（スペース含む）に対して実行
        if (inputManager != null)
        {
            inputManager.ProcessInput(inputChar);
        }

        // 正誤判定：現在の有効な文字と一致するかを確認
        if (inputChar == targetChar)
        {
            NextChar();
        }
    }

    private void NextChar()
    {
        currentIndex++;
        // 改行を含めた無効な文字を内部的に自動スキップ
        SkipInvalidChars();
        UpdateGuideDisplay();
    }

    // 内部処理：改行文字を検知した場合、currentIndexを自動で進める
    private void SkipInvalidChars()
    {
        while (currentIndex < charList.Count)
        {
            char currentChar = charList[currentIndex];

            // 改行文字は内部的にスキップ
            if (currentChar == '\n' || currentChar == '\r')
            {
                currentIndex++;
                continue;
            }

            // マップにない文字もスキップ（※スペースはマップに含まれるためスキップされない）
            if (!charToGestureMap.ContainsKey(char.ToLower(currentChar)))
            {
                currentIndex++;
                continue;
            }

            break;
        }
    }

    private void SplitSentence()
    {
        charList.Clear();
        if (!string.IsNullOrEmpty(targetSentence))
        {
            string normalized = targetSentence.Replace("\r\n", "\n").Replace('\r', '\n');
            charList.AddRange(normalized.ToCharArray());
        }
        currentIndex = 0;
    }

    private void BuildReverseMap(string[,] layout)
    {
        charToGestureMap.Clear();
        int rows = layout.GetLength(0);
        int cols = layout.GetLength(1);

        for (int mDir = 0; mDir < rows; mDir++)
        {
            for (int sDir = 0; sDir < cols; sDir++)
            {
                string keyCharStr = layout[mDir, sDir];
                if (string.IsNullOrEmpty(keyCharStr)) continue;
                char baseChar = keyCharStr[0];
                var combo = new GestureCombo { middleDir = mDir, secondDir = sDir };
                charToGestureMap[char.ToLower(baseChar)] = combo;
                charToGestureMap[char.ToUpper(baseChar)] = combo;
            }
        }
        // スペースキーは入力可能な文字として維持
        charToGestureMap[' '] = new GestureCombo { isSpace = true };
    }

    public void UpdateGuideDisplay()
    {
        if (currentIndex >= charList.Count)
        {
            if (targetCharText != null) targetCharText.text = "Good Job!";
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        char targetChar = charList[currentIndex];
        if (targetCharText != null)
        {
            // 改行はスキップされるため、ここにはスペースか通常の文字のみが到達する
            targetCharText.text = (targetChar == ' ') ? "[Space]" : $"'{targetChar}'";
        }

        char searchChar = char.ToLower(targetChar);
        if (!charToGestureMap.ContainsKey(searchChar)) return;

        GestureCombo combo = charToGestureMap[searchChar];
        if (combo.isSpace)
        {
            SetImageSprite(firstGestureImage, spaceGestureSprite);
            SetImageActive(firstGestureImage, true);
            SetImageActive(secondGestureImage, false);
        }
        else
        {
            SetImageSprite(firstGestureImage, directionSprites[combo.middleDir]);
            SetImageSprite(secondGestureImage, directionSprites[combo.secondDir]);
            SetImageActive(firstGestureImage, true);
            SetImageActive(secondGestureImage, true);
        }
    }

    private void SetImageSprite(Image targetImage, Sprite newSprite) { if (targetImage != null) targetImage.sprite = newSprite; }
    private void SetImageActive(Image targetImage, bool isActive) { if (targetImage != null) targetImage.gameObject.SetActive(isActive); }
}
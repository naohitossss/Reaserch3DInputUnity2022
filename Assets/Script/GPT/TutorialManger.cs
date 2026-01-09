using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("External References")]
    [Tooltip("キーマップ定義と入力イベントを持つ InputController への参照")]
    [SerializeField] private InputController inputController;
    [Tooltip("タイピングゲームロジックを持つ InputManager への参照")]
    [SerializeField] private InputManager inputManager;

    [Header("Target Settings")]
    [Tooltip("表示対象の文章（これがInputManagerにも渡されます）")]
    [TextArea(3, 10)]
    public string targetSentence = "Hello World\nThis is next line.";

    [Header("UI References (Text)")]
    [Tooltip("現在入力すべき文字のガイドを表示するテキスト")]
    public TextMeshProUGUI targetCharText;

    [Header("UI References (Images)")]
    [Tooltip("1段階目のジェスチャーガイド画像")]
    public Image firstGestureImage;
    [Tooltip("2段階目のジェスチャーガイド画像")]
    public Image secondGestureImage;

    [Header("Resources (Sprites)")]
    [Tooltip("方向指示用スプライト。順序: 右, 左, 上, 下, 前, 後")]
    public Sprite[] directionSprites;
    [Tooltip("スペース/エンター入力指示用スプライト")]
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
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();

        if (inputController == null || directionSprites == null || directionSprites.Length != 6 || spaceGestureSprite == null)
        {
            Debug.LogError("【TutorialManager】必要な参照が不足しています。Inspectorを確認してください。");
            return;
        }

        if (inputManager != null && !string.IsNullOrEmpty(targetSentence))
        {
            inputManager.SetTargetText(targetSentence);
        }

        SplitSentence();

        if (inputController.KeyLayout != null)
        {
            BuildReverseMap(inputController.KeyLayout);
        }
        else
        {
            Debug.LogError("【TutorialManager】InputControllerからKeyLayoutを取得できませんでした。");
            return;
        }

        SkipInvalidChars();
        UpdateGuideDisplay();
        inputController.OnCharacterInputted += OnAnyCharacterInputted;
    }

    void OnDestroy()
    {
        if (inputController != null)
        {
            inputController.OnCharacterInputted -= OnAnyCharacterInputted;
        }
    }

    private void OnAnyCharacterInputted(string inputtedString)
    {
        if (string.IsNullOrEmpty(inputtedString)) return;
        char inputChar = inputtedString[0];

        // InputManagerへの重複転送は行わない

        if (currentIndex >= charList.Count) return;

        char targetChar = charList[currentIndex];

        bool isCorrect = false;
        // 改行文字はスペース入力で正解とする
        if (targetChar == '\n' || targetChar == '\r')
        {
            isCorrect = (inputChar == ' ');
        }
        else
        {
            // ▼▼▼ 修正箇所：大文字小文字を厳密に区別して比較 ▼▼▼
            isCorrect = (inputChar == targetChar);
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }

        if (isCorrect)
        {
            NextChar();
        }
    }

    private void NextChar()
    {
        currentIndex++;
        SkipInvalidChars();
        UpdateGuideDisplay();
    }

    private void SkipInvalidChars()
    {
        while (currentIndex < charList.Count)
        {
            char currentChar = charList[currentIndex];

            if (currentChar == '\n' || currentChar == '\r')
            {
                break;
            }

            if (charToGestureMap.ContainsKey(char.ToLower(currentChar)))
            {
                break;
            }

            currentIndex++;
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
                // ガイド表示用には大小両方を登録しておく（変更なし）
                charToGestureMap[char.ToLower(baseChar)] = combo;
                charToGestureMap[char.ToUpper(baseChar)] = combo;
            }
        }
        var spaceCombo = new GestureCombo { isSpace = true };
        charToGestureMap[' '] = spaceCombo;
        charToGestureMap['\n'] = spaceCombo;
        charToGestureMap['\r'] = spaceCombo;
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

        if (charList.Count == 0)
        {
            if (targetCharText != null) targetCharText.text = "";
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        char targetChar = charList[currentIndex];

        if (targetCharText != null)
        {
            string charDisplay = $"'{targetChar}'";
            if (targetChar == ' ') charDisplay = "[Space]";
            else if (targetChar == '\n' || targetChar == '\r') charDisplay = "[Enter]";

            targetCharText.text = charDisplay;
        }

        char searchChar = char.ToLower(targetChar);

        if (!charToGestureMap.ContainsKey(searchChar))
        {
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        GestureCombo combo = charToGestureMap[searchChar];

        if (combo.isSpace)
        {
            SetImageSprite(firstGestureImage, spaceGestureSprite);
            SetImageActive(firstGestureImage, true);
            SetImageActive(secondGestureImage, false);
        }
        else
        {
            if (IsValidIndex(combo.middleDir) && IsValidIndex(combo.secondDir))
            {
                SetImageSprite(firstGestureImage, directionSprites[combo.middleDir]);
                SetImageSprite(secondGestureImage, directionSprites[combo.secondDir]);
                SetImageActive(firstGestureImage, true);
                SetImageActive(secondGestureImage, true);
            }
        }
    }

    private void SetImageSprite(Image targetImage, Sprite newSprite)
    {
        if (targetImage != null && newSprite != null) targetImage.sprite = newSprite;
    }

    private void SetImageActive(Image targetImage, bool isActive)
    {
        if (targetImage != null && targetImage.gameObject.activeSelf != isActive)
        {
            targetImage.gameObject.SetActive(isActive);
        }
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < directionSprites.Length;
    }
}
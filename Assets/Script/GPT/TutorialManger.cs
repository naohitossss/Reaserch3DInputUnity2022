using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("External References")]
    [Tooltip("キーマップ定義を持つ InputController への参照")]
    [SerializeField] private InputController inputController;

    [Header("Target Settings")]
    [Tooltip("表示対象の文章")]
    [TextArea(3, 10)]
    public string targetSentence = "Hello World";

    [Header("UI References (Text)")]
    public TextMeshProUGUI targetCharText;

    [Header("UI References (Images)")]
    public Image firstGestureImage;
    public Image secondGestureImage;

    [Header("Resources (Sprites)")]
    [Tooltip("順序: 右, 左, 上, 下, 前, 後")]
    public Sprite[] directionSprites;
    public Sprite spaceGestureSprite;

    private List<char> charList = new List<char>();
    private int currentIndex = 0;

    // 逆引き用構造体
    private struct GestureCombo
    {
        public int middleDir;
        public int secondDir;
        public bool isSpace;
    }
    private Dictionary<char, GestureCombo> charToGestureMap = new Dictionary<char, GestureCombo>();

    void Start()
    {
        // 参照チェック
        if (inputController == null)
        {
            inputController = FindObjectOfType<InputController>();
            if (inputController == null)
            {
                Debug.LogError("【TutorialManager】InputControllerが見つかりません。");
                return;
            }
        }

        // 画像リソースチェック
        if (directionSprites == null || directionSprites.Length != 6 || spaceGestureSprite == null)
        {
            Debug.LogError("【TutorialManager】画像リソースが設定されていません。");
            return;
        }

        // 初期化処理
        SplitSentence();
        BuildReverseMap(inputController.KeyLayout);

        // 最初のガイドを表示
        UpdateGuideDisplay();

        // ★復活: InputControllerの入力イベントを購読する
        inputController.OnCharacterInputted += OnAnyCharacterInputted;
    }

    // ★復活: オブジェクト破棄時にイベント購読を解除
    void OnDestroy()
    {
        if (inputController != null)
        {
            inputController.OnCharacterInputted -= OnAnyCharacterInputted;
        }
    }

    /// <summary>
    /// ★追加: 何らかの文字が入力されたら呼ばれるハンドラ（正誤判定はしない）
    /// </summary>
    /// <param name="inputtedString">入力された文字（今回は使わない）</param>
    private void OnAnyCharacterInputted(string inputtedString)
    {
        // まだ表示する文字が残っていれば、次の文字へ進む
        if (currentIndex < charList.Count)
        {
            NextChar();
            Debug.Log($"入力検知: 次の文字へ進みます。(Index: {currentIndex})");
        }
    }

    private void SplitSentence()
    {
        charList.Clear();
        charList.AddRange(targetSentence.ToCharArray());
        currentIndex = 0;
    }

    // 引数でキーレイアウトを受け取る
    private void BuildReverseMap(string[,] layout)
    {
        charToGestureMap.Clear();
        if (layout == null)
        {
            Debug.LogError("キーレイアウトが取得できませんでした。");
            return;
        }

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
        charToGestureMap[' '] = new GestureCombo { isSpace = true };
    }

    public void UpdateGuideDisplay()
    {
        // 最後の文字まで表示し終わった場合
        if (currentIndex >= charList.Count)
        {
            targetCharText.text = "完了!";
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        // 文章が空の場合
        if (charList.Count == 0)
        {
            targetCharText.text = "";
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        char targetChar = charList[currentIndex];
        targetCharText.text = targetChar == ' ' ? "[Space]" : $"'{targetChar}'";

        if (!charToGestureMap.ContainsKey(targetChar))
        {
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        GestureCombo combo = charToGestureMap[targetChar];

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
        if (targetImage != null) targetImage.gameObject.SetActive(isActive);
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < directionSprites.Length;
    }

    // ★復活: 次の文字へインデックスを進めて表示を更新する
    private void NextChar()
    {
        currentIndex++;
        UpdateGuideDisplay();
    }
}
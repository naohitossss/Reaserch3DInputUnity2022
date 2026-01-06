using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("External References")]
    [Tooltip("キーマップ定義と入力イベントを持つ InputController への参照")]
    [SerializeField] private InputController inputController;

    // ▼▼▼ 追加：InputManager への参照 ▼▼▼
    [Tooltip("タイピングゲームロジックを持つ InputManager への参照")]
    [SerializeField] private InputManager inputManager;
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    [Header("Target Settings")]
    [Tooltip("表示対象の文章（これがInputManagerにも渡され、タイピングの目標になります）")]
    [TextArea(3, 10)]
    public string targetSentence = "Hello World";

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
    [Tooltip("スペース入力指示用スプライト")]
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
        // 参照チェックと自動取得
        if (inputController == null) inputController = FindObjectOfType<InputController>();
        // ▼▼▼ 追加：InputManagerの自動取得 ▼▼▼
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        // 必須コンポーネントのチェック
        if (inputController == null)
        {
            Debug.LogError("【TutorialManager】InputControllerが見つかりません。Inspectorで設定してください。");
            // InputControllerがないと動作できないためリターン
            return;
        }
        if (inputManager == null)
        {
            Debug.LogWarning("【TutorialManager】InputManagerが見つかりません。タイピング判定は行われません。");
            // InputManagerがなくてもガイド表示だけは続けるため続行
        }

        // 画像リソースチェック
        if (directionSprites == null || directionSprites.Length != 6 || spaceGestureSprite == null)
        {
            Debug.LogError("【TutorialManager】画像リソースが不足しています。Inspectorで設定してください。");
            return;
        }

        // ▼▼▼ 重要：自身のテキストを InputManager に設定する ▼▼▼
        // これにより、TutorialManagerで設定した文章がタイピングの正解データとなります。
        if (inputManager != null)
        {
            inputManager.SetTargetText(targetSentence);
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        // 初期化処理（TutorialManager自身の準備）
        SplitSentence();

        // InputControllerからキーレイアウトを取得して逆引きマップを構築
        if (inputController.KeyLayout != null)
        {
            BuildReverseMap(inputController.KeyLayout);
        }
        else
        {
            Debug.LogError("【TutorialManager】InputControllerからKeyLayoutを取得できませんでした。");
            return;
        }

        // 最初の文字のガイドを表示
        UpdateGuideDisplay();

        // InputControllerからの文字入力イベントを購読する
        inputController.OnCharacterInputted += OnAnyCharacterInputted;
    }

    void OnDestroy()
    {
        // イベント購読の解除
        if (inputController != null)
        {
            inputController.OnCharacterInputted -= OnAnyCharacterInputted;
        }
    }

    /// <summary>
    /// InputControllerから何らかの文字入力があったら呼ばれるハンドラ
    /// </summary>
    /// <param name="inputtedString">入力された文字（今回は判定に使わないので無視します）</param>
    private void OnAnyCharacterInputted(string inputtedString)
    {
        // まだ表示する文字が残っていれば、無条件で次の文字へ進む
        if (currentIndex < charList.Count)
        {
            // Debug.Log($"入力検知 → 次の文字へ進みます。(Index: {currentIndex} -> {currentIndex + 1})");
            NextChar();
        }
    }

    // 次の文字へインデックスを進めて表示を更新する
    private void NextChar()
    {
        currentIndex++;
        UpdateGuideDisplay();
    }

    // 対象文章を文字リストに分割
    private void SplitSentence()
    {
        charList.Clear();
        if (!string.IsNullOrEmpty(targetSentence))
        {
            charList.AddRange(targetSentence.ToCharArray());
        }
        currentIndex = 0;
    }

    // キーレイアウトから逆引きマップ（文字→ジェスチャー）を構築
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

                // 大文字小文字両方を同じジェスチャーにマッピング
                var combo = new GestureCombo { middleDir = mDir, secondDir = sDir };
                charToGestureMap[char.ToLower(baseChar)] = combo;
                charToGestureMap[char.ToUpper(baseChar)] = combo;
            }
        }
        // スペースキーのマッピング
        charToGestureMap[' '] = new GestureCombo { isSpace = true };
    }

    // 現在のターゲット文字に合わせてガイド表示を更新
    public void UpdateGuideDisplay()
    {
        // 最後の文字まで表示し終わった場合
        if (currentIndex >= charList.Count)
        {
            if (targetCharText != null) targetCharText.text = "Good Job!";
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        // 文章が空の場合
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
            targetCharText.text = targetChar == ' ' ? "[Space]" : $"'{targetChar}'";
        }

        // マップにない文字（改行など）の場合はガイドを非表示
        if (!charToGestureMap.ContainsKey(targetChar))
        {
            SetImageActive(firstGestureImage, false);
            SetImageActive(secondGestureImage, false);
            return;
        }

        GestureCombo combo = charToGestureMap[targetChar];

        if (combo.isSpace)
        {
            // スペース用表示
            SetImageSprite(firstGestureImage, spaceGestureSprite);
            SetImageActive(firstGestureImage, true);
            SetImageActive(secondGestureImage, false);
        }
        else
        {
            // 通常文字用表示（2段階ジェスチャー）
            if (IsValidIndex(combo.middleDir) && IsValidIndex(combo.secondDir))
            {
                SetImageSprite(firstGestureImage, directionSprites[combo.middleDir]);
                SetImageSprite(secondGestureImage, directionSprites[combo.secondDir]);
                SetImageActive(firstGestureImage, true);
                SetImageActive(secondGestureImage, true);
            }
        }
    }

    // ユーティリティ: 画像のスプライト設定（nullチェック付き）
    private void SetImageSprite(Image targetImage, Sprite newSprite)
    {
        if (targetImage != null && newSprite != null) targetImage.sprite = newSprite;
    }

    // ユーティリティ: 画像の表示/非表示設定（nullチェック付き）
    private void SetImageActive(Image targetImage, bool isActive)
    {
        if (targetImage != null && targetImage.gameObject.activeSelf != isActive)
        {
            targetImage.gameObject.SetActive(isActive);
        }
    }

    // ユーティリティ: 配列インデックスの有効性チェック
    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < directionSprites.Length;
    }
}
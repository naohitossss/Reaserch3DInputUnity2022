using UnityEngine;
using TMPro;

public class WPMDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("WPMと文字数を表示するTextMeshProコンポーネント")]
    public TextMeshProUGUI wpmText;
    [Tooltip("入力監視対象のInputField")]
    public TMP_InputField inputField;

    [Header("WPM Settings")]
    [Tooltip("WPMの更新間隔（秒）")]
    public float wpmUpdateInterval = 0.5f; // 更新頻度を少し上げました

    private float startTime;
    private bool isTyping = false;
    private float nextWPMUpdate;

    void Start()
    {
        if (inputField == null)
        {
            Debug.LogError("WPMDisplay: InputFieldが割り当てられていません。");
            return;
        }
        
        // 初期表示の更新
        UpdateDisplay(0, 0);

        // InputFieldのイベントリスナー登録
        // 文字が入力または削除されるたびに呼び出される
        inputField.onValueChanged.AddListener(OnInputValueChanged);
    }

    /// <summary>
    /// InputFieldの値が変更されたときに呼ばれる
    /// </summary>
    /// <param name="text">現在の入力テキスト</param>
    private void OnInputValueChanged(string text)
    {
        // タイピングがまだ開始されておらず、かつテキストが空でない場合、タイピング開始とみなす
        if (!isTyping && text.Length > 0)
        {
            StartTyping();
        }
        // テキストが空になったらタイピング終了とみなす（すべて削除された場合など）
        else if (isTyping && text.Length == 0)
        {
            StopTyping();
        }

        // 文字数が変化したら即座に表示を更新する
        // WPMは頻繁に更新するとちらつくので、文字数だけ更新する
        CalculateAndDisplay(true); // true = 文字数のみ更新
    }

    void Update()
    {
        // タイピング中、かつ一定時間経過したらWPMも含めて表示を更新
        if (isTyping && Time.time > nextWPMUpdate)
        {
            CalculateAndDisplay(false); // false = WPMも更新
            nextWPMUpdate = Time.time + wpmUpdateInterval;
        }
    }

    /// <summary>
    /// タイピング開始時の処理
    /// </summary>
    public void StartTyping()
    {
        if (!isTyping)
        {
            startTime = Time.time;
            isTyping = true;
            nextWPMUpdate = Time.time + wpmUpdateInterval;
            // Debug.Log("Typing Started");
        }
    }

    /// <summary>
    /// タイピング終了時の処理
    /// </summary>
    public void StopTyping()
    {
        if (isTyping)
        {
            isTyping = false;
            // 最後に一度WPMと文字数を計算して表示
            CalculateAndDisplay(false);
            // Debug.Log("Typing Stopped");
        }
    }

    /// <summary>
    /// WPMと文字数を計算し、表示を更新するメイン処理
    /// </summary>
    /// <param name="charCountOnly">trueの場合、WPMは再計算せず文字数のみ更新する</param>
    private void CalculateAndDisplay(bool charCountOnly)
    {
        if (inputField == null) return;

        int currentLength = inputField.text.Length;
        int wpm = 0;

        if (isTyping)
        {
            if (charCountOnly)
            {
                // 文字数のみ更新したいが、現在のWPMの値がわからないので
                // 仕方なく再計算する（負荷は軽微）
                wpm = CalculateWPM(currentLength);
            }
            else
            {
                wpm = CalculateWPM(currentLength);
            }
        }
        else if (currentLength > 0)
        {
             // タイピング終了後も、文字が残っていれば最後の計算結果を表示する
             wpm = CalculateWPM(currentLength);
        }

        UpdateDisplay(wpm, currentLength);
    }

    /// <summary>
    /// 現在の文字数と経過時間からWPMを計算する
    /// </summary>
    private int CalculateWPM(int currentLength)
    {
        float elapsedTime = Time.time - startTime;

        if (elapsedTime <= 0 || currentLength == 0)
        {
            return 0;
        }

        // WPM計算: (文字数 / 5) / (経過時間(分))
        // 5文字で1単語とみなすのが一般的な定義
        float words = currentLength / 5.0f;
        float minutes = elapsedTime / 60.0f;
        return Mathf.RoundToInt(words / minutes);
    }

    /// <summary>
    /// UIのテキスト表示を更新する
    /// </summary>
    private void UpdateDisplay(int wpm, int charCount)
    {
        if (wpmText != null)
        {
            // WPMと文字数を併記するフォーマット
            wpmText.text = $"WPM: {wpm} | 文字数: {charCount}";
        }
    }

    /// <summary>
    /// 外部からリセットしたい場合（例：クリアボタンなど）
    /// </summary>
    public void ResetDisplay()
    {
        StopTyping();
        if (inputField != null)
        {
            inputField.text = "";
        }
        UpdateDisplay(0, 0);
    }
}
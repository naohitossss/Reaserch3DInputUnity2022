using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    public static InputManager instance { get; private set; }

    [SerializeField]
    private TMP_InputField inputField; // UIの入力フィールドへの参照

    [Header("WPM Display")]
    [SerializeField]
    private TextMeshProUGUI wpmText;    // WPM表示用UIへの参照

    private bool isShiftActive = false; // Shiftキーの状態

    // WPM計算用変数
    private float startTime;         // 入力開始時刻
    private bool isTyping = false;   // 入力中かどうかのフラグ

    [Header("WPM Calculation")]
    [Tooltip("WPMを更新する間隔（秒）")]
    public float wpmUpdateInterval = 1.0f;
    private float nextWpmUpdateTime;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーンをまたいでも破棄されないようにする
        }
        else
        {
            Destroy(gameObject); // 既にインスタンスがあれば、重複を防ぐために自身を破棄
        }
    }

    private void Start()
    {
        // 初期化時にInputFieldとWPMTextが設定されているか確認
        if (inputField == null)
        {
            Debug.LogError("TMP_InputField is not assigned to InputManager!");
        }
        if (wpmText == null)
        {
            Debug.LogWarning("WPM TextMeshProUGUI is not assigned to InputManager. WPM will not be displayed.");
        }

        // 初期表示をクリアし、WPMを0に設定
        if (inputField != null) inputField.text = ""; // InputFieldのテキストをクリア
        UpdateWPMText(0);

        // WPM更新の初期時刻を設定
        nextWpmUpdateTime = Time.time + wpmUpdateInterval;
    }

    private void Update()
    {
        // 入力中の場合のみWPMを更新
        if (isTyping && Time.time >= nextWpmUpdateTime)
        {
            CalculateAndDisplayWPM();
            nextWpmUpdateTime = Time.time + wpmUpdateInterval;
        }
    }

    // 文字入力
    public void AppendCharacter(string character)
    {
        if (inputField == null) return;

        if (!isTyping)
        {
            StartTyping(); // 最初の一文字が入力されたらタイピング開始
        }

        string finalCharacter = isShiftActive ? character.ToUpper() : character.ToLower();
        inputField.text += finalCharacter; // 直接 inputField.text を操作
    }

    // バックスペース
    public void Backspace()
    {
        if (inputField == null) return;

        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1); // 直接 inputField.text を操作
        }
        
        // inputField.text が空になったらタイピング終了とみなす
        if (inputField.text.Length == 0 && isTyping)
        {
            StopTyping();
        }
    }

    // スペース入力
    public void Space()
    {
        if (inputField == null) return;

        if (!isTyping)
        {
            StartTyping();
        }
        inputField.text += " "; // 直接 inputField.text を操作
    }
    
    // Shift状態の切り替え
    public void ToggleShift()
    {
        isShiftActive = !isShiftActive;
        Debug.Log("Shift is now " + (isShiftActive ? "ON (Uppercase)" : "OFF (Lowercase)"));
    }
    
    // Shift状態を明示的に設定するメソッド
    public void SetShift(bool active)
    {
        isShiftActive = active;
        if (active)
        {
            Debug.Log("Shift is now ON (Uppercase)");
        }
        else
        {
            Debug.Log("Shift is now OFF (Lowercase)");
        }
    }

    // WPMの計算と表示
    private void CalculateAndDisplayWPM()
    {
        // inputField.text.Length が0の場合、またはタイピング中でない場合はWPMを0に
        if (!isTyping || inputField == null || inputField.text.Length == 0)
        {
            UpdateWPMText(0);
            return;
        }

        float elapsedTime = Time.time - startTime; // 経過時間（秒）

        // 経過時間が0の場合の除算エラーを避ける
        if (elapsedTime <= 0)
        {
            UpdateWPMText(0);
            return;
        }

        // inputField.text.Length を直接使用してWPMを計算
        // 5文字で1単語と仮定: (総文字数 / 5) / (経過時間 / 60)
        float wpm = (inputField.text.Length / 5f) / (elapsedTime / 60f);
        UpdateWPMText(Mathf.RoundToInt(wpm)); // 整数に丸めて表示
    }

    // WPMをUIに反映
    private void UpdateWPMText(int wpmValue)
    {
        if (wpmText != null)
        {
            wpmText.text = $"WPM: {wpmValue}";
        }
    }

    // タイピング開始時の処理
    private void StartTyping()
    {
        isTyping = true;
        startTime = Time.time; // タイピング開始時刻を記録
        // inputField.text.Length が0から始まることを想定
        nextWpmUpdateTime = Time.time + wpmUpdateInterval; // 次回更新時刻を設定
        Debug.Log("Typing started.");
    }

    // タイピング終了時の処理
    private void StopTyping()
    {
        isTyping = false;
        CalculateAndDisplayWPM(); // 終了時のWPMを最終表示
        // inputField.text.Length はそのまま残る
        Debug.Log("Typing stopped.");
    }
}
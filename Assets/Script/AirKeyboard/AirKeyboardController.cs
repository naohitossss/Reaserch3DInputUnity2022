using UnityEngine;
using TMPro;

public class KeyboardTestController : MonoBehaviour
{
    // --- インスペクター設定 ---
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    
    [Header("PCキーボード設定")]
    [Tooltip("キーボードを呼び出すPCキーボードのキー")]
    // 'Q' キーを使用
    public KeyCode triggerKey = KeyCode.Q; 
    
    private TouchScreenKeyboard overlayKeyboard;

    private void Start()
    {
        if (textMeshProUGUI != null)
        {
            // 初期テキストがnullでないことを保証
            textMeshProUGUI.text = textMeshProUGUI.text ?? ""; 
        }
    }

    private void Update()
    {
        // 1. キーボードが開いていない場合、PCキーボード入力をチェック
        if (overlayKeyboard == null)
        {
            CheckPCKeyboardInput();
            return;
        }

        // 2. キーボードが開いている場合、状態をチェック
        TouchScreenKeyboard.Status keyboardStatus = overlayKeyboard.status;

        // キーボードが閉じられた場合 (完了、キャンセル、または非表示)
        if (keyboardStatus != TouchScreenKeyboard.Status.Visible)
        {
            HandleKeyboardClosed(keyboardStatus);
        }
    }

    private void CheckPCKeyboardInput()
    {
        // 設定されたPCキー（KeyCode.Q）が押された瞬間をチェック
        // Input.GetKeyDownはUnity Editor上でのデバッグに有効です。
        if (Input.GetKeyDown(triggerKey))
        {
            OpenSystemKeyboard(); // キーボード呼び出しメソッドを実行
        }
    }

    void OpenSystemKeyboard()
    {
        // キーボードがすでに開いている場合は、何もしない
        if (overlayKeyboard != null) return;

        // TouchScreenKeyboard.Open() でキーボードを呼び出す
        string initialText = textMeshProUGUI != null ? textMeshProUGUI.text : "";
        
        overlayKeyboard = TouchScreenKeyboard.Open(
            initialText, 
            TouchScreenKeyboardType.Default,
            false, 
            false, 
            false, 
            false, 
            "テキストを入力してください"
        );

        Debug.Log($"--- PCキーボードの '{triggerKey.ToString()}' キーで呼び出しを試行 ---");
    }

    void HandleKeyboardClosed(TouchScreenKeyboard.Status status)
    {
        string finalInput = overlayKeyboard.text;

        if (status == TouchScreenKeyboard.Status.Done)
        {
            // 入力確定時: 最終結果をUIに反映
            if (textMeshProUGUI != null) textMeshProUGUI.text = finalInput;
            Debug.Log("入力完了。結果: " + finalInput);
        }
        else if (status == TouchScreenKeyboard.Status.Canceled)
        {
            // 入力キャンセル時: 元のテキストを維持
            Debug.Log("入力キャンセルされました。元のテキストを維持します。");
        }
        else
        {
            // Hiddenなどのその他の状態: 念のため最終結果を反映
            if (textMeshProUGUI != null) textMeshProUGUI.text = finalInput;
            Debug.Log("キーボードが閉じられました (状態: " + status.ToString() + ")");
        }

        // キーボードのインスタンスをリセット
        overlayKeyboard = null;
    }
}
using UnityEngine;
using TMPro; // TextMeshProUGUIを使用

public class SimpleKeyboardCaller : MonoBehaviour
{
    // --- インスペクター設定 ---
    [Header("表示するTextコンポーネント")]
    [Tooltip("入力結果を表示する TextMeshProUGUI コンポーネントを割り当ててください。")]
    public TextMeshProUGUI targetText;
    
    [Header("キーボードを呼び出すボタン")]
    [Tooltip("キーボード表示をトリガーするボタンを指定します。例: OVRInput.RawButton.A")]
    public OVRInput.RawButton triggerButton = OVRInput.RawButton.A; 
    
    // --- プライベート変数 ---
    private TouchScreenKeyboard systemKeyboard; 
    private string currentInputText = "新しい入力"; // キーボードに表示する初期テキスト

    void Update()
    {
        // 1. キーボードが開いていないときのみ、ボタン入力をチェック
        if (systemKeyboard == null)
        {
            CheckForKeyboardTrigger();
        }
        // 2. キーボードが開いている場合は、状態をチェックし、入力結果を処理
        else
        {
            HandleKeyboardState();
        }
    }

    private void CheckForKeyboardTrigger()
    {
        // コントローラーのボタンが押された瞬間 (Down) にキーボードを呼び出す
        if (OVRInput.GetDown(triggerButton)) 
        {
            OpenSystemKeyboard();
        }
    }

    /// <summary>
    /// Unity標準の TouchScreenKeyboard.Open() を使用してシステムキーボードを呼び出す。
    /// </summary>
    public void OpenSystemKeyboard()
    {
        Debug.Log("--- キーボード呼び出しを試行 ---");
        // TouchScreenKeyboard.Open() を使用。
        // Meta Quest環境では、これがVRシステムキーボード（エアキーボード）としてオーバーレイ表示されます。
        systemKeyboard = TouchScreenKeyboard.Open(
            currentInputText,              // 初期テキスト
            TouchScreenKeyboardType.Default, // キーボードの種類
            false,                         // オートコレクト
            false,                         // 複数行
            false,                         // パスワード入力（非表示）
            false,                         // アラート
            "文字を入力してください"         // プレースホルダーテキスト
        );

        // ※注意: TouchScreenKeyboard.Open() はキーボードの「表示位置」を指定できません。
        // 通常、キーボードはHMDに対して固定の位置に表示されます。
        
        if (systemKeyboard == null)
        {
            Debug.LogError("システムキーボードの呼び出しに失敗しました。OVRManagerの設定を確認してください。");
        }
    }

    /// <summary>
    /// キーボードの状態（入力中、完了、キャンセル）を処理し、結果をUIに反映する。
    /// </summary>
    private void HandleKeyboardState()
{
    if (systemKeyboard != null)
    {
        // ユーザーが入力中のテキストを随時更新
        currentInputText = systemKeyboard.text;

        // キーボードの状態を取得
        TouchScreenKeyboard.Status keyboardStatus = systemKeyboard.status;

        // キーボードが閉じられたか（完了、キャンセル、または非表示）をチェック
        if (keyboardStatus != TouchScreenKeyboard.Status.Visible) 
        {
            if (keyboardStatus == TouchScreenKeyboard.Status.Done)
            {
                // 入力完了時の処理
                if (targetText != null)
                {
                    targetText.text = "入力結果 (完了): " + currentInputText;
                }
                Debug.Log("入力完了。結果: " + currentInputText);
            }
            else if (keyboardStatus == TouchScreenKeyboard.Status.Canceled)
            {
                // 入力キャンセル時の処理
                if (targetText != null)
                {
                    targetText.text = "入力キャンセルされました。";
                }
                Debug.Log("入力キャンセル。");
                
                // キャンセルの場合、currentInputTextをリセットする選択肢もある
                // currentInputText = ""; 
            }
            else // Hidden (システムによる非表示など) の場合
            {
                Debug.Log("キーボードが非表示になりました (状態: " + keyboardStatus.ToString() + ")");
            }
            
            // キーボードの状態をリセット
            systemKeyboard = null; 
        }
    }
}
}

using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    public static InputManager instance { get; private set; }

    [SerializeField]
    private TMP_InputField inputField;

    // Shiftキーが押されているかどうかの状態を保持するフラグ
    private bool isShiftActive = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // 他のスクリプトから文字を追加するための公開メソッド
    public void AppendCharacter(string character)
    {
        if (inputField == null) return;

        // Shiftがアクティブなら文字を大文字に、そうでなければ小文字にする
        string finalCharacter = isShiftActive ? character.ToUpper() : character.ToLower();
        inputField.text += finalCharacter;

        // ▼▼▼ 以下の行を削除またはコメントアウト ▼▼▼
        // isShiftActive = false; 
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
    }

    // バックスペース処理用のメソッド
    public void Backspace()
    {
        if (inputField != null && inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }

    // スペースを追加するメソッド
    public void Space()
    {
        if (inputField != null)
        {
            inputField.text += " ";
        }
    }

    // Shift状態を切り替えるメソッド（このメソッドは変更なし）
    public void ToggleShift()
    {
        isShiftActive = !isShiftActive;
        Debug.Log("Shift Lock: " + isShiftActive); // 状態確認用のログ
    }
}
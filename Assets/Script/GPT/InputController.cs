using UnityEngine;
using System;

public class InputController : MonoBehaviour
{
    public GestureInput gestureManager;

    private readonly string[] categories = { "1", "2", "3", "4", "5", "6" };
    private readonly string[,] keys =
    {
        { "1", "A", "B", "C", "D", "E" },
        { "2", "F", "G", "H", "I", "J" },
        { "3", "K", "L", "M", "N", "O" },
        { "4", "P", "Q", "R", "S", "T" },
        { "5", "U", "V", "W", "X", "Y" },
        { "6", "7", "8", "9", "0", " " },
    };

    public string[,] KeyLayout => keys;
    public event Action<string> OnCharacterInputted;

    private int currentCategory = -1;

    void Start()
    {
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += OnCategorySelected;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;
            gestureManager.OnUppercase += OnUppercase;
            gestureManager.OnLowercase += OnLowercase;

            // ▼▼▼【重要】ここを追加 ▼▼▼
            // スペース入力イベントを購読します。
            // ※ 'OnSpaceKey' というイベント名が GestureInput 側に存在している前提です。
            // もしエラーになる場合は、GestureInput.cs を確認し、正しいイベント名（例: OnSpace）に修正してください。
            gestureManager.OnSpaceKey += OnSpaceInput;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }
    }

    // ▼▼▼【重要】ここを追加 ▼▼▼
    // スペース入力時に呼ばれるメソッド
    void OnSpaceInput()
    {
        if (InputManager.instance != null)
        {
            // InputManagerを通してスペースを入力
            // ※InputManagerに 'Space()' メソッドが存在する前提です。
            // なければ AppendCharacter(" ") などに置き換えてください。
            InputManager.instance.Space(); 

            // チュートリアル用にイベントを発火（半角スペースを通知）
            OnCharacterInputted?.Invoke(" ");
            Debug.Log("␣ Space Inputted");
        }
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void OnCategorySelected(Vector3 start, Vector3 end)
    {
        currentCategory = DirectionalSelector.GetDirectionIndex(start, end);
        Debug.Log($" Category Selected: {categories[currentCategory]}");
    }

    void OnKeySelected(Vector3 start, Vector3 end)
    {
        if (currentCategory < 0) return;

        int keyIndex = DirectionalSelector.GetDirectionIndex(start, end);
        string key = keys[currentCategory, keyIndex];

        Debug.Log($"🔡 Key Selected: {key}");

        if (InputManager.instance != null)
        {
            InputManager.instance.AppendCharacter(key);
            // 簡易的に入力文字をそのまま通知
            OnCharacterInputted?.Invoke(key);
        }

        currentCategory = -1;
    }

    void OnBackspace()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.Backspace();
        }
    }

    void OnUppercase()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.SetShift(true);
            Debug.Log("🔠 Shift Activated (Uppercase)");
        }
    }

    void OnLowercase()
    {
        if (InputManager.instance != null)
        {
            InputManager.instance.SetShift(false);
            Debug.Log("🔡 Shift Deactivated (Lowercase)");
        }
    }
}
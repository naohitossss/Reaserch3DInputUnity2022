using UnityEngine;
using System;
using TMPro;

public class InputController : MonoBehaviour
{
    [Header("External References")]
    public GestureInput gestureManager;
    public TMP_InputField inputField;

    private readonly string[,] keys =
    {
        { "E", "A", "R", "M", "F", "V" },
        { "T", "O", "L", "C", "Y", "K" },
        { "N", "S", "D", "W", "P", "Q" },
        { "I", "H", "U", "G", "B", "J" },
        { "Z", "1", "2", "3", "4", "5" },
        { "X", "6", "7", "8", "9", "0" }
    };

    public string[,] KeyLayout => keys;
    public event Action<string> OnCharacterInputted;
    private int currentCategory = -1;
    private bool isShift = false;

    void Start()
    {
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += (idx) => currentCategory = idx;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;
            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
                gestureManager.GetType().GetEvent("OnSpaceKey").AddEventHandler(gestureManager, new Action(OnSpace));
            gestureManager.OnUppercase += () => isShift = true;
            gestureManager.OnLowercase += () => isShift = false;
        }
    }

    void OnKeySelected(int keyIndex)
    {
        if (currentCategory < 0 || currentCategory >= keys.GetLength(0) || keyIndex < 0 || keyIndex >= keys.GetLength(1)) return;

        string keyStr = keys[currentCategory, keyIndex];
        char inputChar = isShift ? char.ToUpper(keyStr[0]) : char.ToLower(keyStr[0]);

        if (inputField != null) inputField.text += inputChar;

        // 文字が入力されたことだけを通知（統計はTutorialManagerが処理）
        OnCharacterInputted?.Invoke(inputChar.ToString());
        currentCategory = -1;
    }

    public void OnSpace()
    {
        if (inputField != null) inputField.text += ' ';
        OnCharacterInputted?.Invoke(" ");
    }

    void OnBackspace()
    {
        if (inputField != null && inputField.text.Length > 0)
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
    }
}
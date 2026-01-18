using UnityEngine;
using TMPro;

public class OpenSystemKeyboard : MonoBehaviour
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        inputField.onSelect.AddListener(OpenKeyboard);
    }

    void OpenKeyboard(string text)
    {
        Debug.Log("Input Field Selected");
        keyboard = TouchScreenKeyboard.Open(
            text,
            TouchScreenKeyboardType.Default,
            false,
            false,
            false,
            false
        );
    }

    void Update()
    {
        if (keyboard != null)
        {
            inputField.text = keyboard.text;
        }
    }
}



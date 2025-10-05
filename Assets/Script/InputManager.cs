using UnityEngine;
using TMPro;

public class InputManager : MonoBehaviour
{
    public static InputManager instance { get; private set; }

    [SerializeField]
    private TMP_InputField inputField;

    private bool isShiftActive = false;

    private void Awake()
    {
        if (instance == null) 
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    public void AppendCharacter(string character)
    {
        if (inputField == null) return;

        string finalCharacter = isShiftActive ? character.ToUpper() : character.ToLower();
        inputField.text += finalCharacter;
    }

    public void Backspace()
    {
        if (inputField != null && inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }

    public void Space()
    {
        if (inputField != null)
        {
            inputField.text += " ";
        }
    }
    
    public void ToggleShift()
    {
        isShiftActive = !isShiftActive;
    }
}
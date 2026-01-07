using UnityEngine;
using System;
using TMPro;

public class InputController : MonoBehaviour
{
    [Header("External References")]
    public GestureInput gestureManager;
    public InputManager inputManager;
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

    // シフト状態を管理するフラグ
    private bool isShift = false;

    void Start()
    {
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                Debug.LogError("[DEBUG] InputController: InputManagerが見つかりません！");
            }
        }

        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += OnCategorySelected;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;

            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
            {
                gestureManager.GetType().GetEvent("OnSpaceKey").AddEventHandler(gestureManager, new Action(OnSpace));
            }

            // ▼▼▼ 追加：シフト（大文字/小文字）イベントの購読 ▼▼▼
            // GestureInput側にこれらのイベントが定義されている前提です
            gestureManager.OnUppercase += SetShiftOn;
            gestureManager.OnLowercase += SetShiftOff;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }
    }

    void OnDestroy()
    {
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected -= OnCategorySelected;
            gestureManager.OnKeySelected -= OnKeySelected;
            gestureManager.OnBackspace -= OnBackspace;
            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
            {
                gestureManager.GetType().GetEvent("OnSpaceKey").RemoveEventHandler(gestureManager, new Action(OnSpace));
            }

            // ▼▼▼ 追加：イベント購読解除 ▼▼▼
            gestureManager.OnUppercase -= SetShiftOn;
            gestureManager.OnLowercase -= SetShiftOff;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
        }
    }

    // ▼▼▼ 追加：シフト状態切り替えハンドラ ▼▼▼
    private void SetShiftOn()
    {
        isShift = true;
        // Debug.Log("[DEBUG] Shift ON (Uppercase)");
    }

    private void SetShiftOff()
    {
        isShift = false;
        // Debug.Log("[DEBUG] Shift OFF (Lowercase)");
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    void OnCategorySelected(Vector3 start, Vector3 end)
    {
        currentCategory = DirectionalSelector.GetDirectionIndex(start, end);
    }

    void OnKeySelected(Vector3 start, Vector3 end)
    {
        if (currentCategory < 0) return;

        int keyIndex = DirectionalSelector.GetDirectionIndex(start, end);
        string keyStr = keys[currentCategory, keyIndex];

        // ここで isShift フラグを使って大文字/小文字を決定します
        char inputChar = isShift ? char.ToUpper(keyStr[0]) : char.ToLower(keyStr[0]);

        // Debug.Log($"[DEBUG] InputController: キー選択検知: {inputChar} (Shift: {isShift})");

        if (inputField != null)
        {
            inputField.text += inputChar;
        }

        if (inputManager != null)
        {
            inputManager.ProcessInput(inputChar);
        }

        OnCharacterInputted?.Invoke(inputChar.ToString());

        currentCategory = -1;
    }

    public void OnSpace()
    {
        // Debug.Log($"[DEBUG] InputController: スペース入力検知");
        if (inputField != null) inputField.text += ' ';
        if (inputManager != null) inputManager.ProcessInput(' ');
        OnCharacterInputted?.Invoke(" ");
    }

    void OnBackspace()
    {
        // InputFieldのみ反映（InputManager側は進む仕様なので戻さない）
        if (inputField != null && inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
    }
}
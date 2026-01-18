using UnityEngine;
using System;
using TMPro;

public class InputController : MonoBehaviour
{
    [Header("External References")]
    public GestureInput gestureManager;
    public InputManager inputManager;
    public TMP_InputField inputField;

    // 6行x6列の配列
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
        // InputManagerの自動取得
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                // Debug.LogError("[DEBUG] InputController: InputManagerが見つかりません！");
            }
        }

        // GestureInputのイベント購読
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += OnCategorySelected;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;
            
            // OnSpaceKeyイベントが存在する場合のみ購読（互換性のため）
            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
            {
                gestureManager.GetType().GetEvent("OnSpaceKey").AddEventHandler(gestureManager, new Action(OnSpace));
            }

            gestureManager.OnUppercase += SetShiftOn;
            gestureManager.OnLowercase += SetShiftOff;
        }
    }

    void OnDestroy()
    {
        // イベント購読解除
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected -= OnCategorySelected;
            gestureManager.OnKeySelected -= OnKeySelected;
            gestureManager.OnBackspace -= OnBackspace;
            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
            {
                gestureManager.GetType().GetEvent("OnSpaceKey").RemoveEventHandler(gestureManager, new Action(OnSpace));
            }

            gestureManager.OnUppercase -= SetShiftOn;
            gestureManager.OnLowercase -= SetShiftOff;
        }
    }

    // シフト状態の切り替え
    private void SetShiftOn() { isShift = true; }
    private void SetShiftOff() { isShift = false; }

    // カテゴリ選択イベントハンドラ（方向インデックスを受け取る）
    void OnCategorySelected(int directionIndex)
    {
        currentCategory = directionIndex;
        // Debug.Log($"[InputController] Category set to: {currentCategory}");
    }

    // キー選択イベントハンドラ（方向インデックスを受け取る）
    void OnKeySelected(int keyIndex)
    {
        // カテゴリが未選択、または負の値の場合は無視
        if (currentCategory < 0) return;

        // 配列の範囲外アクセスを防ぐガード処理
        int rows = keys.GetLength(0); // 通常は 6
        int cols = keys.GetLength(1); // 通常は 6

        // currentCategory が行数以上、または keyIndex が不正な値(負または列数以上)の場合を防ぐ
        if (currentCategory >= rows || keyIndex < 0 || keyIndex >= cols)
        {
            Debug.LogWarning($"[InputController] 無効なインデックスを検知しました。Category: {currentCategory}, KeyIndex: {keyIndex}");
            currentCategory = -1; // 安全のためリセット
            return; // エラーになる前に処理を中断
        }

        // ここで安全にアクセスできる
        string keyStr = keys[currentCategory, keyIndex];

        // シフト状態に応じて大文字/小文字を決定
        char inputChar = isShift ? char.ToUpper(keyStr[0]) : char.ToLower(keyStr[0]);

        // InputFieldへの入力
        if (inputField != null)
        {
            inputField.text += inputChar;
        }

        // InputManagerへの入力通知
        if (inputManager != null)
        {
            inputManager.ProcessInput(inputChar);
        }

        // 外部へのイベント通知
        OnCharacterInputted?.Invoke(inputChar.ToString());

        // カテゴリ選択状態をリセット
        currentCategory = -1;
    }

    // スペースキー入力処理
    public void OnSpace()
    {
        if (inputField != null) inputField.text += ' ';
        if (inputManager != null) inputManager.ProcessInput(' ');
        OnCharacterInputted?.Invoke(" ");
    }

    // バックスペース入力処理
    void OnBackspace()
    {
        if (inputField != null && inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
        // InputManager側にはバックスペースの通知は行わない（仕様に合わせて調整してください）
    }
}
using UnityEngine;
using System.Collections.Generic;
using System;

public class JapaneseFlickController : MonoBehaviour
{
    [Header("References")]
    public GestureInput gestureManager;
    public InputManager inputManagerTarget;

    // ■■■ 案2配列定義 (行統合型) ■■■

    // カテゴリ名（表示用）
    private readonly string[] categories = { "あ/か行", "さ/た行", "な/は行", "ま/や行", "ら/わ行", "機能・記号" };

    // 人差し指用マップ (ペアの左側: あさなまら行)
    // 方向順序: →(あ段), ←(い段), ↑(う段), ↓(え段), ●(お段), ◎(予備)
    private readonly string[,] indexKeys =
    {
        { "あ", "い", "う", "え", "お", "ー" }, // 中指→ (あ行)
        { "さ", "し", "す", "せ", "そ", "・" }, // 中指← (さ行)
        { "な", "に", "ぬ", "ね", "の", "、" }, // 中指↑ (な行)
        { "ま", "み", "む", "め", "も", "。" }, // 中指↓ (ま行)
        { "ら", "り", "る", "れ", "ろ", "？" }, // 中指● (ら行)
        { "", "", "", "", "", "" }             // 中指◎ (機能・予備)
    };

    // 薬指用マップ (ペアの右側: かたはやわ行 + 機能)
    private readonly string[,] ringKeys =
    {
        { "か", "き", "く", "け", "こ", "っ" }, // 中指→ (か行)
        { "た", "ち", "つ", "て", "と", "！" }, // 中指← (た行)
        { "は", "ひ", "ふ", "へ", "ほ", "ん" }, // 中指↑ (は行 + ん)
        { "や", "「", "ゆ", "」", "よ", "…"},  // 中指↓ (や行変則)
        { "わ", "を", "（", "）", "～", "" },   // 中指● (わ行変則)
        // 特殊キー定義: [濁]濁点, [半]半濁点, [小]小文字化
        { "[濁]", "[半]", "[小]", "゛", "゜", "ゃ" } // 中指◎ (機能)
    };

    // 現在どちらのマップを使うか
    private bool useRingMap = false;
    // 最後に入力した文字（濁点処理用）
    private string lastInputChar = "";

    // 外部連携用（現在のマップを返す）
    public string[,] KeyLayout => useRingMap ? ringKeys : indexKeys;
    public event Action<string> OnCharacterInputted;

    private int currentCategory = -1;
    private Dictionary<string, string> dakutenMap;
    private Dictionary<string, string> handakutenMap;
    private Dictionary<string, string> smallCharMap;


    void Start()
    {
        InitializeModifierMaps();

        if (inputManagerTarget == null) inputManagerTarget = FindObjectOfType<InputManager>();

        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += OnCategorySelected;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;
            
            // 人差し指/薬指の検知でマップを切り替える
            gestureManager.OnLowercase += OnIndexFingerStart; // 人差し指開始
            gestureManager.OnUppercase += OnRingFingerStart;  // 薬指開始

            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
            {
                gestureManager.GetType().GetEvent("OnSpaceKey").AddEventHandler(gestureManager, new Action(OnSpaceInput));
            }
        }
    }

    void OnIndexFingerStart() { useRingMap = false; Debug.Log("Map: Index Finger (あさなまら)"); }
    void OnRingFingerStart() { useRingMap = true; Debug.Log("Map: Ring Finger (かたはやわ)"); }

    void OnCategorySelected(Vector3 start, Vector3 end)
    {
        currentCategory = DirectionalSelector.GetDirectionIndex(start, end);
        // Debug.Log($"カテゴリ選択: {categories[currentCategory]}");
    }

    void OnKeySelected(Vector3 start, Vector3 end)
    {
        if (currentCategory < 0) return;

        int keyIndex = DirectionalSelector.GetDirectionIndex(start, end);
        
        // 使う指に応じて配列を切り替える
        string[,] currentMap = useRingMap ? ringKeys : indexKeys;
        string inputChar = currentMap[currentCategory, keyIndex];

        if (string.IsNullOrEmpty(inputChar)) {
            currentCategory = -1;
            return;
        }

        // 特殊機能キーの処理
        if (inputChar.StartsWith("["))
        {
            ProcessModifier(inputChar);
        }
        else
        {
            // 通常文字入力
            OutputString(inputChar);
        }

        currentCategory = -1;
    }

    // 文字出力処理
    private void OutputString(string text)
    {
        if (inputManagerTarget != null)
        {
            inputManagerTarget.AppendCharacter(text);
            OnCharacterInputted?.Invoke(text);
            lastInputChar = text; // 濁点用に記録
        }
    }

    // 濁点・半濁点・小文字化の処理
    private void ProcessModifier(string modifier)
    {
        if (string.IsNullOrEmpty(lastInputChar) || inputManagerTarget == null) return;

        Dictionary<string, string> targetMap = null;
        switch (modifier)
        {
            case "[濁]": targetMap = dakutenMap; break;
            case "[半]": targetMap = handakutenMap; break;
            case "[小]": targetMap = smallCharMap; break;
        }

        if (targetMap != null && targetMap.ContainsKey(lastInputChar))
        {
            // バックスペースで前の文字を消して、変換後の文字を入力
            inputManagerTarget.Backspace();
            string newChar = targetMap[lastInputChar];
            OutputString(newChar);
        }
    }

    void OnBackspace()
    {
        if (inputManagerTarget != null)
        {
            inputManagerTarget.Backspace();
            lastInputChar = ""; // 履歴クリア
        }
    }

    void OnSpaceInput()
    {
        OutputString(" ");
    }

    // 変換テーブル初期化
    private void InitializeModifierMaps()
    {
        dakutenMap = new Dictionary<string, string> {
            {"か", "が"}, {"き", "ぎ"}, {"く", "ぐ"}, {"け", "げ"}, {"こ", "ご"},
            {"さ", "ざ"}, {"し", "じ"}, {"す", "ず"}, {"せ", "ぜ"}, {"そ", "ぞ"},
            {"た", "だ"}, {"ち", "ぢ"}, {"つ", "づ"}, {"て", "で"}, {"と", "ど"},
            {"は", "ば"}, {"ひ", "び"}, {"ふ", "ぶ"}, {"へ", "べ"}, {"ほ", "ぼ"},
            {"う", "ゔ"}
        };
        handakutenMap = new Dictionary<string, string> {
            {"は", "ぱ"}, {"ひ", "ぴ"}, {"ふ", "ぷ"}, {"へ", "ぺ"}, {"ほ", "ぽ"}
        };
        smallCharMap = new Dictionary<string, string> {
            {"あ", "ぁ"}, {"い", "ぃ"}, {"う", "ぅ"}, {"え", "ぇ"}, {"お", "ぉ"},
            {"つ", "っ"}, {"や", "ゃ"}, {"ゆ", "ゅ"}, {"よ", "ょ"},
            {"わ", "ゎ"}
        };
    }
}

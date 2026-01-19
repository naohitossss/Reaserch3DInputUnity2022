using UnityEngine;
using System.Collections.Generic;
using System;

public class JapaneseFlickController : MonoBehaviour
{
    [Header("References")]
    public GestureInput gestureManager;
    public InputManager inputManagerTarget; // UI表示/バックスペース用

    private readonly string[,] indexKeys = {
        { "あ", "い", "う", "え", "お", "ー" }, { "さ", "し", "す", "せ", "そ", "・" },
        { "な", "に", "ぬ", "ね", "の", "、" }, { "ま", "み", "む", "め", "も", "。" },
        { "ら", "り", "る", "れ", "ろ", "？" }, { "", "", "", "", "", "" }
    };

    private readonly string[,] ringKeys = {
        { "か", "き", "く", "け", "こ", "っ" }, { "た", "ち", "つ", "て", "と", "！" },
        { "は", "ひ", "ふ", "へ", "ほ", "ん" }, { "や", "「", "ゆ", "」", "よ", "…"},
        { "わ", "を", "（", "）", "～", "" }, { "[濁]", "[半]", "[小]", "゛", "゜", "ゃ" }
    };

    private bool useRingMap = false;
    private string lastInputChar = "";
    private int currentCategory = -1;
    private Dictionary<string, string> dakutenMap;
    private Dictionary<string, string> handakutenMap;
    private Dictionary<string, string> smallCharMap;

    public event Action<string> OnCharacterInputted;

    void Start()
    {
        InitializeModifierMaps();
        if (inputManagerTarget == null) inputManagerTarget = FindObjectOfType<InputManager>();

        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += (idx) => currentCategory = idx;
            gestureManager.OnKeySelected += OnKeySelected;
            gestureManager.OnBackspace += OnBackspace;
            gestureManager.OnLowercase += () => useRingMap = false;
            gestureManager.OnUppercase += () => useRingMap = true;
            if (gestureManager.GetType().GetEvent("OnSpaceKey") != null)
                gestureManager.GetType().GetEvent("OnSpaceKey").AddEventHandler(gestureManager, new Action(OnSpaceInput));
        }
    }

    void OnKeySelected(int keyIndex)
    {
        if (currentCategory < 0) return;
        string[,] currentMap = useRingMap ? ringKeys : indexKeys;
        if (currentCategory >= currentMap.GetLength(0) || keyIndex < 0 || keyIndex >= currentMap.GetLength(1)) return;

        string inputChar = currentMap[currentCategory, keyIndex];
        if (string.IsNullOrEmpty(inputChar)) return;

        if (inputChar.StartsWith("[")) ProcessModifier(inputChar);
        else
        {
            // 文字を通知（TutorialManagerが統計を判断）
            OutputString(inputChar);
        }
        currentCategory = -1;
    }

    private void OutputString(string text)
    {
        if (inputManagerTarget != null)
        {
            inputManagerTarget.AppendCharacter(text);
            OnCharacterInputted?.Invoke(text);
            lastInputChar = text;
        }
    }

    private void ProcessModifier(string modifier)
    {
        if (string.IsNullOrEmpty(lastInputChar) || inputManagerTarget == null) return;
        Dictionary<string, string> targetMap = modifier switch { "[濁]" => dakutenMap, "[半]" => handakutenMap, "[小]" => smallCharMap, _ => null };
        if (targetMap != null && targetMap.ContainsKey(lastInputChar))
        {
            inputManagerTarget.Backspace();
            OutputString(targetMap[lastInputChar]);
        }
    }

    void OnBackspace() { if (inputManagerTarget != null) { inputManagerTarget.Backspace(); lastInputChar = ""; } }
    void OnSpaceInput() { OutputString(" "); }

    private void InitializeModifierMaps()
    {
        dakutenMap = new Dictionary<string, string> { {"か","が"},{"き","ぎ"},{"く","ぐ"},{"け","げ"},{"こ","ご"},{"さ","ざ"},{"し","じ"},{"す","ず"},{"せ","ぜ"},{"そ","ぞ"},{"た","だ"},{"ち","ぢ"},{"つ","づ"},{"て","で"},{"と","ど"},{"は","ば"},{"ひ","び"},{"ふ","ぶ"},{"へ","べ"},{"ほ","ぼ"},{"う","ゔ"} };
        handakutenMap = new Dictionary<string, string> { {"は","ぱ"},{"ひ","ぴ"},{"ふ","ぶ"},{"へ","べ"},{"ほ","ぼ"} };
        smallCharMap = new Dictionary<string, string> { {"あ","ぁ"},{"い","ぃ"},{"う","ぅ"},{"え","ぇ"},{"お","ぉ"},{"つ","っ"},{"や","ゃ"},{"ゆ","ゅ"},{"よ","ょ"},{"わ","ゎ"} };
    }
}
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using TMPro; // TextMeshProを扱うために必要

public class AdvancedCubeController : MonoBehaviour
{
    [Header("文字設定")]
    [SerializeField] private string indexPinchChar = "A";
    [SerializeField] private string middlePinchChar = "B";
    [SerializeField] private string ringPinchChar = "C";
    [SerializeField] private string pinkyPinchChar = "D";

    [Header("UI参照")]
    [SerializeField] private TextMeshPro textDisplay; // 文字を表示するTextMeshPro

     private Renderer cubeRenderer;
    private Color originalColor;

    void Start()
    {
        cubeRenderer = GetComponent<Renderer>();
        originalColor = cubeRenderer.material.color;
        // 起動時は文字を消しておく
        if(textDisplay != null) textDisplay.text = "";
    }

    // InteractionManagerから呼び出される公開メソッド
    public void OnPinchPerformed(GestureManager.GestureType gesture)
    {
        string input = "";

        // ジェスチャーに応じて割り当てられた文字列を取得
        switch (gesture)
        {
            case GestureManager.GestureType.IndexPinch:
                input = indexPinchChar;
                break;
            case GestureManager.GestureType.MiddlePinch:
                input = middlePinchChar;
                break;
            case GestureManager.GestureType.RingPinch:
                input = ringPinchChar;
                break;
            case GestureManager.GestureType.PinkyPinch:
                input = pinkyPinchChar;
                break;
        }

        // 取得した文字列に応じてInputManagerの適切なメソッドを呼び出す
        if (!string.IsNullOrEmpty(input))
        {
            // 同時にフィードバックとして文字を表示
            UpdateFeedbackText(input);

            switch (input.ToUpper()) // 大文字小文字を区別しないように
            {
                case "SHIFT":
                    InputManager.instance.ToggleShift();
                    break;
                case "SPACE":
                    InputManager.instance.Space();
                    break;
                case "BACKSPACE":
                    InputManager.instance.Backspace();
                    break;
                default:
                    // 上記以外は通常の文字として処理
                    InputManager.instance.AppendCharacter(input);
                    break;
            }
        }
    }
    
    // フィードバックテキストを更新するメソッド
    private void UpdateFeedbackText(string inputText)
    {
        if (textDisplay == null) return;

        // 特殊キーには分かりやすい記号を割り当てる
        switch (inputText.ToUpper())
        {
            case "SHIFT":
                textDisplay.text = "⇧"; // Shift symbol
                break;
            case "SPACE":
                textDisplay.text = "␣"; // Space symbol
                break;

            case "BACKSPACE":
                textDisplay.text = "⌫"; // Backspace symbol
                break;
            default:
                textDisplay.text = inputText;
                break;
        }
    }

    // OnTriggerEnterとOnTriggerExitは変更なし
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<IHand>() != null)
        {
            InteractionManager.instance.SetTargetCube(this);
            cubeRenderer.material.color = Color.yellow;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<IHand>() != null)
        {
            InteractionManager.instance.ClearTargetCube(this);
            cubeRenderer.material.color = originalColor;
            // 手が離れたらフィードバックの文字を消す
            if(textDisplay != null) textDisplay.text = "";
        }
    }
}
using UnityEngine;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class KeyCategory
{
    public string categoryName;
    [Tooltip("Right, Left, Up, Down, Forward, Back")]
    public string[] keys = new string[6];
}
public class GestureInputManager : MonoBehaviour
{
    [Header("Hands to Track")]
    public Hand leftHand;
    public Hand rightHand;

    [Header("UI Feedback")]
    public TextMeshProUGUI statusTextUI;

    [Header("Sensitivity")]
    [Tooltip("Minimum distance in meters to register as a swipe.")]
    public float swipeThreshold = 0.01f;

    [Header("Key Mapping")]
    [Tooltip("Set up 6 categories, each with 6 keys.")]
    public List<KeyCategory> keyCategories = new List<KeyCategory>();

    // --- Internal State Management ---
    private enum State { Idle, WaitingForCategorySwipe, WaitingForKeySwipe }
    private State currentState = State.Idle;

    private Hand activeHand;
    private Vector3 pinchStartPosition;
    private int selectedCategoryIndex = -1;

    private bool wasLeftMiddlePinching = false;
    private bool wasRightMiddlePinching = false;
    private bool wasActiveIndexPinching = false;

    void Start()
    {
        if (keyCategories.Count == 0)
        {
            Debug.LogError("Key Categories are not set up in the Inspector!");
        }
        UpdateUI("Waiting");
    }

void Update()
{
    if (leftHand == null || rightHand == null) return;

    // --- 現在のフレームのピンチ状態を全て取得 ---
    bool isLeftMiddlePinching = leftHand.GetFingerIsPinching(HandFinger.Middle);
    bool isRightMiddlePinching = rightHand.GetFingerIsPinching(HandFinger.Middle);
    
    // --- State Machine Logic ---
    switch (currentState)
    {
        case State.Idle:
            if (isLeftMiddlePinching && !wasLeftMiddlePinching) StartInteraction(leftHand);
            else if (isRightMiddlePinching && !wasRightMiddlePinching) StartInteraction(rightHand);
            break;

        case State.WaitingForCategorySwipe:
            bool isActiveIndexPinching = (activeHand != null) && activeHand.GetFingerIsPinching(HandFinger.Index);

            // 人差し指ピンチが始まった瞬間にカテゴリを決定
            if (isActiveIndexPinching && !wasActiveIndexPinching) 
            {
                Vector3 moveVector = activeHand.transform.position - pinchStartPosition;
                selectedCategoryIndex = GetDirectionIndex(moveVector);

                if (selectedCategoryIndex != -1 && selectedCategoryIndex < keyCategories.Count)
                {
                    currentState = State.WaitingForKeySwipe;
                    pinchStartPosition = activeHand.transform.position; // キー選択の始点を更新
                    
                    string categoryName = keyCategories[selectedCategoryIndex].categoryName;
                    UpdateUI($"Category: {categoryName} | Select Key");
                }
                else
                {
                    ResetState(); // 移動量が足りなければリセット
                }
            }
            
            // もし人差し指ピンチの前に中指ピンチを離してしまったらキャンセル
            bool isMiddleHandPinching = (activeHand == leftHand) ? isLeftMiddlePinching : isRightMiddlePinching;
            if (!isMiddleHandPinching)
            {
                ResetState();
            }
            break;

        case State.WaitingForKeySwipe:
            bool isIndexHandPinching = (activeHand != null) && activeHand.GetFingerIsPinching(HandFinger.Index);

            // 人差し指ピンチが離された瞬間にキーを決定
            if (!isIndexHandPinching && wasActiveIndexPinching)
            {
                int keyIndex = GetDirectionIndex(activeHand.transform.position - pinchStartPosition);
                if (keyIndex != -1)
                {
                    string key = keyCategories[selectedCategoryIndex].keys[keyIndex];
                    HandleKeyPress(key);
                }
                ResetState();
            }

            // 中指ピンチでキャンセル
            if ((isLeftMiddlePinching && !wasLeftMiddlePinching && activeHand == leftHand) ||
                (isRightMiddlePinching && !wasRightMiddlePinching && activeHand == rightHand))
            {
                ResetState();
            }

            // wasActiveIndexPinchingはここで更新
            wasActiveIndexPinching = isIndexHandPinching;
            break;
    }

    // --- 次のフレームのために今の状態を保存 ---
    wasLeftMiddlePinching = isLeftMiddlePinching;
    wasRightMiddlePinching = isRightMiddlePinching;

    // activeHandがnullでない場合のみwasActiveIndexPinchingを更新
    if (activeHand != null)
    {
        // WaitingForCategorySwipe中は人差し指の状態を追跡しないため、ここで更新
        if (currentState != State.WaitingForKeySwipe)
        {
            wasActiveIndexPinching = activeHand.GetFingerIsPinching(HandFinger.Index);
        }
    }
    else
    {
        wasActiveIndexPinching = false;
    }
}

    // --- Helper Methods ---

    void StartInteraction(Hand hand)
    {
        activeHand = hand;
        pinchStartPosition = activeHand.transform.position;
        currentState = State.WaitingForCategorySwipe;
        UpdateUI("Select Category");
    }

    private int GetDirectionIndex(Vector3 moveVector)
    {
        if (moveVector.magnitude < swipeThreshold)
        {
            UpdateUI($"Move More");
            return -1;
        }
        float absX = Mathf.Abs(moveVector.x);
        float absY = Mathf.Abs(moveVector.y);
        float absZ = Mathf.Abs(moveVector.z);
        if (absX > absY && absX > absZ) return (moveVector.x > 0) ? 0 : 1; // Right/Left
        if (absY > absX && absY > absZ) return (moveVector.y > 0) ? 2 : 3; // Up/Down
        return (moveVector.z > 0) ? 4 : 5; // Forward/Back
    }

    void ResetState()
    {
        currentState = State.Idle;
        activeHand = null;
        UpdateUI("Reset");
    }

    void UpdateUI(string status)
    {
        if (statusTextUI != null) statusTextUI.text = status;
    }

    void HandleKeyPress(string key)
    {
        if (InputManager.instance == null) return;

        if (key == "BS") InputManager.instance.Backspace();
        else if (key == "SPACE") InputManager.instance.Space();
        else if (key == "SHIFT") InputManager.instance.ToggleShift();
        else InputManager.instance.AppendCharacter(key);
    }
}
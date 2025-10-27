using UnityEngine;
using System;

public class GestureInput : MonoBehaviour
{
    public enum InputPhase
    {
        Idle,
        CategoryReady,
        KeySelecting
    }

    public InputPhase CurrentPhase { get; private set; } = InputPhase.Idle;

    [Header("Hand Tracking")]
    public OVRHand hand;
    public OVRSkeleton skeleton;

    [Header("World Settings")]
    [Tooltip("ワールド内の基準点。入力操作の中心となる位置。")]
    public Transform worldCenter;

    [Header("Parameters")]
    public float moveThreshold = 0.04f;
    public bool debugLog = true;

    [Header("UI Settings")]
    [Tooltip("UIブロックの生成位置。手の操作がしやすい場所に設定。")]
    public Vector3 uiPosition = new Vector3(0f, 1.0f, 0.5f);  // Z位置を調整

    private Transform indexTip;
    private Transform middleTip;
    private bool isInitialized;

    private bool prevMiddlePinch;
    private bool prevIndexPinch;

    private Vector3 categoryStartPos;
    private Vector3 keyStartPos;

    public event Action<Vector3, Vector3> OnCategorySelected;
    public event Action<Vector3, Vector3> OnKeySelected;
    public event Action OnBackspace;
    public event Action OnUppercase;
    public event Action OnLowercase;
    public event Action OnSpace;

    private bool prevFistGesture;
    private bool prevGoodGesture;
    private bool prevPinkyGesture;
    private bool prevWaveGesture;

    private Vector3 previousHandPosition;
    private float waveDetectionTime = 0.5f; // 検出に必要な時間
    private float waveStartTime;

    private int waveDirectionChanges = 0; // 左右の移動回数
    private float lastWaveDirection = 0; // 前回の移動方向

    void Start()
    {
        InitializeBones();
    }

    void InitializeBones()
    {
        if (skeleton == null || !skeleton.IsDataValid) return;

        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                indexTip = bone.Transform;
            if (bone.Id == OVRSkeleton.BoneId.Hand_MiddleTip)
                middleTip = bone.Transform;
        }

        if (indexTip && middleTip)
        {
            isInitialized = true;
            if (debugLog) Debug.Log("HandTracker initialized");
        }
    }

    void Update()
    {
        if (!isInitialized)
        {
            InitializeBones();
            return;
        }

        bool middleNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        bool indexNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        bool middlePinchDown = middleNow && !prevMiddlePinch;
        bool middlePinchUp = !middleNow && prevMiddlePinch;
        bool indexPinchDown = indexNow && !prevIndexPinch;
        bool indexPinchUp = !indexNow && prevIndexPinch;

        // === 新しいジェスチャーの検出 ===
        bool isFist = IsFistGesture();
        bool isGood = IsGoodGesture();
        bool isPinky = IsPinkyGesture();
        bool isWave = IsWaveGesture();

        if (isWave && !prevWaveGesture)
        {
            OnBackspace?.Invoke();
            if (debugLog) Debug.Log("🔙 Backspace triggered");
        }

        if (isGood && !prevGoodGesture)
        {
            OnUppercase?.Invoke();
            if (debugLog) Debug.Log("🔠 Uppercase triggered");
        }

        if (isPinky && !prevPinkyGesture)
        {
            OnLowercase?.Invoke();
            if (debugLog) Debug.Log("🔡 Lowercase triggered");
        }

        if (isFist && !prevFistGesture)
        {
            OnSpace?.Invoke();
            if (debugLog) Debug.Log("␣ Space triggered");
        }

        // === 各フェーズ ===
        switch (CurrentPhase)
        {
            case InputPhase.Idle:
                if (middlePinchDown)
                {
                    categoryStartPos = middleTip.position;
                    CurrentPhase = InputPhase.CategoryReady;
                    if (debugLog) Debug.Log("🟢 Category ready at world pos " + categoryStartPos);
                }
                break;

            case InputPhase.CategoryReady:
                if (indexPinchDown)
                {
                    Vector3 categoryEndPos = indexTip.position;
                    float distance = Vector3.Distance(categoryStartPos, categoryEndPos);

                    if (distance > moveThreshold)
                    {
                        int directionIndex = DirectionalSelector.GetDirectionIndex(categoryStartPos, categoryEndPos);
                        if (directionIndex != -1)
                        {
                            if (debugLog) Debug.Log($"Direction: {DirectionalSelector.GetDirectionName(directionIndex)}");
                            OnCategorySelected?.Invoke(categoryStartPos, categoryEndPos);
                            keyStartPos = categoryEndPos;
                            CurrentPhase = InputPhase.KeySelecting;
                        }
                    }
                }
                break;

            case InputPhase.KeySelecting:
                if (indexPinchUp)
                {
                    Vector3 keyEndPos = indexTip.position;
                    if (Vector3.Distance(keyStartPos, keyEndPos) > moveThreshold)
                    {
                        OnKeySelected?.Invoke(keyStartPos, keyEndPos);
                        if (debugLog) Debug.Log("🔡 Key Selected");
                    }
                    ResetState();
                }
                break;
        }

        prevMiddlePinch = middleNow;
        prevIndexPinch = indexNow;
        prevFistGesture = isFist;
        prevGoodGesture = isGood;
        prevPinkyGesture = isPinky;
        prevWaveGesture = isWave;
    }

    private void ResetState()
    {
        CurrentPhase = InputPhase.Idle;
    }

    private bool IsFistGesture()
    {
        return hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
               hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
               hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) &&
               hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);
    }

    private bool IsGoodGesture()
    {
        return hand.GetFingerIsPinching(OVRHand.HandFinger.Thumb) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);
    }

    private bool IsPinkyGesture()
    {
        return hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
               !hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);
    }

    private bool IsWaveGesture()
    {
        // 他のフェーズ中は手を振るジェスチャーを無効化
        if (CurrentPhase != InputPhase.Idle)
        {
            return false;
        }

        Vector3 currentHandPosition = hand.PointerPose.position;

        // 初回の位置を記録
        if (previousHandPosition == Vector3.zero)
        {
            previousHandPosition = currentHandPosition;
            waveStartTime = Time.time;
            return false;
        }

        // 現在の移動方向を計算
        float currentDirection = currentHandPosition.x - previousHandPosition.x;

        // 移動方向が変わった場合
        if (Mathf.Sign(currentDirection) != Mathf.Sign(lastWaveDirection) && Mathf.Abs(currentDirection) > moveThreshold)
        {
            waveDirectionChanges++;
            lastWaveDirection = currentDirection;

            // 一定回数以上方向が変わったらジェスチャーを検出
            if (waveDirectionChanges >= 4) // 例: 左右2回ずつで4回
            {
                waveDirectionChanges = 0; // リセット
                previousHandPosition = Vector3.zero; // リセット
                return true;
            }
        }

        // 一定時間内に方向変化がなければリセット
        if (Time.time - waveStartTime > waveDetectionTime)
        {
            waveDirectionChanges = 0;
            waveStartTime = Time.time;
        }

        previousHandPosition = currentHandPosition;
        return false;
    }
}


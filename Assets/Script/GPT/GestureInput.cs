using UnityEngine;
using System;

public class GestureInput : MonoBehaviour
{
    public enum InputPhase
    {
        Idle,
        CategoryReady,     // 中指ピンチ開始 -> 中指ピンチ中
        CategorySelected,  // 中指ピンチ解除 -> キー選択待機中
        KeySelecting       // 人差し指/薬指曲げ開始 -> 曲げ維持中
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

    // ✅ 追加: 各指の曲がり具合の閾値設定
    [Header("Gesture Settings (Bend Thresholds)")]
    [Tooltip("人差し指（小文字）の曲がり判定閾値")]
    [Range(0.0f, 1.0f)]
    public float indexBendThreshold = 0.6f;
    [Tooltip("薬指（大文字）の曲がり判定閾値")]
    [Range(0.0f, 1.0f)]
    public float ringBendThreshold = 0.6f;
    [Tooltip("小指（スペース）の曲がり判定閾値")]
    [Range(0.0f, 1.0f)]
    public float pinkyBendThreshold = 0.8f;

    [Header("UI Settings")]
    [Tooltip("UIブロックの生成位置。手の操作がしやすい場所に設定。")]
    public Vector3 uiPosition = new Vector3(0f, 1.0f, 0.5f);  // Z位置を調整

    private Transform indexTip;
    private Transform middleTip;
    private Transform thumbTip;
    private Transform pinkyTip;
    private Transform ringTip;
    private bool isInitialized;

    private bool prevMiddlePinch;
    
    // ✅ 追加: 前フレームの曲がり状態を保持する変数
    private bool prevIndexBent;
    private bool prevRingBent;
    private bool prevPinkyBent;

    private Vector3 categoryStartPos;
    private Vector3 categoryEndPosAtMiddlePinchUp;
    private Vector3 keyStartPos;

    public event Action<Vector3, Vector3> OnCategorySelected;
    public event Action<Vector3, Vector3> OnKeySelected;
    public event Action OnBackspace;
    public event Action OnUppercase;
    public event Action OnLowercase;
    public event Action OnSpace;
    public event Action OnSpaceKey; // スペースキー入力イベント

    // バックスペース用ジェスチャーの状態
    private Vector3 previousHandPosition;
    private float waveDetectionTime = 1f; // 検出に必要な時間
    private float waveStartTime;
    private int waveDirectionChanges = 0; // 左右の移動回数
    private float lastWaveDirection = 0; // 前回の移動方向

    [Header("Wave Settings")]
    [Tooltip("手を振る動作で必要な方向変化回数")]
    public int waveRequiredChanges = 4;

    // キャッシュ用の変数を追加 (未使用だが残しておく)
    private Vector3 lastIndexPos;
    private Vector3 lastMiddlePos;
    private float updateThreshold = 0.001f; // 位置更新の閾値
    [SerializeField]
    private float gestureUpdateInterval = 0.016f; // 100msごとに更新 (未使用)
    private float nextGestureUpdateTime = 0f;

    [SerializeField]
    private InputManager inputManager; // InputManagerへの参照を追加

    void Start()
    {
        InitializeBones();

        // InputManagerの取得
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManager>();
            if (inputManager == null)
            {
                Debug.LogError("InputManager not found!");
            }
        }
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

            if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip)
                thumbTip = bone.Transform;
            if (bone.Id == OVRSkeleton.BoneId.Hand_PinkyTip)
                pinkyTip = bone.Transform;
            if (bone.Id == OVRSkeleton.BoneId.Hand_RingTip)
                ringTip = bone.Transform;
        }

        if (indexTip && middleTip && ringTip && pinkyTip)
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

        // --- 中指のピンチ（接触）判定（カテゴリ選択用） ---
        bool middleNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        bool middlePinchDown = middleNow && !prevMiddlePinch;
        bool middlePinchUp = !middleNow && prevMiddlePinch;

        // --- ✅ 修正: 指の曲がり具合による判定 ---
        // 各指のピンチ強度（曲がり具合 0.0〜1.0）を取得
        float indexStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float ringStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Ring);
        float pinkyStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);

        // 閾値判定
        bool indexBentNow = indexStrength > indexBendThreshold;
        bool ringBentNow = ringStrength > ringBendThreshold;
        bool pinkyBentNow = pinkyStrength > pinkyBendThreshold;

        // 曲げ開始（Down）と解除（Up）の検出
        bool indexBentDown = indexBentNow && !prevIndexBent;
        bool indexBentUp = !indexBentNow && prevIndexBent;
        bool ringBentDown = ringBentNow && !prevRingBent;
        bool ringBentUp = !ringBentNow && prevRingBent;
        bool pinkyBentDown = pinkyBentNow && !prevPinkyBent;

        // --- 小指の曲げでスペースキー入力 ---
        // ✅ 修正: 曲がり具合が 0.8 を超えた瞬間に反応
        if (pinkyBentDown)
        {
            HandleSpaceKey();
            // if (debugLog) Debug.Log($"Pinky bend strength: {pinkyStrength:F2}"); // デバッグ用
        }

        // === 各フェーズ ===
        switch (CurrentPhase)
        {
            case InputPhase.Idle:
                if (middlePinchDown) // カテゴリ選択は引き続き中指の接触ピンチで開始
                {
                    categoryStartPos = middleTip.position;
                    CurrentPhase = InputPhase.CategoryReady;
                    if (debugLog) Debug.Log("🟢 Category gesture started (middle pinch down)");
                }
                break;

            case InputPhase.CategoryReady:
                if (middlePinchUp) // 中指ピンチ解除でカテゴリ決定
                {
                    categoryEndPosAtMiddlePinchUp = middleTip.position;
                    float distance = Vector3.Distance(categoryStartPos, categoryEndPosAtMiddlePinchUp);

                    if (distance > moveThreshold)
                    {
                        CurrentPhase = InputPhase.CategorySelected;
                        if (debugLog) Debug.Log("✅ Category direction recorded. Awaiting key gesture.");
                    }
                    else
                    {
                        ResetState();
                        if (debugLog) Debug.LogWarning("Category gesture too small. Resetting state.");
                    }
                }
                break;

            case InputPhase.CategorySelected:
                // ✅ 修正: 人差し指または薬指の「曲げ開始」を待つ
                if (indexBentDown || ringBentDown)
                {
                    if (indexBentDown)
                    {
                        OnLowercase?.Invoke(); // 小文字モード
                        keyStartPos = indexTip.position;
                        if (debugLog) Debug.Log("Key gesture started with Index Bend (Lowercase)");
                    }
                    else // ringBentDown
                    {
                        OnUppercase?.Invoke(); // 大文字モード
                        keyStartPos = ringTip.position;
                        if (debugLog) Debug.Log($"Key gesture started with Ring Bend (Uppercase, strength: {ringStrength:F2})");
                    }
                    CurrentPhase = InputPhase.KeySelecting;
                }
                break;

            case InputPhase.KeySelecting:
                // ✅ 修正: 人差し指または薬指の「曲げ解除」を待つ
                if (indexBentUp || ringBentUp)
                {
                    Vector3 keyEndPos;
                    // どちらの指を解除したか判定（前回曲がっていた方の指を使用）
                    if (prevIndexBent)
                    {
                        keyEndPos = indexTip.position;
                    }
                    else // prevRingBent
                    {
                        keyEndPos = ringTip.position;
                    }
                    
                    if (Vector3.Distance(keyStartPos, keyEndPos) > moveThreshold)
                    {
                        OnCategorySelected?.Invoke(categoryStartPos, categoryEndPosAtMiddlePinchUp);
                        OnKeySelected?.Invoke(keyStartPos, keyEndPos);
                        if (debugLog) Debug.Log("🔡 Key Selected (Bend Up)");
                    }
                    else
                    {
                        if (debugLog) Debug.LogWarning("Key gesture too small. Not selecting key.");
                    }
                    ResetState();
                }
                break;
        }

        // 状態更新
        prevMiddlePinch = middleNow;
        // ✅ 追加: 曲がり状態を更新
        prevIndexBent = indexBentNow;
        prevRingBent = ringBentNow;
        prevPinkyBent = pinkyBentNow;

        // バックスペース判定（変更なし）
        if (IsWaveGesture())
        {
            OnBackspace?.Invoke();
            if (debugLog) Debug.Log("🔙 Backspace triggered");
        }
    }

    private void ResetState()
    {
        CurrentPhase = InputPhase.Idle;
        categoryStartPos = Vector3.zero;
        categoryEndPosAtMiddlePinchUp = Vector3.zero;
        keyStartPos = Vector3.zero;
    }

    // ... (IsWaveGesture などのメソッドは変更なし) ...
    private bool IsWaveGesture()
    {
        if (CurrentPhase != InputPhase.Idle) return false;

        Vector3 currentHandPosition = hand.PointerPose.position;

        if (previousHandPosition == Vector3.zero)
        {
            previousHandPosition = currentHandPosition;
            waveStartTime = Time.time;
            waveDirectionChanges = 0; 
            lastWaveDirection = 0;    
            return false;
        }

        float currentDirection = currentHandPosition.x - previousHandPosition.x;

        if (Mathf.Sign(currentDirection) != Mathf.Sign(lastWaveDirection) && Mathf.Abs(currentDirection) > moveThreshold)
        {
            waveDirectionChanges++;
            lastWaveDirection = currentDirection;

            if (waveDirectionChanges >= waveRequiredChanges)
            {
                waveDirectionChanges = 0; 
                previousHandPosition = Vector3.zero; 
                return true;
            }
        }

        if (Time.time - waveStartTime > waveDetectionTime)
        {
            waveDirectionChanges = 0;
            waveStartTime = Time.time;
            lastWaveDirection = 0; 
        }

        previousHandPosition = currentHandPosition;
        return false;
    }

    private void HandleSpaceKey()
    {
        if (inputManager != null)
        {
            inputManager.Space();
            OnSpace?.Invoke();
            OnSpaceKey?.Invoke();
            // ✅ ログメッセージも修正
            if (debugLog) Debug.Log("Space key triggered by **pinky bend** gesture");
        }
        else
        {
            Debug.LogWarning("InputManager is not assigned!");
        }
    }
}
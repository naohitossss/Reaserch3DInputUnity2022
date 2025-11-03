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
    private Transform thumbTip;    // 追加
    private Transform pinkyTip;    // 追加
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
    public event Action OnSpaceKey; // スペースキー入力イベント

    private bool prevFistGesture;
    private bool prevGoodGesture;
    private bool prevPinkyGesture;
    private bool prevWaveGesture;

    private Vector3 previousHandPosition;
    private float waveDetectionTime = 0.5f; // 検出に必要な時間
    private float waveStartTime;

    private int waveDirectionChanges = 0; // 左右の移動回数
    private float lastWaveDirection = 0; // 前回の移動方向

    // 親指・小指用の振り検出用状態
    private Vector3 previousThumbPosition;
    private int thumbWaveDirectionChanges = 0;
    private float thumbLastWaveDirection = 0;
    private float thumbWaveStartTime;

    private Vector3 previousPinkyPosition;
    private int pinkyWaveDirectionChanges = 0;
    private float pinkyLastWaveDirection = 0;
    private float pinkyWaveStartTime;

    [Header("Wave Settings")]
    [Tooltip("親指振りで必要な方向変化回数")]
    public int thumbWaveRequiredChanges = 4;
    [Tooltip("小指振りで必要な方向変化回数")]
    public int pinkyWaveRequiredChanges = 4;

    private bool debugMode = false; // デバッグログを制御

    // キャッシュ用の変数を追加
    private Vector3 lastIndexPos;
    private Vector3 lastMiddlePos;
    private float updateThreshold = 0.001f; // 位置更新の閾値

    private float gestureUpdateInterval = 0.1f; // 100msごとに更新
    private float nextGestureUpdateTime = 0f;

    private bool isGood; // クラスレベルで定義
    private bool isWave;  // クラスレベルで定義
    private bool isPinky; // クラスレベルで定義
    private bool isFist;  // クラスレベルで定義

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

            // 追加: 親指と小指の先端を取得
            if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip)
                thumbTip = bone.Transform;
            if (bone.Id == OVRSkeleton.BoneId.Hand_PinkyTip)
                pinkyTip = bone.Transform;
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
            return; // 初期化が完了するまで他の処理をスキップ
        }

        bool middleNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);
        bool indexNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        bool pinkyNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky); // 小指のピンチを検出
        bool ringNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);  // 薬指のピンチを検出

        bool middlePinchDown = middleNow && !prevMiddlePinch;
        bool middlePinchUp = !middleNow && prevMiddlePinch;
        bool indexPinchDown = indexNow && !prevIndexPinch;
        bool indexPinchUp = !indexNow && prevIndexPinch;
        bool pinkyPinchDown = pinkyNow && !prevPinkyGesture; // 小指のピンチ開始を検出
        bool ringPinchDown = ringNow && !prevGoodGesture;    // 薬指のピンチ開始を検出

        // 小指のピンチでスペースキー入力をトリガー
        if (pinkyPinchDown)
        {
            HandleSpaceKey();
        }

        // 薬指のピンチで大文字小文字変換をトリガー
        if (ringPinchDown)
        {
            HandleCaseToggle();
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
        prevPinkyGesture = pinkyNow; // 小指のピンチ状態を更新
        prevGoodGesture = ringNow;  // 薬指のピンチ状態を更新
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

    private bool IsThumbWaveGesture()
    {
        if (CurrentPhase != InputPhase.Idle) return false;
        if (thumbTip == null) return false;

        Vector3 pos = thumbTip.position;

        if (previousThumbPosition == Vector3.zero)
        {
            previousThumbPosition = pos;
            thumbWaveStartTime = Time.time;
            thumbLastWaveDirection = 0;
            thumbWaveDirectionChanges = 0;
            return false;
        }

        float dir = pos.x - previousThumbPosition.x;

        if (Mathf.Abs(dir) > moveThreshold)
        {
            if (thumbLastWaveDirection == 0)
            {
                thumbLastWaveDirection = dir;
            }
            else if (Mathf.Sign(dir) != Mathf.Sign(thumbLastWaveDirection))
            {
                thumbWaveDirectionChanges++;
                thumbLastWaveDirection = dir;

                if (thumbWaveDirectionChanges >= thumbWaveRequiredChanges)
                {
                    thumbWaveDirectionChanges = 0;
                    previousThumbPosition = Vector3.zero;
                    thumbLastWaveDirection = 0;
                    return true;
                }
            }
        }

        if (Time.time - thumbWaveStartTime > waveDetectionTime)
        {
            thumbWaveDirectionChanges = 0;
            thumbWaveStartTime = Time.time;
            thumbLastWaveDirection = 0;
        }

        previousThumbPosition = pos;
        return false;
    }

    private bool IsPinkyWaveGesture()
    {
        if (CurrentPhase != InputPhase.Idle) return false;
        if (pinkyTip == null) return false;

        Vector3 pos = pinkyTip.position;

        if (previousPinkyPosition == Vector3.zero)
        {
            previousPinkyPosition = pos;
            pinkyWaveStartTime = Time.time;
            pinkyLastWaveDirection = 0;
            pinkyWaveDirectionChanges = 0;
            return false;
        }

        float dir = pos.x - previousPinkyPosition.x;

        if (Mathf.Abs(dir) > moveThreshold)
        {
            if (pinkyLastWaveDirection == 0)
            {
                pinkyLastWaveDirection = dir;
            }
            else if (Mathf.Sign(dir) != Mathf.Sign(pinkyLastWaveDirection))
            {
                pinkyWaveDirectionChanges++;
                pinkyLastWaveDirection = dir;

                if (pinkyWaveDirectionChanges >= pinkyWaveRequiredChanges)
                {
                    pinkyWaveDirectionChanges = 0;
                    previousPinkyPosition = Vector3.zero;
                    pinkyLastWaveDirection = 0;
                    return true;
                }
            }
        }

        if (Time.time - pinkyWaveStartTime > waveDetectionTime)
        {
            pinkyWaveDirectionChanges = 0;
            pinkyWaveStartTime = Time.time;
            pinkyLastWaveDirection = 0;
        }

        previousPinkyPosition = pos;
        return false;
    }

    private void HandleSpaceKey()
    {
        // スペースキー入力イベントをトリガー
        OnSpaceKey?.Invoke();

        // デバッグログ
        Debug.Log("Space key triggered by ring finger pinch");
    }

    private void HandleCaseToggle()
    {
        // 大文字小文字変換イベントをトリガー
        if (debugLog) Debug.Log("🔄 Case toggle triggered");
        // 実際の変換処理はここに実装
    }
}


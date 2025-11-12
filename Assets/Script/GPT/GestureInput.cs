using UnityEngine;
using System;

public class GestureInput : MonoBehaviour
{
    public enum InputPhase
    {
        Idle,
        CategoryReady,     // 中指ピンチ開始 -> 中指ピンチ中
        CategorySelected,  // 中指ピンチ解除 -> キー選択待機中 (Shift選択待ち)
        KeySelecting       // 人差し指/薬指ピンチ開始 -> ピンチ中
    }

    public InputPhase CurrentPhase { get; private set; } = InputPhase.Idle;

    [Header("Hand Tracking")]
    public OVRHand hand;
    public OVRSkeleton skeleton;

    [Header("World Settings")]
    [Tooltip("ワールド内の基準点。入力操作の中心となる位置。")]
    public Transform worldCenter;

    [Header("Parameters")]
    public float moveThreshold = 0.02f;
    public bool debugLog = true;
    private float updateThreshold = 0.005f; // 位置更新の閾値

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
    private bool prevIndexPinch;
    private bool prevPinkyPinch;
    private bool prevRingPinch;

    private Vector3 categoryStartPos;
    // カテゴリ選択の終了位置を保持するための変数 (中指ピンチ解除時の位置)
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
    private float waveDetectionTime = 0.5f; // 検出に必要な時間
    private float waveStartTime;
    private int waveDirectionChanges = 0; // 左右の移動回数
    private float lastWaveDirection = 0; // 前回の移動方向

    // 親指・小指用の振り検出用状態 (未使用だが残しておく)
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

    private bool debugMode = false; // デバッグログを制御 (未使用)

    // キャッシュ用の変数を追加 (未使用だが残しておく)
    private Vector3 lastIndexPos;
    private Vector3 lastMiddlePos;

    private float gestureUpdateInterval = 0.005f; // 100msごとに更新 (未使用)
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

        if (indexTip && middleTip && ringTip)
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
        bool pinkyNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);
        bool ringNow = hand.GetFingerIsPinching(OVRHand.HandFinger.Ring);

        bool middlePinchDown = middleNow && !prevMiddlePinch;
        bool middlePinchUp = !middleNow && prevMiddlePinch; // 中指ピンチ解除を検出
        bool indexPinchDown = indexNow && !prevIndexPinch;
        bool indexPinchUp = !indexNow && prevIndexPinch;   // 人差し指ピンチ解除を検出
        bool pinkyPinchDown = pinkyNow && !prevPinkyPinch;
        bool ringPinchDown = ringNow && !prevRingPinch;
        bool ringPinchUp = !ringNow && prevRingPinch;     // 薬指ピンチ解除を検出


        // 小指のピンチでスペースキー入力
        if (pinkyPinchDown)
        {
            HandleSpaceKey();
        }

        // === 各フェーズ ===
        switch (CurrentPhase)
        {
            case InputPhase.Idle:
                if (middlePinchDown)
                {
                    categoryStartPos = middleTip.position;
                    CurrentPhase = InputPhase.CategoryReady; // 中指ピンチ中
                    if (debugLog) Debug.Log("🟢 Category gesture started (middle pinch down)");
                }
                break;

            case InputPhase.CategoryReady: // 中指ピンチが続いている状態
                if (middlePinchUp) // 中指ピンチが解除されたらカテゴリを決定
                {
                    categoryEndPosAtMiddlePinchUp = middleTip.position; // 中指ピンチ解除時の位置を記録

                    float distance = Vector3.Distance(categoryStartPos, categoryEndPosAtMiddlePinchUp);

                    if (distance > moveThreshold)
                    {
                        // ここではまだ方向を判定しきらず、CategorySelectedに移行
                        // 方向の判定とOnCategorySelectedの発火はKeySelectingのピンチアップ時に行う
                        
                        CurrentPhase = InputPhase.CategorySelected; // カテゴリは選択済み、キー選択待ち
                        if (debugLog) Debug.Log("✅ Category direction recorded. Awaiting key gesture.");
                    }
                    else
                    {
                        // 移動が小さすぎる場合はリセット
                        ResetState();
                        if (debugLog) Debug.LogWarning("Category gesture too small. Resetting state.");
                    }
                }
                // 中指ピンチが続いている間は、何もしない
                break;

            case InputPhase.CategorySelected: // カテゴリ方向は決まっているが、Shiftとキー選択待ち
                // ここで人差し指または薬指のピンチダウンを待つ
                if (indexPinchDown || ringPinchDown)
                {
                    // Shift状態の確定
                    if (indexPinchDown)
                    {
                        OnLowercase?.Invoke(); // Shift Off/小文字モードへ
                        keyStartPos = indexTip.position; // keyStartPos を人差し指ピンチ開始位置に設定
                        if (debugLog) Debug.Log("Key gesture started with Index Pinch. (Lowercase)");
                    }
                    else // ringPinchDown
                    {
                        OnUppercase?.Invoke(); // Shift On/大文字モードへ
                        keyStartPos = ringTip.position; // keyStartPos を薬指ピンチ開始位置に設定
                        if (debugLog) Debug.Log("Key gesture started with Ring Pinch. (Uppercase)");
                    }
                    CurrentPhase = InputPhase.KeySelecting; // キー選択中
                }
                // このフェーズで中指が再度ピンチされた場合は、新しいカテゴリ選択を開始すべきか、あるいはエラーとするか？
                // 現状ではIdleに戻るまで待機。
                break;

            case InputPhase.KeySelecting: // 人差し指/薬指ピンチが続いている状態
                // ピンチを解除したらキーを決定
                if (indexPinchUp || ringPinchUp)
                {
                    Vector3 keyEndPos;
                    // どちらの指を解除したかに関わらず、最後にピンチしていた指の解除位置をキー選択の終点とする
                    if (prevIndexPinch) // indexPinchUpが真なので、前回はindexPinchだった
                    {
                        keyEndPos = indexTip.position;
                    }
                    else // ringPinchUpが真なので、前回はringPinchだった
                    {
                        keyEndPos = ringTip.position;
                    }
                    
                    if (Vector3.Distance(keyStartPos, keyEndPos) > moveThreshold)
                    {
                        // ここで中指ジェスチャーで得られた方向（categoryStartPos, categoryEndPosAtMiddlePinchUp）と
                        // 人差し指/薬指ジェスチャーで得られた方向（keyStartPos, keyEndPos）を組み合わせて
                        // InputControllerに通知する。
                        OnCategorySelected?.Invoke(categoryStartPos, categoryEndPosAtMiddlePinchUp); // 中指ジェスチャーで決定したカテゴリ
                        OnKeySelected?.Invoke(keyStartPos, keyEndPos);                             // 人差し指/薬指ジェスチャーで決定したキー

                        if (debugLog) Debug.Log("🔡 Key Selected (Pinch Up)");
                    }
                    else
                    {
                        if (debugLog) Debug.LogWarning("Key gesture too small. Not selecting key.");
                    }
                    ResetState(); // 入力完了後、状態をリセット
                }
                break;
        }

        prevMiddlePinch = middleNow;
        prevIndexPinch = indexNow;
        prevPinkyPinch = pinkyNow;
        prevRingPinch = ringNow;

        // 手を振るジェスチャーの検出（バックスペース用）
        if (IsWaveGesture())
        {
            OnBackspace?.Invoke();
            if (debugLog) Debug.Log("🔙 Backspace triggered");
        }
    }

    private void ResetState()
    {
        CurrentPhase = InputPhase.Idle;
        // その他の状態変数もリセットが必要ならここに追加
        categoryStartPos = Vector3.zero;
        categoryEndPosAtMiddlePinchUp = Vector3.zero; // 追加
        keyStartPos = Vector3.zero;
    }

    // IsFistGesture, IsGoodGesture, IsPinkyGesture は現在のシステムでは未使用

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
            waveDirectionChanges = 0; // 追加: 新しいジェスチャー開始時にリセット
            lastWaveDirection = 0;    // 追加: 新しいジェスチャー開始時にリセット
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
            lastWaveDirection = 0; // 追加: リセット時に方向もリセット
        }

        previousHandPosition = currentHandPosition;
        return false;
    }

    // 以下は未使用のジェスチャー検出だが、もし必要なら活性化
    /*
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
    */
    private void HandleSpaceKey()
    {
        // InputManagerが存在する場合のみスペース入力を実行
        if (inputManager != null)
        {
            inputManager.Space();
            OnSpace?.Invoke();
            OnSpaceKey?.Invoke();
            if (debugLog) Debug.Log("Space key triggered by pinky pinch");
        }
        else
        {
            Debug.LogWarning("InputManager is not assigned!");
        }
    }
}
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

    private Transform indexTip;
    private Transform middleTip;
    private bool isInitialized;

    private bool prevMiddlePinch;
    private bool prevIndexPinch;

    private Vector3 categoryStartPos;
    private Vector3 keyStartPos;

    public event Action<Vector3, Vector3> OnCategorySelected;
    public event Action<Vector3, Vector3> OnKeySelected;

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

        // === 各フェーズ ===
        switch (CurrentPhase)
        {
            case InputPhase.Idle:
                if (middlePinchUp)
                {
                    // ワールド座標に変換して記録
                    categoryStartPos = middleTip.position;
                    CurrentPhase = InputPhase.CategoryReady;
                    if (debugLog) Debug.Log("🟢 Category ready at world pos " + categoryStartPos);
                }
                break;

            case InputPhase.CategoryReady:
                if (indexPinchDown)
                {
                    Vector3 categoryEndPos = indexTip.position;
                    if (Vector3.Distance(categoryStartPos, categoryEndPos) > moveThreshold)
                    {
                        // 世界座標基準で方向判定
                        OnCategorySelected?.Invoke(categoryStartPos, categoryEndPos);

                        keyStartPos = categoryEndPos;
                        CurrentPhase = InputPhase.KeySelecting;
                        if (debugLog) Debug.Log("📁 Category Selected → KeySelecting");
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
    }

    private void ResetState()
    {
        CurrentPhase = InputPhase.Idle;
    }
}


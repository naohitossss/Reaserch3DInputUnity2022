using UnityEngine;
using Oculus.Interaction.Input;
using System;

public class GestureManager : MonoBehaviour
{
    public static GestureManager instance { get; private set; }
    public event Action<GestureType, Handedness> OnNewGesturePerformed;
    public enum GestureType { None, IndexPinch, MiddlePinch, RingPinch, PinkyPinch }

    private Hand leftHand, rightHand;
    private GestureType currentLeftGesture = GestureType.None, lastLeftGesture = GestureType.None;
    private GestureType currentRightGesture = GestureType.None, lastRightGesture = GestureType.None;

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        // HandManagerから参照を取得
        leftHand = HandManager.instance.leftHand;
        rightHand = HandManager.instance.rightHand;
    }

    // GestureManager.cs の Update メソッド内
void Update()
{
    currentLeftGesture = DetectHandGesture(leftHand);
    currentRightGesture = DetectHandGesture(rightHand);

    if (currentLeftGesture != GestureType.None && currentLeftGesture != lastLeftGesture)
    {
        // ▼▼▼ このログが表示されるか確認 ▼▼▼
        Debug.Log("GestureManager: Left Hand Gesture Detected - " + currentLeftGesture);
        OnNewGesturePerformed?.Invoke(currentLeftGesture, Handedness.Left);
    }
    
    if (currentRightGesture != GestureType.None /* && currentRightGesture != lastRightGesture */)
    {
        // このログがピンチしている間、毎フレーム表示されるか確認
        Debug.Log("GestureManager Update: ピンチ状態を継続して検知中！");
        OnNewGesturePerformed?.Invoke(currentRightGesture, Handedness.Right);
    }

    lastLeftGesture = currentLeftGesture;
    lastRightGesture = currentRightGesture;
}

    private GestureType DetectHandGesture(Hand hand)
{
    // --- 確認1: hand変数自体がnullでないか ---
    if (hand == null)
    {
        Debug.Log("DetectHandGesture: Error - hand変数がnullです。HandManagerを確認してください。");
        return GestureType.None;
    }

    // --- 確認2: 手がトラッキングされているか ---
    if (!hand.IsConnected)
    {
        // このログが大量に出る場合、トラッキングに問題があります
        Debug.LogWarning("DetectHandGesture: Warning - " + hand.name + " が接続されていません。");
        return GestureType.None;
    }

    // --- 確認3: ピンチ自体がSDKに認識されているか ---
    if (hand.GetFingerIsPinching(HandFinger.Index))
    {
        // このログが表示されれば、ピンチの認識は成功しています
        Debug.Log("DetectHandGesture: " + hand.name + "でピンチを検知！");
        return GestureType.IndexPinch;
    }

    // ... 他の指のピンチ判定 ...

    return GestureType.None;
}
}
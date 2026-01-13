using UnityEngine;

public class HandTracker : MonoBehaviour
{
    [Header("OVR Hand References")]
    public OVRHand hand;        // Meta SDK の Hand コンポーネント
    public OVRSkeleton skeleton; // 骨情報

    private Transform indexTip;
    private Transform middleTip;
    private bool isInitialized;

    private float updateInterval = 0.005f; // 0.1秒ごとに更新
    private float nextUpdateTime = 0f;

    public Vector3 IndexTipPos => indexTip ? indexTip.position : Vector3.zero;
    public Vector3 MiddleTipPos => middleTip ? middleTip.position : Vector3.zero;

    public bool IsIndexPinching => hand && hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    public bool IsMiddlePinching => hand && hand.GetFingerIsPinching(OVRHand.HandFinger.Middle);

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
            isInitialized = true;
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            if (!isInitialized && skeleton != null && skeleton.IsDataValid)
            {
                InitializeBones();
            }
            nextUpdateTime = Time.time + updateInterval;
        }
    }
}


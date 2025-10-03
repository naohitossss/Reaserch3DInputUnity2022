using UnityEngine;
using Oculus.Interaction.Input;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager instance { get; private set; }
    private AdvancedCubeController targetCube;
    private ChildBlockTrigger targetChildCube;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private void OnEnable()
    {
        if (GestureManager.instance != null)
            GestureManager.instance.OnNewGesturePerformed += HandleGesture;
    }

    private void OnDisable()
    {
        if (GestureManager.instance != null)
            GestureManager.instance.OnNewGesturePerformed -= HandleGesture;
    }

    // InteractionManager.cs の HandleGesture メソッド
private void HandleGesture(GestureManager.GestureType gesture, Handedness hand)
{
    // ▼▼▼ このログが重要です ▼▼▼
    Debug.Log("InteractionManager: ジェスチャー受信: " + gesture);

    if (targetCube != null)
    {
        Debug.Log("InteractionManager: ターゲット (" + targetCube.name + ") にジェスチャーを伝達します。");
        targetCube.OnPinchPerformed(gesture);
    }
    else
    {
        // ▼▼▼ おそらく、こちらのログが表示されます ▼▼▼
        Debug.LogWarning("InteractionManager: ターゲットがNULLです！ジェスチャーを伝える相手がいません。");
    }
}

    public void SetTargetCube(AdvancedCubeController cube) => targetCube = cube;

    public void ClearTargetCube(AdvancedCubeController cube)
    {
        if (targetCube == cube) targetCube = null;
    }

    public void SetTargetChildCube(ChildBlockTrigger cube) => targetChildCube = cube;

    public void ClearTargetChildCube(ChildBlockTrigger cube)
    {
        if (targetChildCube == cube) targetChildCube = null;
    }
}
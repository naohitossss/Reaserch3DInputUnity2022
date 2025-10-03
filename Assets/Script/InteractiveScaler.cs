using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InteractiveScaler : MonoBehaviour
{
    [Header("方向を示す子ブロック (子ブロック)")]
    [Tooltip("このブロックが触られた時に表示する6つの子ブロックのリスト")]
    public List<GameObject> directionBlocks = new List<GameObject>(6);

    [Header("感度設定")]
    [Tooltip("手の移動量に対して子ブロックが大きくなる感度")]
    public float scaleMultiplier = 2.0f;

    [Header("透明度の設定")]
    [Tooltip("手が触れたときの本体のアルファ値(0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float fadedAlpha = 0.5f;

    // --- プライベート変数 ---
    private List<Renderer> blockRenderers = new List<Renderer>();
    private List<Vector3> initialScales = new List<Vector3>();
    private Transform handTransform;
    private Vector3 entryPosition;
    private MeshRenderer mainBlockRenderer;
    private Color originalColor;
    private Coroutine resetCoroutine;

    // --- 状態管理変数 ---
    private GameObject currentTouchedObject = null;
    private GameObject activeChild = null;
    private bool isScalingActive = false;

    void Start()
    {
        mainBlockRenderer = GetComponent<MeshRenderer>();
        if (mainBlockRenderer != null)
        {
            originalColor = mainBlockRenderer.material.color;
        }

        for (int i = 0; i < directionBlocks.Count; i++)
        {
            GameObject block = directionBlocks[i];
            if (block != null)
            {
                blockRenderers.Add(block.GetComponent<Renderer>());
                initialScales.Add(block.transform.localScale);
                
                var renderer = block.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }

                var col = block.GetComponent<BoxCollider>() ?? block.AddComponent<BoxCollider>();
                col.isTrigger = true;
                
                var triggerScript = block.GetComponent<ChildBlockTrigger>() ?? block.AddComponent<ChildBlockTrigger>();
                triggerScript.parentScaler = this;
            }
        }
    }

    // --- グローバルリセット機能 ---
    
    public static void TriggerGlobalReset()
    {
        KeyBlock.GlobalReset();
    
        InteractiveScaler[] allRootBlocks = FindObjectsOfType<InteractiveScaler>(true);

        foreach (var block in allRootBlocks)
        {
            block.gameObject.SetActive(true);
            block.ResetAllBlocks();
        }
    }

    // --- トリガーイベント管理 ---

    private void OnTriggerEnter(Collider other) { OnBlockEnter(this.gameObject, other); }
    private void OnTriggerExit(Collider other) { OnBlockExit(this.gameObject, other); }

    public void OnBlockEnter(GameObject enteredBlock, Collider handCollider)
    {


        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }

        currentTouchedObject = enteredBlock;

        if (enteredBlock == this.gameObject)
        {
            if (activeChild != null)
            {
                TriggerGlobalReset();
            }
            else
            {
                StartMainInteraction(handCollider.transform);
            }
        }
        else 
        {

            activeChild = enteredBlock;
            foreach (var block in directionBlocks)
            {
                if (block != null && block != activeChild)
                {
                    var renderer = block.GetComponent<Renderer>();
                    if(renderer != null) renderer.enabled = false;
                }
            }
            isScalingActive = false;
        }
    }

    public void OnBlockExit(GameObject exitedBlock, Collider handCollider)
    {

        if (currentTouchedObject == exitedBlock)
        {
            currentTouchedObject = null;
            if (resetCoroutine == null)
            {
                resetCoroutine = StartCoroutine(ResetAfterDelay(10f));
            }
        }
    }
    
    // --- ローカルリセット処理 ---

    private void ResetAllBlocks()
    {
        currentTouchedObject = null;
        activeChild = null;
        isScalingActive = false;
        handTransform = null;
        
        if (mainBlockRenderer != null)
        {
            mainBlockRenderer.enabled = true;
        }
        SetBlockAlpha(originalColor.a);

        foreach (var block in directionBlocks)
        {
            if (block != null)
            {
                var renderer = block.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
                
                var childTrigger = block.GetComponent<ChildBlockTrigger>();
                if (childTrigger != null)
                {
                    childTrigger.ResetChildBlock();
                }
            }
        }
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerGlobalReset();
        resetCoroutine = null;
    }
    
    // --- ヘルパーメソッド群 ---

    private void StartMainInteraction(Transform hand)
    {
        isScalingActive = true;
        handTransform = hand;
        entryPosition = handTransform.position;
        ShowBlocks();
        SetBlockAlpha(fadedAlpha);
    }
    
    private void SetBlockAlpha(float alpha) 
    { 
        if (mainBlockRenderer == null) return; 
        Color newColor = originalColor; 
        newColor.a = alpha; 
        mainBlockRenderer.material.color = newColor; 
    }

    private void ShowBlocks() 
    { 
        foreach (var renderer in blockRenderers) 
        { 
            if (renderer != null) renderer.enabled = true; 
        } 
    }

    private void Update()
    {
        if (isScalingActive && handTransform != null)
        {
            UpdateBlockScales();
        }
    }

    private void UpdateBlockScales() 
    { 
        Vector3 displacement = handTransform.position - entryPosition; 
        for (int i = 0; i < directionBlocks.Count; i++) 
        { 
            if (directionBlocks[i] != null) 
            { 
                directionBlocks[i].transform.localScale = initialScales[i]; 
            } 
        } 
        float dx_plus = Mathf.Max(0, displacement.x); float dx_minus = Mathf.Max(0, -displacement.x); 
        float dy_plus = Mathf.Max(0, displacement.y); float dy_minus = Mathf.Max(0, -displacement.y); 
        float dz_plus = Mathf.Max(0, displacement.z); float dz_minus = Mathf.Max(0, -displacement.z); 
        directionBlocks[0].transform.localScale = initialScales[0] + Vector3.one * dx_plus * scaleMultiplier; 
        directionBlocks[1].transform.localScale = initialScales[1] + Vector3.one * dx_minus * scaleMultiplier; 
        directionBlocks[2].transform.localScale = initialScales[2] + Vector3.one * dy_plus * scaleMultiplier; 
        directionBlocks[3].transform.localScale = initialScales[3] + Vector3.one * dy_minus * scaleMultiplier; 
        directionBlocks[4].transform.localScale = initialScales[4] + Vector3.one * dz_plus * scaleMultiplier; 
        directionBlocks[5].transform.localScale = initialScales[5] + Vector3.one * dz_minus * scaleMultiplier; 
    }
}
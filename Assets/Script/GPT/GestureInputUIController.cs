using UnityEngine;

public class GestureInputUIController : MonoBehaviour
{
    [Header("References")]
    public GestureInput gestureInput;
    
    [Header("Blocks")]
    public GameObject mainBlockPrefab;
    public GameObject categoryBlockPrefab;
    public GameObject keyBlockPrefab;
    
    [Header("Layout")]
    public float categoryBlockDistance = 0.2f;
    public float keyBlockDistance = 0.15f;
    
    [Header("Position Settings")]
    public Camera mainCamera;  // VRカメラの参照
    public float distanceFromCamera = 0.5f;  // カメラからの距離
    public Vector2 offset = new Vector2(-0.4f, 0.4f);  // 左上へのオフセット

    private GameObject mainBlock;
    private GameObject[] categoryBlocks = new GameObject[6];
    private GameObject[] keyBlocks = new GameObject[6];
    private int selectedCategory = -1;

    private readonly Vector3[] directions = {
        Vector3.right,      // 0: Right
        Vector3.left,       // 1: Left
        Vector3.up,         // 2: Up
        Vector3.down,       // 3: Down
        Vector3.forward,    // 4: Forward
        Vector3.back        // 5: Back
    };

    void Start()
    {
        if (gestureInput != null)
        {
            gestureInput.OnCategorySelected += HandleCategorySelected;
            gestureInput.OnKeySelected += HandleKeySelected;
            
            // カメラが設定されていない場合、メインカメラを取得
            if (mainCamera == null)
                mainCamera = Camera.main;

            // UIの位置をカメラの左上に設定
            UpdateUIPosition();
        }
        
        CreateMainBlock();
        CreateCategoryBlocks();
        CreateKeyBlocks();
        UpdateUIState(GestureInput.InputPhase.Idle);
    }

    void UpdateUIPosition()
    {
        if (mainCamera != null)
        {
            // カメラの前方ベクトルに距離を掛けて基準位置を得る
            Vector3 basePosition = mainCamera.transform.position + 
                                 mainCamera.transform.forward * distanceFromCamera;

            // カメラの右と上のベクトルを使用してオフセットを適用
            Vector3 offsetPosition = basePosition + 
                                   mainCamera.transform.right * offset.x + 
                                   mainCamera.transform.up * offset.y;

            transform.position = offsetPosition;
        }
    }

    void CreateMainBlock()
    {
        mainBlock = Instantiate(mainBlockPrefab, transform.position, transform.rotation);
        mainBlock.transform.parent = transform;
    }

    void CreateCategoryBlocks()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector3 position = transform.position + transform.rotation * directions[i] * categoryBlockDistance;
            categoryBlocks[i] = Instantiate(categoryBlockPrefab, position, transform.rotation);
            categoryBlocks[i].transform.parent = transform;
        }
    }

    void CreateKeyBlocks()
    {
        for (int i = 0; i < 6; i++)
        {
            keyBlocks[i] = Instantiate(keyBlockPrefab, Vector3.zero, Quaternion.identity);
            keyBlocks[i].transform.parent = transform;
            keyBlocks[i].SetActive(false);
        }
    }

    void HandleCategorySelected(Vector3 start, Vector3 end)
    {
        selectedCategory = DirectionalSelector.GetDirectionIndex(start, end);
        UpdateUIState(GestureInput.InputPhase.CategoryReady);
    }

    void HandleKeySelected(Vector3 start, Vector3 end)
    {
        int keyIndex = DirectionalSelector.GetDirectionIndex(start, end);
        UpdateUIState(GestureInput.InputPhase.KeySelecting, keyIndex);
    }

    void UpdateUIState(GestureInput.InputPhase phase, int selectedIndex = -1)
    {
        switch (phase)
        {
            case GestureInput.InputPhase.Idle:
                // メインブロックのみを表示
                mainBlock.SetActive(true);
                foreach (var block in categoryBlocks) 
                    block.SetActive(false);
                foreach (var block in keyBlocks) 
                    block.SetActive(false);
                break;

            case GestureInput.InputPhase.CategoryReady:
                // メインブロックを非表示、選択されたカテゴリーブロックのみ表示
                mainBlock.SetActive(false);
                
                if (selectedCategory >= 0 && selectedCategory < categoryBlocks.Length)
                {
                    // 選択されたカテゴリーブロックのみ表示
                    for (int i = 0; i < categoryBlocks.Length; i++)
                    {
                        categoryBlocks[i].SetActive(i == selectedCategory);
                        categoryBlocks[i].transform.position = transform.position + directions[i] * categoryBlockDistance;
                    }
                    
                    // キーブロックは非表示
                    foreach (var block in keyBlocks) 
                        block.SetActive(false);
                }
                break;

            case GestureInput.InputPhase.KeySelecting:
                mainBlock.SetActive(false);
                
                if (selectedCategory >= 0 && selectedCategory < categoryBlocks.Length &&
                    selectedIndex >= 0 && selectedIndex < keyBlocks.Length)
                {
                    // 選択されたカテゴリーブロックを表示
                    categoryBlocks[selectedCategory].SetActive(true);
                    Vector3 categoryPos = categoryBlocks[selectedCategory].transform.position;

                    // 選択されたキーブロックとその位置を設定
                    for (int i = 0; i < keyBlocks.Length; i++)
                    {
                        keyBlocks[i].transform.position = categoryPos + directions[i] * keyBlockDistance;
                        keyBlocks[i].SetActive(i == selectedIndex);
                    }
                }
                break;
        }
    }

    void OnDestroy()
    {
        if (gestureInput != null)
        {
            gestureInput.OnCategorySelected -= HandleCategorySelected;
            gestureInput.OnKeySelected -= HandleKeySelected;
        }
    }

    void LateUpdate()
    {
        // 毎フレーム位置を更新してカメラに追従
        UpdateUIPosition();
    }
}
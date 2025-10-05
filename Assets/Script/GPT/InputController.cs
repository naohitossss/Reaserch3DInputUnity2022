using UnityEngine;

public class InputController : MonoBehaviour
{
    public GestureInput gestureManager;

    // ✅ カテゴリとキー配列を記録
    private readonly string[] categories = { "1", "2", "3", "4", "5", "6" };
    private readonly string[,] keys =
    {
        { "1", "A", "B", "C", "D", "E" },
        { "2", "F", "G", "H", "I", "J" },
        { "3", "K", "L", "M", "N", "O" },
        { "4", "P", "Q", "R", "S", "T" },
        { "5", "U", "V", "W", "X", "Y" },
        { "6", "7", "8", "9", "0", " " },
    };

    private int currentCategory = -1;

    void Start()
    {
        if (gestureManager != null)
        {
            gestureManager.OnCategorySelected += OnCategorySelected;
            gestureManager.OnKeySelected += OnKeySelected;
        }
    }

    void OnCategorySelected(Vector3 start, Vector3 end)
    {
        currentCategory = DirectionalSelector.GetDirectionIndex(start, end);
        Debug.Log($" Category Selected: {categories[currentCategory]}");
    }

    void OnKeySelected(Vector3 start, Vector3 end)
    {
        if (currentCategory < 0) return;

        int keyIndex = DirectionalSelector.GetDirectionIndex(start, end);
        string key = keys[currentCategory, keyIndex];

        Debug.Log($"🔡 Key Selected: {key}");

        if (InputManager.instance != null)
        {
            InputManager.instance.AppendCharacter(key);
        }

        currentCategory = -1;
    }
}
using UnityEngine;

public static class DirectionalSelector
{
    // キャッシュ用の配列を静的に保持
    private static readonly Vector3[] DirectionVectors = {
        Vector3.right,
        Vector3.left,
        Vector3.up,
        Vector3.down,
        Vector3.forward,
        Vector3.back
    };

    public static int GetDirectionIndex(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start);
        float maxDot = float.MinValue;
        int selectedIndex = -1;

        // マグニチュードの計算を1回だけ行う
        float magnitude = direction.magnitude;
        if (magnitude < 0.001f) return -1;

        direction /= magnitude; // 正規化を1回だけ

        for (int i = 0; i < DirectionVectors.Length; i++)
        {
            float dot = Vector3.Dot(direction, DirectionVectors[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                selectedIndex = i;
            }
        }

        return maxDot > 0.7f ? selectedIndex : -1;
    }

    // デバッグ用
    public static string GetDirectionName(int index)
    {
        return index switch
        {
            0 => "Right",
            1 => "Left",
            2 => "Up",
            3 => "Down",
            4 => "Forward",
            5 => "Back",
            _ => "Invalid"
        };
    }
}


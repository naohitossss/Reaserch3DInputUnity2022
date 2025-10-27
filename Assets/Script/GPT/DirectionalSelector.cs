using UnityEngine;

public static class DirectionalSelector
{
    private static readonly Vector3[] DirectionVectors = {
        Vector3.right,      // 0: Right
        Vector3.left,       // 1: Left
        Vector3.up,         // 2: Up
        Vector3.down,       // 3: Down
        Vector3.forward,    // 4: Forward
        Vector3.back        // 5: Back
    };

    public static int GetDirectionIndex(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float maxDot = float.MinValue;
        int selectedIndex = 0;

        // 各方向ベクトルとの内積を計算し、最も近い方向を選択
        for (int i = 0; i < DirectionVectors.Length; i++)
        {
            float dot = Vector3.Dot(direction, DirectionVectors[i]);
            if (dot > maxDot)
            {
                maxDot = dot;
                selectedIndex = i;
            }
        }

        // 内積が一定値以上の場合のみ方向を確定
        if (maxDot > 0.7f) // cos(45度) ≒ 0.7
        {
            return selectedIndex;
        }

        return -1; // 方向が不明確な場合
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


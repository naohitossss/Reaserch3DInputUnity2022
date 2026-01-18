using UnityEngine;

public static class DirectionalSelector
{
    // インデックスと方向の対応（変更なし）
    // 0: Right (+X)
    // 1: Left  (-X)
    // 2: Up    (+Y)
    // 3: Down  (-Y)
    // 4: Forward (+Z)
    // 5: Back   (-Z)

    /// <summary>
    /// 最大成分法を用いて、ベクトルが主要6方向のどれに最も近いかを判定します。
    /// </summary>
    /// <param name="start">開始点</param>
    /// <param name="end">終了点</param>
    /// <returns>方向インデックス (0-5)。移動量が小さすぎる場合は -1。</returns>
    public static int GetDirectionIndex(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;

        // 移動量が極端に小さい場合は未検出(-1)とする（誤検出防止）
        // sqrMagnitudeは平方根計算が不要なので高速
        if (direction.sqrMagnitude < 0.0001f)
        {
            return -1;
        }

        // 手順1: 各成分の絶対値を取得
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        float absZ = Mathf.Abs(direction.z);

        // 手順2: 最大の値を持つ軸を特定する
        // 暫定的にX軸が最大と仮定
        float maxAbsComponent = absX;
        int dominantAxis = 0; // 0=X, 1=Y, 2=Z

        // Y軸の方が大きければ更新
        if (absY > maxAbsComponent)
        {
            maxAbsComponent = absY;
            dominantAxis = 1;
        }
        // Z軸の方が大きければ更新
        if (absZ > maxAbsComponent)
        {
            maxAbsComponent = absZ;
            dominantAxis = 2;
        }

        // 手順3: その軸の成分の符号を確認し、最終的な方向を決定する
        switch (dominantAxis)
        {
            case 0: // X軸が主軸
                return (direction.x > 0) ? 0 : 1; // 正ならRight(0), 負ならLeft(1)
            case 1: // Y軸が主軸
                return (direction.y > 0) ? 2 : 3; // 正ならUp(2), 負ならDown(3)
            case 2: // Z軸が主軸
                return (direction.z > 0) ? 4 : 5; // 正ならForward(4), 負ならBack(5)
            default:
                return -1; // ここには到達しないはず
        }
    }

    // デバッグ用（変更なし）
    public static string GetDirectionName(int index)
    {
        return index switch
        {
            0 => "Right (+X)",
            1 => "Left  (-X)",
            2 => "Up    (+Y)",
            3 => "Down  (-Y)",
            4 => "Forward (+Z)",
            5 => "Back   (-Z)",
            _ => "Invalid"
        };
    }
}


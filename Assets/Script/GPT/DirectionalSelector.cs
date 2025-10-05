using UnityEngine;

public static class DirectionalSelector
{
    // start→endの方向ベクトルで6方向を判定
    public static int GetDirectionIndex(Vector3 start, Vector3 end)
    {
        Vector3 dir = (end - start).normalized;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

        // 0〜360度に正規化
        if (angle < 0) angle += 360f;

        // 6方向に分割
        int index = Mathf.FloorToInt(angle / 60f) % 6;
        return index;
    }
}


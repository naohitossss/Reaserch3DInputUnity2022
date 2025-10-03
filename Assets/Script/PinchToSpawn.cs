using UnityEngine;

public class PinchToSpawn : MonoBehaviour
{
    [Header("表示するブロックシステム")]
    [Tooltip("シーン内に既に配置されている、非アクティブなブロックシステムを割り当てます")]
    public GameObject targetBlockSystem; // PrefabからGameObjectへの参照に変更

    // メソッド名を役割に合わせて変更
    public void ActivateTargetBlockSystem()
    {
        if (targetBlockSystem == null)
        {
            Debug.LogError("targetBlockSystemが設定されていません！", this);
            return;
        }

        // --- ここからが変更点 ---
        // ターゲットを指定の位置と向きに移動させる
        targetBlockSystem.transform.position = this.transform.position;
        targetBlockSystem.transform.rotation = this.transform.rotation;

        // ターゲットをアクティブにして表示する
        targetBlockSystem.SetActive(true);
        // --- ここまで ---

        // このブロック（自身）が所属する親のシステム全体を非表示にする
        InteractiveScaler parentScaler = GetComponentInParent<InteractiveScaler>();
        if (parentScaler != null)
        {
            parentScaler.gameObject.SetActive(false);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
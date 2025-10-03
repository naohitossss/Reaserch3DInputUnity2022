using UnityEngine;
using Oculus.Interaction.Input; // Handクラスを利用するために必要
using System.Collections.Generic; // Listを使うために必要

public class ChildBlockTrigger : MonoBehaviour
{
    [Header("スクリプト参照")]
    [Tooltip("親のInteractiveScaler。起動時に自動で設定されます。")]
    public InteractiveScaler parentScaler;

    [Header("ピンチで表示するオブジェクト")]
    [Tooltip("このブロックがピンチされた時にアクティブにする、6つの子ブロックのリスト")]
    public List<GameObject> childBlocksToActivate = new List<GameObject>(6);

    // --- プライベート変数 ---
    private Hand hand;
    private bool wasPinchingLastFrame = false;
    private bool childrenAreVisible = false;

    // --- トリガーイベント ---
    private void OnTriggerEnter(Collider other)
    {
        // 親に「自分に入ってきた」ことを伝える
        if (parentScaler != null)
        {
            parentScaler.OnBlockEnter(this.gameObject, other);
        }

        // 手のコンポーネントを探して保持する
        Hand handComponent = other.GetComponentInParent<Hand>();
        if (handComponent != null)
        {
            hand = handComponent;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 親に「自分から出ていった」ことを伝える
        if (parentScaler != null)
        {
            parentScaler.OnBlockExit(this.gameObject, other);
        }

        // 保持していた手が出て行ったなら、参照をクリアする
        if (hand != null && other.gameObject == hand.gameObject)
        {
            hand = null;
            wasPinchingLastFrame = false;
        }
    }

    // --- フレーム毎のピンチ状態監視 ---
    private void Update()
    {
        if (hand == null) return;

        bool isPinchingNow = hand.GetFingerIsPinching(HandFinger.Index);

        // --- ピンチを開始した瞬間 ---
        if (isPinchingNow && !wasPinchingLastFrame)
        {
            // 子ブロックを全て表示する
            foreach (var block in childBlocksToActivate)
            {
                if (block != null) block.SetActive(true);
            }
            childrenAreVisible = true;
        }
        // --- ピンチを離した瞬間 ---
        else if (!isPinchingNow && wasPinchingLastFrame)
        {
            // 子が表示されている状態でのみ処理
            if (childrenAreVisible)
            {
                // 手のエリア内にある子ブロックを見つける
                GameObject selectedChild = FindChildHandIsIn();

                // もし手がどの子ブロックのエリアにもなければ、何もしない
                if (selectedChild == null)
                {
                    return;
                }

                // 選ばれた子以外を非表示にする
                foreach (var block in childBlocksToActivate)
                {
                    if (block != null && block != selectedChild)
                    {
                        block.SetActive(false);
                    }
                }

                // 元の親システムを非表示にする
                if (parentScaler != null)
                {
                    parentScaler.gameObject.SetActive(false);
                }

                // このブロックのインタラクションを無効化
                GetComponent<Collider>().enabled = false;
                this.enabled = false;
            }
        }

        wasPinchingLastFrame = isPinchingNow;
    }

    // --- ヘルパーメソッド ---
    /// <summary>
    /// 現在の手の位置にある子ブロックを探して返す
    /// </summary>
    private GameObject FindChildHandIsIn()
    {
        // 手の位置を中心とした半径1cmの球体と重なるコライダーを全て取得
        Collider[] hitColliders = Physics.OverlapSphere(hand.transform.position, 0.01f);

        foreach (var hitCollider in hitColliders)
        {
            // 見つかったコライダーが、アクティブな子ブロックのリストに含まれているか確認
            if (childBlocksToActivate.Contains(hitCollider.gameObject))
            {
                // 見つかったら、その子ブロックを返す
                return hitCollider.gameObject;
            }
        }

        // どのブロックにも触れていなければnullを返す
        return null;
    }
    public void ResetChildBlock()
    {
        // 自身がアクティブにしたキーブロックを全て非表示にする
        foreach (var block in childBlocksToActivate)
        {
            if (block != null)
            {
                block.SetActive(false);
            }
        }

        childrenAreVisible = false;

        // ▼▼▼ 以下の2行を追加 ▼▼▼
        // 保持している手の情報をクリアする
        hand = null;
        wasPinchingLastFrame = false;
        // ▲▲▲ ここまで ▲▲▲

        // 無効化した自身のコンポーネントを再度有効化し、再利用可能にする
        GetComponent<Collider>().enabled = true;
        this.enabled = true;
    }
}
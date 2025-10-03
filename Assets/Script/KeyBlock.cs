using UnityEngine;
using Oculus.Interaction.Input;

public class KeyBlock : MonoBehaviour
{
    [Header("このキーに割り当てる文字")]
    [Tooltip("InputManagerに送信する文字（'a', 'Q', '!'など）")]
    public string character;

    // 現在アクティブなキー自身を、全インスタンスで共有する
    private static KeyBlock engagedKey = null;

    private Hand hand; // トリガー内にある手の情報
    private bool wasPinchingLastFrame = false; // 前のフレームでピンチしていたか

    private void OnTriggerEnter(Collider other)
    {
        Hand handComponent = other.GetComponentInParent<Hand>();
        if (handComponent == null) return;

        // もし自分以外のキーがアクティブなら、そのキーの権利を強制的に解除させる
        if (engagedKey != null && engagedKey != this)
        {
            engagedKey.Disengage();
        }

        // 自分がアクティブなキーになる
        engagedKey = this;
        hand = handComponent;
    }

    private void OnTriggerExit(Collider other)
    {
        // 自分が保持している手が出ていった場合のみ処理
        if (hand != null && other.gameObject == hand.gameObject)
        {
            // もし自分がアクティブなキーのままなら、全体のロックを解放する
            if (engagedKey == this)
            {
                engagedKey = null;
            }
            // 自身の状態は必ずリセット
            Disengage();
        }
    }

    private void Update()
    {
        // 手が範囲内にない場合は何もしない
        if (hand == null) return;

        bool isPinchingNow = hand.GetFingerIsPinching(HandFinger.Index);

        // 「前のフレームでピンチしていて、今のフレームで離した」瞬間を検知
         if (!isPinchingNow && wasPinchingLastFrame)
        {
            if (InputManager.instance != null)
            {
                Debug.Log("キーブロック '" + this.gameObject.name + "' が文字 '" + this.character + "' を送信しました。");
                // InputManagerに文字を送る
                InputManager.instance.AppendCharacter(character);

                // ▼▼▼ この行を追加 ▼▼▼
                // 文字入力の直後にグローバルリセットを呼び出す
                InteractiveScaler.TriggerGlobalReset();
                // ▲▲▲ ここまで ▲▲▲
            }
        }

        // 現在のピンチ状態を次のフレームのために保存
        wasPinchingLastFrame = isPinchingNow;
    }

    /// <summary>
    /// このキーの反応権を外部から強制的に解除する
    /// </summary>
    public void Disengage()
    {
        hand = null;
        wasPinchingLastFrame = false;
    }

    /// <summary>
    /// 全てのキーブロックで共有されているロック状態をグローバルにリセットする
    /// </summary>
    public static void GlobalReset()
    {
        engagedKey = null;
    }
}
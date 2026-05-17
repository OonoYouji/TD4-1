using System;

/// <summary>
/// 実行されると特定のイベントを発行し、演出の完了を待機するアクションノード。
/// 永久停止を防ぐためのタイムアウト機能を備える。
/// </summary>
public class InvokeEventNode : BehaviorNode
{
    public FrameEvent.Type eventType = FrameEvent.Type.TestEvent;
    public bool waitUntilComplete = true;
    public float timeoutSec = 5.0f; // 必須：永久停止防止のフェイルセーフ

    public InvokeEventNode() { }

    public InvokeEventNode(FrameEvent.Type eventType, bool waitUntilComplete = true, float timeoutSec = 5.0f)
    {
        this.eventType = eventType;
        this.waitUntilComplete = waitUntilComplete;
        this.timeoutSec = timeoutSec;
    }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // ノード固有の開始時刻キーとして NodeIdHash を使用
        uint startTimeKey = NodeIdHash;

        if (!blackboard.HasKey(startTimeKey))
        {
            // 初回実行: イベントを発行
            FrameEvent.EnqueueEntityEvent(eventType, owner.Id);
            
            if (!waitUntilComplete)
            {
                return NodeStatus.Success;
            }

            // 開始時刻を保存してRunning開始
            blackboard.SetFloat(startTimeKey, Time.time);
            return NodeStatus.Running;
        }

        // 2回目以降の実行: タイムアウトをチェック
        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = Time.time - startTime;

        if (elapsed >= timeoutSec)
        {
            // タイムアウト: 失敗として復帰（永久停止防止）
            blackboard.Remove(startTimeKey);
            return NodeStatus.Failure;
        }

        // TODO: 外部システム（StageSystem等）からの完了通知フラグをチェックするロジックをここに追加
        // 現時点では、InvokeEventNode自体が時間経過を待つ仕様として動作
        
        return NodeStatus.Running;
    }
}

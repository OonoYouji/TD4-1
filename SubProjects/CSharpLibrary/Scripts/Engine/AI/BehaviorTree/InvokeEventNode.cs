using System;

/// <summary>
/// 実行されると特定のイベントを発行し、演出の完了を待機するアクションノード。
/// 永久停止を防ぐためのタイムアウト機能を備える。
/// </summary>
public class InvokeEventNode : BehaviorNode
{
    public FrameEvent.Type EventType { get; set; }
    public bool WaitUntilComplete { get; set; } = true;
    public float TimeoutSec { get; set; } = 5.0f; // 必須：永久停止防止のフェイルセーフ

    public InvokeEventNode(FrameEvent.Type eventType, bool waitUntilComplete = true, float timeoutSec = 5.0f)
    {
        EventType = eventType;
        WaitUntilComplete = waitUntilComplete;
        TimeoutSec = timeoutSec;
    }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // ノード固有の開始時刻キーとして NodeIdHash を使用
        uint startTimeKey = NodeIdHash;

        if (!blackboard.HasKey(startTimeKey))
        {
            // 初回実行: イベントを発行
            FrameEvent.EnqueueEntityEvent(EventType, owner.Id);
            
            if (!WaitUntilComplete)
            {
                return NodeStatus.Success;
            }

            // 開始時刻を保存してRunning開始
            blackboard.SetFloat(startTimeKey, Time.time);
            return NodeStatus.Running;
        }

        // 2回目以降の実行: タイムアウトをチェック
        // ※本来はイベント完了通知をBlackboardの別フラグ等でチェックすべきだが、
        // 現状の仕様に合わせて経過時間での簡易的な待機/タイムアウト処理とする。
        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = Time.time - startTime;

        if (elapsed >= TimeoutSec)
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

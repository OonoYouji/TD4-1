using System;

/// <summary>
/// 実行されるとC++エンジン側に対して特定のイベント（FrameEvent）を発行するアクションノード。
/// ボスの必殺技の開始演出など、AIとゲームスクリプト・演出システムを連動させる際に使用する。
/// 永久停止（デッドロック）を防ぐためのタイムアウト機能（フェイルセーフ）を備える。
/// </summary>
public class InvokeEventNode : BehaviorNode
{
    /// <summary>
    /// 発行するイベントの名前。
    /// エディタ上で文字列として設定可能。
    /// </summary>
    public string eventName = "DefaultEvent";

    /// <summary>
    /// イベントを発行した後、外部システムから「完了通知」が来るまで待機するかどうか。
    /// </summary>
    public bool waitUntilComplete = true;

    /// <summary>
    /// 待機状態におけるタイムアウト時間（秒）。
    /// </summary>
    public float timeoutSec = 5.0f; 

    public InvokeEventNode() { }

    public InvokeEventNode(string eventName, bool waitUntilComplete = true, float timeoutSec = 5.0f)
    {
        this.eventName = eventName;
        this.waitUntilComplete = waitUntilComplete;
        this.timeoutSec = timeoutSec;
    }

    /// <summary>
    /// イベント発行および待機・タイムアウト処理。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("EventStart_" + NodeIdHash);
        uint completeKey = BehaviorTreeLoader.HashString("EventComplete_" + eventName);

        if (!blackboard.HasKey(startTimeKey))
        {
            // 1. 初回実行: イベントを発行
            Debug.Log($"<color=orange>[InvokeEvent]</color> {owner.name} <b>TRIGGERED</b> event: <color=white>{eventName}</color>");
            
            // 完了フラグをリセットしてから発行
            blackboard.SetBool(completeKey, false);
            FrameEvent.EnqueueNamedEvent(eventName, owner.Id);
            
            if (!waitUntilComplete)
            {
                return NodeStatus.Success;
            }

            blackboard.SetFloat(startTimeKey, Time.time);
            return NodeStatus.Running;
        }

        // 2. 2回目以降の実行（待機中）
        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = Time.time - startTime;

        // 設定されたタイムアウト時間を超えた場合
        if (elapsed >= timeoutSec)
        {
            Debug.LogWarning($"<color=red>[InvokeEvent]</color> {owner.name} event '{eventName}' <b>TIMED OUT</b> after {timeoutSec}s.");
            blackboard.Remove(startTimeKey);
            return NodeStatus.Failure;
        }

        // Blackboardの完了フラグを監視
        if (blackboard.GetBool(completeKey))
        {
            Debug.Log($"<color=cyan>[InvokeEvent]</color> {owner.name} event '{eventName}' <b>RECEIVED</b> completion signal.");
            blackboard.SetBool(completeKey, false); // 次回のためにリセット
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }
        
        return NodeStatus.Running;
    }
}

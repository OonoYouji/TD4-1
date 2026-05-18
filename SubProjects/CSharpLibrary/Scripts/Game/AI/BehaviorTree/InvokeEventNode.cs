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
        uint startTimeKey = NodeIdHash;

        if (!blackboard.HasKey(startTimeKey))
        {
            // 1. 初回実行: イベントを発行
            Debug.Log($"[InvokeEvent] {owner.name} triggered event: {eventName}");
            
            // TODO: エンジン側の文字列イベントシステムが実装されたらここに呼び出しを追加
            // 現状は互換性のためにTestEventとして発行しておく（必要に応じて）
            FrameEvent.EnqueueEntityEvent(FrameEvent.Type.TestEvent, owner.Id);
            
            if (!waitUntilComplete)
            {
                return NodeStatus.Success;
            }

            blackboard.SetFloat(startTimeKey, Time.time);
            return NodeStatus.Running;
        }

        // 2. 2回目以降の実行（待機中）: タイムアウトをチェックする
        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = Time.time - startTime;

        // 設定されたタイムアウト時間を超えた場合
        if (elapsed >= timeoutSec)
        {
            // フェイルセーフ発動：Blackboardから時刻を消去し、Failure（失敗）として異常終了する
            blackboard.Remove(startTimeKey);
            return NodeStatus.Failure;
        }

        // TODO: 外部システムからの「演出完了通知」を受け取るフラグ監視ロジックをここに追加する。
        // （完了通知を受け取った場合は、startTimeKeyを削除してNodeStatus.Successを返す）
        
        // 完了通知もタイムアウトも来ていなければ、引き続き待機（Running）
        return NodeStatus.Running;
    }
}

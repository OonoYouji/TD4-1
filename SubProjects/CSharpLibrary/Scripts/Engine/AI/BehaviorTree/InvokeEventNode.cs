using System;

/// <summary>
/// 実行されるとC++エンジン側に対して特定のイベント（FrameEvent）を発行するアクションノード。
/// ボスの必殺技の開始演出など、AIとゲームスクリプト・演出システムを連動させる際に使用する。
/// 永久停止（デッドロック）を防ぐためのタイムアウト機能（フェイルセーフ）を備える。
/// </summary>
public class InvokeEventNode : BehaviorNode
{
    /// <summary>
    /// 発行するイベントの種類。
    /// </summary>
    public FrameEvent.Type eventType = FrameEvent.Type.TestEvent;

    /// <summary>
    /// イベントを発行した後、外部システムから「完了通知」が来るまで待機するかどうか。
    /// falseの場合はイベント発行後、即座にSuccessを返す。
    /// </summary>
    public bool waitUntilComplete = true;

    /// <summary>
    /// 待機状態におけるタイムアウト時間（秒）。
    /// 外部システムからの完了通知が来なかった場合に、AIが永久に止まってしまうのを防ぐためのフェイルセーフ。
    /// </summary>
    public float timeoutSec = 5.0f; 

    public InvokeEventNode() { }

    public InvokeEventNode(FrameEvent.Type eventType, bool waitUntilComplete = true, float timeoutSec = 5.0f)
    {
        this.eventType = eventType;
        this.waitUntilComplete = waitUntilComplete;
        this.timeoutSec = timeoutSec;
    }

    /// <summary>
    /// イベント発行および待機・タイムアウト処理。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // ノード固有の開始時刻を保存するためのキーとして、ノード自身のIDハッシュを使用
        uint startTimeKey = NodeIdHash;

        // Blackboardに開始時刻が保存されていない場合は「初回実行」とみなす
        if (!blackboard.HasKey(startTimeKey))
        {
            // 1. 初回実行: キューにイベントを発行し、C++エンジン側に伝達する
            FrameEvent.EnqueueEntityEvent(eventType, owner.Id);
            
            // 待機が不要な設定であれば即座に終了（成功）
            if (!waitUntilComplete)
            {
                return NodeStatus.Success;
            }

            // 待機が必要な場合、現在の時刻を開始時刻としてBlackboardに保存し、Running（実行中）を返す
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

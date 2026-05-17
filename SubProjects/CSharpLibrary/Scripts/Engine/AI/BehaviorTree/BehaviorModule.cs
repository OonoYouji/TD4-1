using System;

/// <summary>
/// ノードの実行条件を制御するモジュールの基底クラス
/// </summary>
public abstract class BehaviorDecorator
{
    public uint NodeIdHash { get; set; }
    public ObserverAbortPolicy AbortPolicy { get; set; } = ObserverAbortPolicy.None;

    /// <summary>
    /// 条件を満たしているかチェックする
    /// </summary>
    public abstract bool CalculateCondition(Blackboard blackboard, Entity owner);

    /// <summary>
    /// 監視対象のBlackboardキーを取得する（継承先で実装）
    /// </summary>
    public virtual uint GetMonitoredKey() => 0;

    /// <summary>
    /// ノードの実行結果を後処理する（成功を失敗に変える、ループさせる等）
    /// </summary>
    public virtual NodeStatus PostProcessStatus(NodeStatus currentStatus, Blackboard blackboard) => currentStatus;
}

/// <summary>
/// ノードの実行中に定期的な処理を行うモジュールの基底クラス
/// </summary>
public abstract class BehaviorService
{
    public uint NodeIdHash { get; set; }
    public float Interval { get; set; } = 0.5f;

    /// <summary>
    /// 定期実行される更新処理
    /// </summary>
    public abstract void OnTick(Blackboard blackboard, Entity owner);
}

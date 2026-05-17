using System;

/// <summary>
/// ノードの実行条件を制御するモジュールの基底クラス
/// </summary>
public abstract class BehaviorDecorator
{
    public uint NodeIdHash { get; set; }

    /// <summary>
    /// 条件を満たしているかチェックする
    /// </summary>
    public abstract bool CalculateCondition(Blackboard blackboard, Entity owner);
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

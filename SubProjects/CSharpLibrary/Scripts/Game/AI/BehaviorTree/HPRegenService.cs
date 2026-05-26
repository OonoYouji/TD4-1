using System;

/// <summary>
/// エンティティのHPを一定量ずつ継続的に回復させるサービス。
/// ボス仕様書にある「HPは常に回復し続ける」を実装するために使用。
/// 回復量などのパラメータは Blackboard から読み取ることでフェーズごとの可変に対応。
/// </summary>
public class HPRegenService : BehaviorService
{
    /// <summary>
    /// 現在のHPが格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string currentHPKey = "CurrentHP";

    /// <summary>
    /// 最大HP。回復の上限。
    /// </summary>
    public float maxHP = 1000.0f;

    /// <summary>
    /// 1秒あたりの回復量。
    /// </summary>
    public float regenPerSecond = 5.0f;

    /// <summary>
    /// 定期実行される回復処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint hpKeyHash = BehaviorTreeLoader.HashString(currentHPKey);
        float currentHP = blackboard.GetFloat(hpKeyHash, maxHP);

        // 回復処理（Interval 考慮）
        if (currentHP < maxHP)
        {
            float amount = regenPerSecond * Interval;
            currentHP = Math.Min(maxHP, currentHP + amount);
            blackboard.SetFloat(hpKeyHash, currentHP);
        }
    }
}

using System;

/// <summary>
/// 現在のHP割合を監視し、指定された範囲内にある場合のみ実行を許可するデコレーター。
/// ボスのフェーズ遷移（例：HP 70%以下でフェーズ2へ）を実装するために使用。
/// </summary>
public class BossPhaseDecorator : BehaviorDecorator
{
    /// <summary>
    /// HP割合が格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string hpRatioKey = "HPRatio";

    /// <summary>
    /// 実行を許可するHP割合の下限。
    /// </summary>
    public float minRatio = 0.0f;

    /// <summary>
    /// 実行を許可するHP割合の上限。
    /// </summary>
    public float maxRatio = 1.0f;

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint keyHash = BehaviorTreeLoader.HashString(hpRatioKey);
        float currentRatio = blackboard.GetFloat(keyHash, 1.0f);

        // 指定範囲内であれば実行許可
        return (currentRatio >= minRatio && currentRatio <= maxRatio);
    }

    public override uint GetMonitoredKey()
    {
        return BehaviorTreeLoader.HashString(hpRatioKey);
    }
}

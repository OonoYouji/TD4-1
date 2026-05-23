using System;

/// <summary>
/// HPコンポーネントの値を監視し、Blackboardの値を更新するサービス。
/// これにより、Behavior Tree内でHPに基づいた条件判定が可能になる。
/// </summary>
public class HPMonitorService : BehaviorService
{
    /// <summary>
    /// 現在のHP割合(0.0~1.0)を書き込むBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string hpRatioKey = "HPRatio";

    /// <summary>
    /// 現在のHP値を書き込むBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string currentHPKey = "CurrentHP";

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        HP hpComp = owner.GetScript<HP>();
        if (hpComp == null) return;

        // Blackboardの値を更新
        blackboard.SetFloat(BehaviorTreeLoader.HashString(hpRatioKey), hpComp.CurrentHpRatio());
        blackboard.SetFloat(BehaviorTreeLoader.HashString(currentHPKey), (float)hpComp.currentHp);
    }
}

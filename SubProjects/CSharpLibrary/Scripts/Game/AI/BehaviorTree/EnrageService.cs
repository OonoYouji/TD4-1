using System;

/// <summary>
/// Blackboard の IsEnraged フラグを監視し、ボスのパラメータを強化するサービス。
/// 仕様書v2に基づき、移動速度1.5倍、攻撃間隔50%短縮を適用する。
/// </summary>
public class EnrageService : BehaviorService
{
    public float normalSpeed = 8.0f;
    public float enragedSpeed = 12.0f;

    protected override void OnTick(Blackboard blackboard, Entity owner)
    {
        bool isEnraged = blackboard.GetBool(BehaviorTreeLoader.HashString("IsEnraged"), false);
        
        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            intent.maxSpeed = isEnraged ? enragedSpeed : normalSpeed;
        }

        // 攻撃間隔の倍率をBlackboardに書き込む（WaitNodeなどが参照できるようにする）
        // 1.0 (通常) -> 0.5 (狂暴化：間隔が半分になる)
        blackboard.SetFloat(BehaviorTreeLoader.HashString("AttackIntervalMultiplier"), isEnraged ? 0.5f : 1.0f);
    }
}

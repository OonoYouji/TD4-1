using System;

/// <summary>
/// 実行されるたびにHPを一定量回復させるアクションノード。
/// Parallelノードの下に配置することで、常時回復を実現できる。
/// </summary>
public class HPRegenNode : BehaviorNode
{
    public float regenPerSecond = 5.0f;
    public float maxHP = 1200.0f;
    public float maxRatio = 0.7f; // デフォルトでPhase 2境界でロック

    private float _accumulatedHeal = 0.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        HP hpComp = owner.GetScript<HP>();
        if (hpComp == null) return NodeStatus.Failure;

        float limitHP = Math.Min(maxHP, maxHP * maxRatio);

        if (hpComp.currentHp >= (int)limitHP)
        {
            _accumulatedHeal = 0;
            return NodeStatus.Running; // 満タンでも実行はし続ける
        }

        _accumulatedHeal += regenPerSecond * Time.deltaTime;
        int amountInt = (int)_accumulatedHeal;

        if (amountInt >= 1)
        {
            int nextHp = hpComp.currentHp + amountInt;
            if (nextHp > (int)limitHP) nextHp = (int)limitHP;

            hpComp.currentHp = nextHp;
            _accumulatedHeal -= amountInt;

            // Blackboard同期
            blackboard.SetFloat(BehaviorTreeLoader.HashString("CurrentHP"), (float)hpComp.currentHp);
        }

        return NodeStatus.Running;
    }
}

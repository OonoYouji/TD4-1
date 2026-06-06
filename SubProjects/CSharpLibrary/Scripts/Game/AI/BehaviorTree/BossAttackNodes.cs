using System;

/// <summary>
/// ボスの特定のアクション（スクリプト）を開始させるための汎用BTノード基底クラス。
/// </summary>
public abstract class BossActionNode<T> : BehaviorNode where T : MonoScript
{
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        T attackScript = owner.GetScript<T>();
        if (attackScript == null)
        {
            Debug.LogError($"[BTNode] {typeof(T).Name} not found on {owner.name}");
            return NodeStatus.Failure;
        }

        // アクション開始メソッドを呼び出す（各スクリプトに共通のインターフェースがないため、リフレクションか個別実装が必要）
        return StartAction(attackScript, blackboard, owner);
    }

    protected abstract NodeStatus StartAction(T script, Blackboard blackboard, Entity owner);
}

public class BossBeamAttackNode : BossActionNode<BossBeamAttack>
{
    protected override NodeStatus StartAction(BossBeamAttack script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

public class BossClogAttackNode : BossActionNode<BossClogAttack>
{
    protected override NodeStatus StartAction(BossClogAttack script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

public class BossRockAttackNode : BossActionNode<BossRockAttack>
{
    protected override NodeStatus StartAction(BossRockAttack script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

public class BossBombBarrageNode : BossActionNode<BossBombBarrage>
{
    protected override NodeStatus StartAction(BossBombBarrage script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

public class BossVortexAttackNode : BossActionNode<BossVortexAttack>
{
    protected override NodeStatus StartAction(BossVortexAttack script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

public class BossPillarAttackNode : BossActionNode<BossPillarAttack>
{
    protected override NodeStatus StartAction(BossPillarAttack script, Blackboard blackboard, Entity owner)
    {
        script.StartAttack();
        return NodeStatus.Success;
    }
}

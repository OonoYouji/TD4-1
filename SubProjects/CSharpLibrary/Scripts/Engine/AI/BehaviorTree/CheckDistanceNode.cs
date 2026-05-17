using System;

/// <summary>
/// 指定したBlackboard変数に格納されたエンティティとの距離をチェックする条件ノード。
/// </summary>
[Decorator]
public class CheckDistanceNode : BehaviorNode
{
    [BlackboardKey]
    public string targetEntityIdKey = "TargetId";

    public float checkDistance = 5.0f;

    public CheckDistanceNode() { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetEntityIdKey);
        if (!blackboard.HasKey(key)) return NodeStatus.Failure;

        int targetId = blackboard.GetInt(key);
        // Entityを検索
        Entity target = owner.Group.GetEntity(targetId);
        if (target == null) return NodeStatus.Failure;

        float dist = Vector3.Distance(owner.transform.position, target.transform.position);
        
        return (dist <= checkDistance) ? NodeStatus.Success : NodeStatus.Failure;
    }
}

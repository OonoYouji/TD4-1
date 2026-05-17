using System;

/// <summary>
/// 指定したBlackboard変数に格納されたエンティティとの距離をチェックする条件ノード。
/// </summary>
[Decorator]
public class CheckDistanceNode : BehaviorNode
{
    [BlackboardKey]
    public string targetEntityNameKey = "TargetName";

    public float checkDistance = 5.0f;

    public CheckDistanceNode() { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetEntityNameKey);
        if (!blackboard.HasKey(key)) return NodeStatus.Failure;

        string targetName = blackboard.GetString(key);
        // Entityを検索
        Entity target = owner.Group.FindEntity(targetName);
        if (target == null) return NodeStatus.Failure;

        float dist = Vector3.Distance(owner.transform.position, target.transform.position);
        
        return (dist <= checkDistance) ? NodeStatus.Success : NodeStatus.Failure;
    }
}

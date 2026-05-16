/// <summary>
/// 子ノードを順番に実行するコンポジットノード。
/// </summary>
public class Sequence : CompositeNode
{
    public Sequence(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        foreach (var node in children)
        {
            switch (node.Execute(blackboard, owner))
            {
                case NodeStatus.Failure:
                    return NodeStatus.Failure;
                case NodeStatus.Running:
                    return NodeStatus.Running;
                case NodeStatus.Success:
                    continue;
            }
        }
        return NodeStatus.Success;
    }
}

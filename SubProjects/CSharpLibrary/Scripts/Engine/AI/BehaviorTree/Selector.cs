/// <summary>
/// 子ノードを順番に実行するコンポジットノード。
/// </summary>
public class Selector : CompositeNode
{
    public Selector(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        foreach (var node in children)
        {
            switch (node.Execute(blackboard, owner))
            {
                case NodeStatus.Success:
                    return NodeStatus.Success;
                case NodeStatus.Running:
                    return NodeStatus.Running;
                case NodeStatus.Failure:
                    continue;
            }
        }
        return NodeStatus.Failure;
    }
}

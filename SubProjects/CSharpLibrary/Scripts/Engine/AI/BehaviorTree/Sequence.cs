/// <summary>
/// 子ノードを順番に実行するコンポジットノード。
/// </summary>
public class Sequence : CompositeNode
{
    public Sequence() : base() { }
    public Sequence(params BehaviorNode[] nodes) : base(nodes) { }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Tick(blackboard, owner);
            switch (status)
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

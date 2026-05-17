/// <summary>
/// 子ノードを順番に実行するコンポジットノード。
/// </summary>
public class Sequence : CompositeNode
{
    public Sequence() : base() { }
    public Sequence(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Execute(blackboard, owner);
            switch (status)
            {
                case NodeStatus.Failure:
                    return LastStatus = NodeStatus.Failure;
                case NodeStatus.Running:
                    return LastStatus = NodeStatus.Running;
                case NodeStatus.Success:
                    continue;
            }
        }

        return LastStatus = NodeStatus.Success;
    }
}

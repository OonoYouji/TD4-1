/// <summary>
/// 子ノードを順番に実行し、一つでも成功すればSuccessを返すコンポジットノード。
/// </summary>
public class Selector : CompositeNode
{
    public Selector() : base() { }
    public Selector(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Execute(blackboard, owner);
            switch (status)
            {
                case NodeStatus.Success:
                    return LastStatus = NodeStatus.Success;
                case NodeStatus.Running:
                    return LastStatus = NodeStatus.Running;
                case NodeStatus.Failure:
                    continue;
            }
        }

        return LastStatus = NodeStatus.Failure;
    }
}


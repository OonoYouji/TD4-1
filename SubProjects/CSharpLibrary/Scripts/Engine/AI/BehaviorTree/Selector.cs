/// <summary>
/// 子ノードを順番に実行し、一つでも成功すればSuccessを返すコンポジットノード。
/// </summary>
public class Selector : CompositeNode
{
    public Selector() : base() { }
    public Selector(params BehaviorNode[] nodes) : base(nodes) { }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            var status = children[i].Tick(blackboard, owner);
            switch (status)
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


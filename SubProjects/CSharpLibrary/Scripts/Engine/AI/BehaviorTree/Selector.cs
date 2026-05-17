/// <summary>
/// 子ノードを順番に実行し、一つでも成功すればSuccessを返すコンポジットノード。
/// </summary>
public class Selector : CompositeNode
{
    public Selector() : base() { }
    public Selector(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // 実行中のインデックスを取得
        int currentIndex = blackboard.GetInt(NodeIdHash, 0);

        for (int i = currentIndex; i < children.Count; i++)
        {
            var status = children[i].Execute(blackboard, owner);
            switch (status)
            {
                case NodeStatus.Success:
                    // 成功時は状態をクリアしてSuccess
                    blackboard.Remove(NodeIdHash);
                    return NodeStatus.Success;
                case NodeStatus.Running:
                    // 継続時は現在のインデックスを保存してRunning
                    blackboard.SetInt(NodeIdHash, i);
                    return NodeStatus.Running;
                case NodeStatus.Failure:
                    // 失敗時は次のノードへ
                    continue;
            }
        }

        // 全て失敗したら状態をクリアしてFailure
        blackboard.Remove(NodeIdHash);
        return NodeStatus.Failure;
    }
}


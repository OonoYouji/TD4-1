/// <summary>
/// 子ノードを順番に実行するコンポジットノード。
/// </summary>
public class Sequence : CompositeNode
{
    public Sequence() : base() { }
    public Sequence(params BehaviorNode[] nodes) : base(nodes) { }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // 実行中のインデックスを取得
        int currentIndex = blackboard.GetInt(NodeIdHash, 0);

        for (int i = currentIndex; i < children.Count; i++)
        {
            var status = children[i].Execute(blackboard, owner);
            switch (status)
            {
                case NodeStatus.Failure:
                    // 失敗時は状態をクリアしてFailure
                    blackboard.Remove(NodeIdHash);
                    return NodeStatus.Failure;
                case NodeStatus.Running:
                    // 継続時は現在のインデックスを保存してRunning
                    blackboard.SetInt(NodeIdHash, i);
                    return NodeStatus.Running;
                case NodeStatus.Success:
                    // 成功時は次のノードへ
                    continue;
            }
        }

        // 全て成功したら状態をクリアしてSuccess
        blackboard.Remove(NodeIdHash);
        return NodeStatus.Success;
    }
}

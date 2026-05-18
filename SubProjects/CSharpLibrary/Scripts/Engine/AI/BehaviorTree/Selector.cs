/// <summary>
/// 子ノードを左から右へ順番に実行し、「一つでも成功すれば即座にSuccessを返す」コンポジットノード。
/// いわゆる「OR（論理和）」の挙動を示し、優先度の高い行動（攻撃など）から順に試し、
/// 失敗したら次の行動（移動など）を試す、というAIのメインブレイン（意思決定）によく使われる。
/// </summary>
public class Selector : CompositeNode
{
    public Selector() : base() { }
    public Selector(params BehaviorNode[] nodes) : base(nodes) { }

    /// <summary>
    /// 子ノードの実行ループ。
    /// 内部に状態を持たない「Reactive」な設計となっているため、毎フレーム必ず最初の子から評価し直す。
    /// これにより、実行中の行動より優先度の高い行動の条件が満たされた際、自然とそちらに実行権が移る（横取りされる）性質を持つ。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            // 子ノードを実行（内部でデコレーターの条件チェックも行われる）
            var status = children[i].Tick(blackboard, owner);
            
            switch (status)
            {
                // 一つでも成功すれば、以降の子ノードは評価せずに自身もSuccessを返す
                case NodeStatus.Success:
                    return NodeStatus.Success;
                    
                // 子ノードが継続中（時間がかかる処理など）であれば、自身もRunningを返す
                case NodeStatus.Running:
                    return NodeStatus.Running;
                    
                // 失敗した場合は、次の優先度の子ノードを試すためにループを継続する
                case NodeStatus.Failure:
                    continue;
            }
        }

        // 全ての子ノードが失敗した場合のみ、自身もFailureを返す
        return NodeStatus.Failure;
    }
}

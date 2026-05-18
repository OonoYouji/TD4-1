/// <summary>
/// 子ノードを左から右へ順番に実行し、「全てが成功したらSuccessを返す」コンポジットノード。
/// いわゆる「AND（論理積）」の挙動を示し、
/// 「敵に近づく」→「攻撃モーションを再生」→「待機」といった、一連の決まった手順（シーケンス）をこなす際によく使われる。
/// </summary>
public class Sequence : CompositeNode
{
    public Sequence() : base() { }
    public Sequence(params BehaviorNode[] nodes) : base(nodes) { }

    /// <summary>
    /// 子ノードの実行ループ。
    /// 内部に状態を持たない「Reactive」な設計となっているため、毎フレーム必ず最初の子から評価し直す。
    /// そのため、Sequence内に配置する各アクションノードは、完了済みのタスクを再度実行しないように
    /// Blackboardを用いて自身の実行状態（終了フラグ等）を管理することが推奨される。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        for (int i = 0; i < children.Count; i++)
        {
            // 子ノードを実行（内部でデコレーターの条件チェックも行われる）
            var status = children[i].Tick(blackboard, owner);
            
            switch (status)
            {
                // 一連の手順の途中で一つでも失敗すれば、即座に手順全体を中断しFailureを返す
                case NodeStatus.Failure:
                    return NodeStatus.Failure;
                    
                // 現在の手順が実行中であれば、以降の手順には進まず自身もRunningを返す
                case NodeStatus.Running:
                    return NodeStatus.Running;
                    
                // 手順が成功したら、次の手順（次の子ノード）の評価へ進む
                case NodeStatus.Success:
                    continue;
            }
        }

        // 全ての子ノードの処理が完了（成功）した場合のみ、自身もSuccessを返す
        return NodeStatus.Success;
    }
}

using System.Collections.Generic;

/// <summary>
/// 複数の子ノードを同時に（並行して）実行するコンポジットノード。
/// 「移動しながら射撃する」「敵を探しながら待機する」といった複合的な挙動を実現する。
/// 全ての子ノードが毎フレーム実行され、設定されたポリシーに基づいて成功・失敗を判定する。
/// </summary>
public class Parallel : CompositeNode
{
    /// <summary>
    /// 成功・失敗を判定するためのポリシー。
    /// One: 一つでも条件を満たせば完了
    /// All: 全てが条件を満たせば完了
    /// </summary>
    public enum Policy { One, All }

    /// <summary>
    /// 成功とみなす条件。
    /// One ならば「いずれかの子が成功」、All ならば「すべての子が成功」で自身も Success を返す。
    /// </summary>
    public Policy successPolicy = Policy.All;

    /// <summary>
    /// 失敗とみなす条件。
    /// One ならば「いずれかの子が失敗」、All ならば「すべての子が失敗」で自身も Failure を返す。
    /// </summary>
    public Policy failurePolicy = Policy.One;

    public Parallel() : base() { }
    public Parallel(params BehaviorNode[] nodes) : base(nodes) { }

    /// <summary>
    /// 並列実行のメインロジック。
    /// 全ての子ノードを順に評価し、それぞれの結果を集計する。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        int successCount = 0;
        int failureCount = 0;
        int runningCount = 0;

        for (int i = 0; i < children.Count; i++)
        {
            // 並列ノードでは毎フレーム全子ノードを評価する。
            // 各子ノードの状態は Tick 内で LastStatus に保存される。
            var status = children[i].Tick(blackboard, owner);
            
            if (status == NodeStatus.Success) successCount++;
            else if (status == NodeStatus.Failure) failureCount++;
            else if (status == NodeStatus.Running) runningCount++;
        }

        // 1. 失敗判定のチェック
        if (failurePolicy == Policy.One && failureCount > 0) return NodeStatus.Failure;
        if (failurePolicy == Policy.All && failureCount == children.Count && children.Count > 0) return NodeStatus.Failure;

        // 2. 成功判定のチェック
        if (successPolicy == Policy.One && successCount > 0) return NodeStatus.Success;
        if (successPolicy == Policy.All && successCount == children.Count && children.Count > 0) return NodeStatus.Success;

        // どちらの完了条件も満たしていない場合は継続中とする
        return NodeStatus.Running;
    }

    /// <summary>
    /// イベント駆動エンジン対応：並列ノードは子からの完了通知を個別に待つのではなく、
    /// 自身が ActiveNode となっている間、毎フレーム Execute を通じて全子を回す。
    /// </summary>
    public override NodeStatus OnChildCompleted(BehaviorNode child, NodeStatus status, Blackboard blackboard, Entity owner)
    {
        // 既に Execute 内で全子の Tick を回しているため、ここでのバブリングは
        // 再度 Execute を呼び出して全体の判定を最新に更新する。
        return Execute(blackboard, owner);
    }
}

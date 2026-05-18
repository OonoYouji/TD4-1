using System;

/// <summary>
/// 指定したBlackboard変数（名前）に格納されたエンティティとの距離をチェックするアクションノード。
/// 対象との距離が checkDistance 以下であれば Success を返す。
/// ※旧仕様では単体ノードとして機能していたが、現在は BlackboardDecorator や SensingService との
/// 組み合わせに移行しつつある。
/// </summary>
[Decorator]
public class CheckDistanceNode : BehaviorNode
{
    /// <summary>
    /// 対象エンティティの名前が格納されているBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetEntityNameKey = "TargetName";

    /// <summary>
    /// チェックする距離の閾値。
    /// </summary>
    public float checkDistance = 5.0f;

    public CheckDistanceNode() { }

    /// <summary>
    /// 距離判定ロジック。
    /// 対象の名前をBlackboardから取得し、ECSグループから実際のエンティティを検索して距離を測る。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetEntityNameKey);
        
        // キー自体がBlackboardに存在しない場合は失敗
        if (!blackboard.HasKey(key)) return NodeStatus.Failure;

        string targetName = blackboard.GetString(key);
        
        // Entityを名前から検索（IDは毎回のゲーム実行で変わるため名前ベースで取得）
        Entity target = owner.Group.FindEntity(targetName);
        if (target == null) return NodeStatus.Failure;

        // 対象と自分との距離を計算
        float dist = Vector3.Distance(owner.transform.position, target.transform.position);
        
        // 指定距離以内であれば成功
        return (dist <= checkDistance) ? NodeStatus.Success : NodeStatus.Failure;
    }
}

using System;

/// <summary>
/// 指定されたターゲットID（EntityID）を持つエンティティの現在座標を、
/// Blackboard の TargetPosition に書き込むサービス。
/// これにより、動くターゲット（Player等）に対して攻撃や予兆を追従させることができる。
/// </summary>
public class UpdateTargetTrackerNode : BehaviorService
{
    /// <summary>
    /// 追跡対象のIDが格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string targetIdKey = "TargetId";

    /// <summary>
    /// 現在座標を書き込むBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint idKeyHash = BehaviorTreeLoader.HashString(targetIdKey);
        if (!blackboard.HasKey(idKeyHash)) return;

        int targetId = blackboard.GetInt(idKeyHash);
        if (targetId == 0) return;

        // 対象エンティティを取得
        // 1. 自分のグループを優先
        Entity target = owner.Group.GetEntity(targetId);
        
        // 2. 他の全てのグループを検索
        if (target == null)
        {
            foreach (var g in EntityComponentSystem.GetAllGroups())
            {
                if (g.groupName == owner.Group.groupName) continue;
                target = g.GetEntity(targetId);
                if (target != null) break;
            }
        }

        if (target != null)
        {
            target.FetchInitialData();
            Vector3 pos = target.transform.position;
            uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);

            blackboard.SetVector3(keyHash, pos);

            // 超詳細ログ
        }
        else
        {
            // IDはあるが見つからない場合はリセット（再検索を促す）
            blackboard.SetInt(idKeyHash, 0);
        }
    }
}


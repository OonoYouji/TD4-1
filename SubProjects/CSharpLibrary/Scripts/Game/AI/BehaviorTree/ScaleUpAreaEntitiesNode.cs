using System;
using System.Collections.Generic;

/// <summary>
/// 指定された座標の範囲内にいるエンティティ（特に援軍）を巨大化させるアクションノード。
/// ボスの「詰まらせ攻撃」の中核ロジック。
/// </summary>
public class ScaleUpAreaEntitiesNode : BehaviorNode
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    /// <summary>
    /// 影響を及ぼす半径。
    /// </summary>
    public float effectRadius = 5.0f;

    /// <summary>
    /// 拡大するスケールの倍率。
    /// </summary>
    public float scaleMultiplier = 3.0f;

    /// <summary>
    /// 対象とするエンティティの名前のキーワード（例："Reinforcement"）。空なら全て対象。
    /// </summary>
    public string targetNameFilter = "Reinforcement";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
        if (!blackboard.HasKey(keyHash))
        {
            Debug.LogWarning($"ScaleUpAreaEntitiesNode: Target position key '{targetPosKey}' not found.");
            return NodeStatus.Failure;
        }

        Vector3 targetPos = blackboard.GetVector3(keyHash);
        var entities = owner.Group.GetEntities();
        int affectedCount = 0;

        foreach (var entity in entities)
        {
            // 自身は除外
            if (entity.Id == owner.Id) continue;

            // 名前フィルタリング
            if (!string.IsNullOrEmpty(targetNameFilter) && !entity.name.Contains(targetNameFilter))
            {
                continue;
            }

            // 距離判定
            float dist = Vector3.Distance(targetPos, entity.transform.position);
            
            if (dist <= effectRadius)
            {
                // 何度も巨大化しないように、現在のスケール値をチェックする
                // （初期スケールが 1.0 であることを前提とし、すでに倍率以上に大きくなっていたらスキップ）
                if (entity.transform.scale.x < scaleMultiplier * 0.8f)
                {
                    // スケールアップ処理
                    entity.transform.scale *= scaleMultiplier;
                    affectedCount++;

                    // 巨大化エフェクトのイベント発行（オプション）
                    FrameEvent.EnqueueNamedEvent("Effect_ScaleUp", entity.Id);
                }
            }
        }

        Debug.Log($"<color=magenta>[ScaleUpAttack]</color> {owner.name} scaled up {affectedCount} entities at {targetPos}.");
        
        // 演出としてボス自身の咆哮イベントも発行
        FrameEvent.EnqueueNamedEvent("Effect_BossRoar", owner.Id);

        return NodeStatus.Success;
    }
}

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

        // 判定範囲をGizmoで可視化 (マゼンタ色、太さ12.0)
        GizmoBatch.DrawWireCircle(targetPos + Vector3.up * 0.1f, effectRadius, new Vector4(1, 0, 1, 1), 32, 12.0f);

        var entities = owner.Group.GetEntities();
        int affectedCount = 0;

        foreach (var entity in entities)
        {
            // 自身は除外
            if (entity.Id == owner.Id) continue;

            // フィルタリング (大文字小文字を区別せず、スクリプトもチェック)
            bool isTarget = entity.name.ToLower().Contains("reinforcement") || entity.GetScript<Reinforcement>() != null;
            if (!isTarget) continue;

            // 距離判定
            float dist = Vector3.Distance(targetPos, entity.transform.position);
            
            if (dist <= effectRadius)
            {
                // 何度も巨大化しないように、現在のスケール値をチェックする
                if (entity.transform.scale.x < scaleMultiplier * 0.8f)
                {
                    // スケールアップ処理
                    entity.transform.scale *= scaleMultiplier;
                    affectedCount++;

                    // 巨大化エフェクトのイベント発行
                    FrameEvent.EnqueueNamedEvent("Effect_ScaleUp", entity.Id);
                }
            }
        }

        Debug.Log($"<color=magenta>[ScaleUpAttack]</color> {owner.name} scaled up {affectedCount} Reinforcements at {Vector3.ToSimpleString(targetPos)}. (Multiplier: {scaleMultiplier})");
        
        // 演出としてボス自身の咆哮イベントも発行
        FrameEvent.EnqueueNamedEvent("Effect_BossRoar", owner.Id);

        return NodeStatus.Success;
    }
}

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
    /// Blackboardから半径を取得する場合のキー名。
    /// </summary>
    [BlackboardKey]
    public string effectRadiusKey = "";

    /// <summary>
    /// 拡大するスケールの倍率。
    /// </summary>
    public float scaleMultiplier = 3.0f;

    /// <summary>
    /// 対象とするエンティティの名前のキーワード（例："Reinforcement"）。空なら全て対象。
    /// </summary>
    public string targetNameFilter = "Reinforcement";

    /// <summary>
    /// 効果が発生するまでの遅延時間（秒）。アニメーションの「叩きつけ」の瞬間に合わせるために使用。
    /// </summary>
    public float delay = 0.0f;

    [BlackboardKey]
    public string delayKey = "";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("ScaleUpStart_" + NodeIdHash);
        float currentTime = Time.time;

        float finalDelay = delay;
        if (!string.IsNullOrEmpty(delayKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(delayKey);
            if (blackboard.HasKey(keyHash))
            {
                finalDelay = blackboard.GetFloat(keyHash, delay);
                if (finalDelay == delay)
                {
                    finalDelay = (float)blackboard.GetInt(keyHash, (int)delay);
                }
            }
        }

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        if (currentTime - startTime < finalDelay)
        {
            return NodeStatus.Running;
        }

        blackboard.Remove(startTimeKey);

        Vector3 targetPos = owner.transform.position;

        if (!string.IsNullOrEmpty(targetPosKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (blackboard.HasKey(keyHash))
            {
                targetPos = blackboard.GetVector3(keyHash);
            }
        }

        float finalRadius = effectRadius;
        if (!string.IsNullOrEmpty(effectRadiusKey)) {
            uint radiusKeyHash = BehaviorTreeLoader.HashString(effectRadiusKey);
            if (blackboard.HasKey(radiusKeyHash)) {
                finalRadius = blackboard.GetFloat(radiusKeyHash, effectRadius);
                if (finalRadius == effectRadius) {
                    finalRadius = (float)blackboard.GetInt(radiusKeyHash, (int)effectRadius);
                }
            }
        }

        // 判定範囲をGizmoで可視化 (マゼンタ色、太さ12.0)
        GizmoBatch.DrawWireCircle(targetPos + Vector3.up * 0.1f, finalRadius, new Vector4(1, 0, 1, 1), 32, 12.0f);

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
            
            if (dist <= finalRadius)
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

//         Debug.Log($"<color=magenta>[ScaleUpAttack]</color> {owner.name} scaled up {affectedCount} Reinforcements at {Vector3.ToSimpleString(targetPos)}. (Multiplier: {scaleMultiplier})");
        
        // 演出としてボス自身の咆哮イベントも発行
        FrameEvent.EnqueueNamedEvent("Effect_BossRoar", owner.Id);

        return NodeStatus.Success;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("ScaleUpStart_" + NodeIdHash));
    }
}

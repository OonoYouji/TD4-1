using System;
using System.Collections.Generic;

/// <summary>
/// 指定された座標に吸引力を発生させるフィールドを生成するノード。
/// 範囲内のエンティティを中心に引き寄せる。
/// </summary>
public class SpawnVortexFieldNode : BehaviorNode
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public string vortexPrefab = "VortexField";
    public float suctionRadius = 10.0f;
    public float suctionForce = 5.0f;
    public float duration = 4.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("VortexStart_" + NodeIdHash);
        uint entityIdKey = BehaviorTreeLoader.HashString("VortexEntityID_" + NodeIdHash);
        float currentTime = Time.time;

        // 1. 開始処理: プレハブの生成
        if (!blackboard.HasKey(startTimeKey))
        {
            uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (!blackboard.HasKey(posKeyHash)) return NodeStatus.Failure;

            Vector3 targetPos = blackboard.GetVector3(posKeyHash);
            blackboard.SetFloat(startTimeKey, currentTime);

            Entity vortex = owner.Group.CreateEntity(vortexPrefab);
            if (vortex != null)
            {
                vortex.transform.position = targetPos;
                blackboard.SetInt(entityIdKey, vortex.Id);
            }

            FrameEvent.EnqueueEffectEvent("Vortex_Activate", owner.Id, suctionRadius, duration);
            Debug.Log($"<color=blue>[VortexAttack]</color> {owner.name} spawned vortex at {targetPos}");
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        // 2. 終了判定
        if (elapsed >= duration)
        {
            if (blackboard.HasKey(entityIdKey))
            {
                owner.Group.DestroyEntity(blackboard.GetInt(entityIdKey));
                blackboard.Remove(entityIdKey);
            }
            blackboard.Remove(startTimeKey);
            FrameEvent.EnqueueNamedEvent("Effect_Vortex_End", owner.Id);
            return NodeStatus.Success;
        }

        // 3. 吸引ロジック (Running中毎フレーム実行)
        Vector3 center = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
        var entities = owner.Group.GetEntities();
        foreach (var entity in entities)
        {
            if (entity.Id == owner.Id) continue;
            // 既に生成したVortex自体も除外
            if (blackboard.HasKey(entityIdKey) && entity.Id == blackboard.GetInt(entityIdKey)) continue;

            float dist = Vector3.Distance(center, entity.transform.position);
            if (dist <= suctionRadius && dist > 0.5f)
            {
                Vector3 pullDir = (center - entity.transform.position).Normalized();
                // 距離が近いほど強く引く、あるいは一定の力で引く
                entity.transform.position += pullDir * suctionForce * Time.deltaTime;
            }
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        uint entityIdKey = BehaviorTreeLoader.HashString("VortexEntityID_" + NodeIdHash);
        if (blackboard.HasKey(entityIdKey))
        {
            owner.Group.DestroyEntity(blackboard.GetInt(entityIdKey));
            blackboard.Remove(entityIdKey);
        }
        blackboard.Remove(BehaviorTreeLoader.HashString("VortexStart_" + NodeIdHash));
    }
}

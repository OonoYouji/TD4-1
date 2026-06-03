using System;
using System.Collections.Generic;

/// <summary>
/// 指定された座標に吸引力を発生させるフィールドを生成するノード。
/// 範囲内のエンティティを中心に引き寄せる。
/// 
/// 仕様書v2対応：距離の2乗に反比例する吸引力と、中心部でのダメージ処理を追加。
/// </summary>
public class SpawnVortexFieldNode : BehaviorNode
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public string vortexPrefab = "VortexField";
    public float suctionRadius = 15.0f;
    public float suctionForce = 20.0f; // 最大吸引力
    public float duration = 5.0f;
    
    public int centerDamage = 50;
    public float centerDamageRadius = 3.0f;
    public float damageInterval = 0.5f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("VortexStart_" + NodeIdHash);
        uint entityIdKey = BehaviorTreeLoader.HashString("VortexEntityID_" + NodeIdHash);
        uint damageTimerKey = BehaviorTreeLoader.HashString("VortexDamageTimer_" + NodeIdHash);
        float currentTime = Time.time;

        // 1. 開始処理: プレハブの生成
        if (!blackboard.HasKey(startTimeKey))
        {
            uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (!blackboard.HasKey(posKeyHash)) return NodeStatus.Failure;

            Vector3 targetPos = blackboard.GetVector3(posKeyHash);
            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(damageTimerKey, 0f);

            Entity vortex = owner.Group.CreateEntity(vortexPrefab);
            if (vortex != null)
            {
                vortex.parent = null;
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
            blackboard.Remove(damageTimerKey);
            FrameEvent.EnqueueNamedEvent("Effect_Vortex_End", owner.Id);
            return NodeStatus.Success;
        }

        // 3. 吸引・ダメージロジック
        Vector3 center = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
        float dTimer = blackboard.GetFloat(damageTimerKey);
        bool applyDamage = false;
        
        if (currentTime >= dTimer)
        {
            applyDamage = true;
            blackboard.SetFloat(damageTimerKey, currentTime + damageInterval);
        }

        var entities = owner.Group.GetEntities();
        foreach (var e in entities)
        {
            if (e == null || e.Id == owner.Id) continue;
            if (blackboard.HasKey(entityIdKey) && e.Id == blackboard.GetInt(entityIdKey)) continue;

            float dist = Vector3.Distance(center, e.transform.position);
            if (dist <= suctionRadius && dist > 0.1f)
            {
                // 距離の2乗に反比例する吸引力 (F = force / dist^2)
                // ただし至近距離で無限大にならないようクランプ
                float power = suctionForce / (dist * dist + 1.0f);
                Vector3 pullDir = (center - e.transform.position).Normalized();
                e.transform.position += pullDir * power * Time.deltaTime;

                // 中心部ダメージ
                if (applyDamage && dist <= centerDamageRadius)
                {
                    if (e.name.Contains("Player"))
                    {
                        e.GetScript<HP>()?.TakeDamage(centerDamage);
                        Debug.Log("<color=red>[Vortex]</color> Player caught in center! Damage applied.");
                    }
                }
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
        blackboard.Remove(BehaviorTreeLoader.HashString("VortexDamageTimer_" + NodeIdHash));
    }
}

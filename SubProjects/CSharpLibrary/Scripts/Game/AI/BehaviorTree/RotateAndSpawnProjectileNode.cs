using System;
using System.Collections.Generic;

/// <summary>
/// ボスを一定時間回転させながら、その間に指定された間隔でプロジェクタイル（弾）を生成するノード。
/// </summary>
public class RotateAndSpawnProjectileNode : BehaviorNode
{
    public string projectilePrefab = "EnemyBullet";
    public float totalDuration = 3.0f;
    public float rotationSpeed = 360.0f;
    public float fireInterval = 0.2f;
    public float projectileSpeed = 15.0f;
    public float indicatorSize = 7.0f;
    
    // v2: 交互に使用する2つの射程距離
    public float fireDistance1 = 10.0f;
    public float fireDistance2 = 25.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("RotateAttackStart_" + NodeIdHash);
        uint lastFireTimeKey = BehaviorTreeLoader.HashString("LastFireTime_" + NodeIdHash);
        uint toggleKey = BehaviorTreeLoader.HashString("FireDistToggle_" + NodeIdHash);
        float currentTime = Time.time;

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(lastFireTimeKey, 0.0f);
            blackboard.SetInt(toggleKey, 0); // 0: distance1, 1: distance2
            
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null) intent.useDesiredRotation = false;

            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (elapsed >= totalDuration)
        {
            blackboard.Remove(startTimeKey);
            blackboard.Remove(lastFireTimeKey);
            blackboard.Remove(toggleKey);
            return NodeStatus.Success;
        }

        float deltaRot = rotationSpeed * Time.deltaTime;
        owner.transform.rotate *= Quaternion.MakeFromAxis(Vector3.up, deltaRot * Mathf.Deg2Rad);

        float lastFireTime = blackboard.GetFloat(lastFireTimeKey);
        if (elapsed - lastFireTime >= fireInterval)
        {
            int currentToggle = blackboard.GetInt(toggleKey);
            SpawnProjectile(owner, currentToggle);
            
            blackboard.SetFloat(lastFireTimeKey, elapsed);
            blackboard.SetInt(toggleKey, (currentToggle + 1) % 2);
        }

        return NodeStatus.Running;
    }

    private void SpawnProjectile(Entity owner, int toggle)
    {
        Entity projectile = owner.Group.CreateEntity(projectilePrefab);
        if (projectile != null)
        {
            projectile.parent = null;

            var bomb = projectile.GetScript<BossBomb>();
            Vector3 fireDir = owner.transform.rotate * Vector3.forward;
            Vector3 startPos = owner.transform.position + Vector3.up * 1.5f + fireDir * 2.0f;

            if (bomb != null)
            {
                // v2: 切り替えロジック
                float distance = (toggle == 0) ? fireDistance1 : fireDistance2;
                
                Vector3 targetPos = startPos + fireDir * distance;
                targetPos.y = 0.0f; 

                bomb.Launch(startPos, targetPos, 1.5f, 5.0f);

                // --- 予兆の設定 ---
                Entity telegraph = owner.Group.CreateEntity("TelegraphCircle");
                if (telegraph != null)
                {
                    telegraph.transform.position = new Vector3(targetPos.x, 0.05f, targetPos.z);
                    telegraph.transform.rotation = Quaternion.identity;
                    telegraph.transform.scale = new Vector3(indicatorSize, 0.05f, indicatorSize);

                    var timedDestruction = telegraph.GetScript<TimedDestruction>();
                    if (timedDestruction == null) timedDestruction = telegraph.AddScript<TimedDestruction>();
                    if (timedDestruction != null) timedDestruction.lifeTime = 1.6f;
                    
                    var renderer = telegraph.GetComponent<MeshRenderer>();
                    if (renderer != null) {
                        renderer.color = new Vector4(1.0f, 0.5f, 0.0f, 0.6f);
                    }
                }
            }
            
            FrameEvent.EnqueueNamedEvent("Effect_BossFire", owner.Id);
        }
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("RotateAttackStart_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("LastFireTime_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("FireDistToggle_" + NodeIdHash));
    }
}

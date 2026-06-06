using System;

/// <summary>
/// ターゲット座標（Blackboard）に向かってダメージ判定のあるビームを照射するノード。
/// C++側のエンジンに対し、ビームの開始位置、方向、長さをイベントとして送信する。
/// 
/// 仕様書v2対応：ヒット時のスロウ効果設定を追加。
/// </summary>
public class FireBeamNode : BehaviorNode
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public float damage = 50.0f;
    public float beamLength = 20.0f;
    public float beamRadius = 1.0f;
    public float duration = 2.0f;
    public float trackingRotationSpeed = 1.0f; // 照射中の追従速度

    public float slowMultiplier = 0.8f; // 20%低下
    public float slowDuration = 1.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("BeamStart_" + NodeIdHash);
        float currentTime = Time.time;

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            Debug.Log($"<color=red>[FireBeam]</color> {owner.name} started FIRING beam!");
            
            // --- 予測線の削除 ---
            uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
            if (blackboard.HasKey(telegraphKey))
            {
                int telegraphId = blackboard.GetInt(telegraphKey);
                if (telegraphId != 0) {
                    owner.Group.DestroyEntity(telegraphId);
                }
                blackboard.Remove(telegraphKey);
            }
            
            // --- ビームメッシュの生成 ---
            Entity beamEntity = owner.Group.CreateEntity("BossBeam");
            if (beamEntity != null)
            {
                beamEntity.parent = null; // 独立させる
                blackboard.SetInt(BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash), beamEntity.Id);
                
                // DamageTriggerのパラメータをノードの設定で上書き
                var trigger = beamEntity.GetScript<DamageTrigger>();
                if (trigger != null)
                {
                    trigger.damage = (int)damage;
                    trigger.interval = 0.1f; // 連続ヒット判定
                    trigger.slowMultiplier = slowMultiplier;
                    trigger.slowDuration = slowDuration;
                    Debug.Log($"<color=red>[FireBeam]</color> Beam configured with {damage} damage and {slowMultiplier} slow. Targeting Player/Reinforcements.");
                }

                // スポーン直後にトランスフォームを一度更新（1フレーム目の1,1,1防止）
                UpdateBeamTransform(beamEntity, owner, blackboard);
            }

            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (elapsed >= duration)
        {
            // --- ビームメッシュの削除 ---
            uint beamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
            if (blackboard.HasKey(beamKey))
            {
                int beamId = blackboard.GetInt(beamKey);
                if (beamId != 0) owner.Group.DestroyEntity(beamId);
                blackboard.Remove(beamKey);
            }

            blackboard.Remove(startTimeKey);
            Debug.Log($"<color=red>[FireBeam]</color> {owner.name} finished FIRING beam.");
            
            return NodeStatus.Success;
        }

        // 毎フレームTransformを更新
        uint currentBeamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
        if (blackboard.HasKey(currentBeamKey))
        {
            Entity beamEntity = owner.Group.GetEntity(blackboard.GetInt(currentBeamKey));
            if (beamEntity != null)
            {
                UpdateBeamTransform(beamEntity, owner, blackboard);
            }
        }

        return NodeStatus.Running;
    }

    private void UpdateBeamTransform(Entity beamEntity, Entity owner, Blackboard blackboard)
    {
        Vector3 targetPos = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
        Vector3 bossPos = owner.transform.position;
        Vector3 diff = new Vector3(targetPos.x - bossPos.x, 0.0f, targetPos.z - bossPos.z);
        Vector3 direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : owner.transform.forward;

        // ボス自身もターゲットの方向を向く
        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            intent.desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            intent.rotationSpeed = trackingRotationSpeed; // 追従速度を設定
            intent.useDesiredRotation = true;
        }

        Vector3 horizontalBoss = new Vector3(bossPos.x, 0.5f, bossPos.z);
        Vector3 visualStartPos = horizontalBoss + direction * 3.0f;
        
        beamEntity.transform.position = visualStartPos + direction * (beamLength * 0.5f);
        
        Quaternion baseRot = Quaternion.LookRotation(direction).Conjugate();
        Quaternion x90 = Quaternion.MakeFromAxis(new Vector3(1, 0, 0), 90.0f * Mathf.Deg2Rad);
        beamEntity.transform.rotation = baseRot * x90;

        beamEntity.transform.scale = new Vector3(beamRadius * 2.0f, beamLength * 0.5f, beamRadius * 2.0f);

        // デバッグ表示 (ビームの軌跡を赤い線、着弾点を赤い円で表示、太さ12.0)
        GizmoBatch.DrawLine(visualStartPos, visualStartPos + direction * beamLength, new Vector4(1, 0, 0, 1), 12.0f);
        GizmoBatch.DrawWireCircle(targetPos + Vector3.up * 0.1f, beamRadius * 2.0f, new Vector4(1, 0.5f, 0, 1), 16, 12.0f);
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("BeamStart_" + NodeIdHash));
        
        uint beamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
        if (blackboard.HasKey(beamKey))
        {
            int beamId = blackboard.GetInt(beamKey);
            if (beamId != 0) owner.Group.DestroyEntity(beamId);
            blackboard.Remove(beamKey);
        }
    }
}

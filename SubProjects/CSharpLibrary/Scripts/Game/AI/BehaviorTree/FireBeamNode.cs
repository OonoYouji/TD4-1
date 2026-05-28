using System;

/// <summary>
/// ターゲット座標（Blackboard）に向かってダメージ判定のあるビームを照射するノード。
/// C++側のエンジンに対し、ビームの開始位置、方向、長さをイベントとして送信する。
/// </summary>
public class FireBeamNode : BehaviorNode
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public float damage = 50.0f;
    public float beamLength = 20.0f;
    public float beamRadius = 1.0f;
    public float duration = 2.0f;

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
                }
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

        // ビームの方向を計算（現在の向き、またはターゲット座標へ）
        Vector3 targetPos = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
        Vector3 bossPos = owner.transform.position;
        Vector3 diff = targetPos - bossPos;
        
        if (diff.sqrMagnitude > 0.001f)
        {
            Vector3 direction = diff.Normalized();
            
            // --- ボス自身もターゲットの方向を向くように設定（ビーム中も追従可能にする） ---
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null)
            {
                intent.desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                intent.useDesiredRotation = true;
            }

            // --- ビームメッシュのTransform更新 ---
            uint beamKey = BehaviorTreeLoader.HashString("BeamEntityID_" + NodeIdHash);
            if (blackboard.HasKey(beamKey))
            {
                Entity beamEntity = owner.Group.GetEntity(blackboard.GetInt(beamKey));
                if (beamEntity != null)
                {
                    Vector3 startPos = new Vector3(bossPos.x, 0.5f, bossPos.z) + direction * 3.0f;
                    beamEntity.transform.position = startPos;
                    beamEntity.transform.rotation = Quaternion.LookRotation(direction).Conjugate();
                    beamEntity.transform.scale = new Vector3(beamRadius * 2.0f, beamRadius * 2.0f, beamLength);
                }
            }
        }

        return NodeStatus.Running;
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

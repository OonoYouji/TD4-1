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
            intent.useDesiredRotation = true;
        }

        Vector3 horizontalBoss = new Vector3(bossPos.x, 0.5f, bossPos.z);
        Vector3 visualStartPos = horizontalBoss + direction * 3.0f;
        
        // シリンダーメッシュの原点は中心にあるため、端を起点にするには
        // 座標を進行方向に「長さの半分」だけずらす必要がある。
        beamEntity.transform.position = visualStartPos + direction * (beamLength * 0.5f);
        
        // 基本の向き（進行方向）を計算
        Quaternion baseRot = Quaternion.LookRotation(direction).Conjugate();
        // シリンダーはデフォルトでY軸（垂直）を向いているため、X軸で90度回転させてZ軸（水平進行方向）に合わせる
        Quaternion x90 = Quaternion.MakeFromAxis(new Vector3(1, 0, 0), 90.0f * Mathf.Deg2Rad);
        beamEntity.transform.rotation = baseRot * x90;

        // スケールの適用
        beamEntity.transform.scale = new Vector3(beamRadius * 2.0f, beamLength * 0.5f, beamRadius * 2.0f);
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

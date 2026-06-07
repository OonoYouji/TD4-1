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
    public float beamLength = 100.0f; // 長さを十分に確保
    public float beamRadius = 1.0f;   // 当たり判定の太さ
    public float duration = 2.0f;
    public float trackingRotationSpeed = 1.0f; // 照射中の追従速度

    // --- 追加の調整用プロパティ ---
    public float beamHeight = 8.0f;       // 発射の高さ
    public float beamOffsetForward = 3.0f; // ボスの中心からの前方オフセット

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
                }

                // スポーン直後にトランスフォームを一度更新
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
        Vector3 diff = targetPos - bossPos;
        diff.y = 0.0f;
        Vector3 direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : owner.transform.forward;

        // ボス自身もターゲットの方向を向く
        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            intent.desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            intent.rotationSpeed = trackingRotationSpeed;
            intent.useDesiredRotation = true;
        }

        // 高さとオフセットをプロパティから適用
        Vector3 emissionPos = new Vector3(bossPos.x, bossPos.y + beamHeight, bossPos.z);
        Vector3 visualStartPos = emissionPos + direction * beamOffsetForward;
        
        beamEntity.transform.position = visualStartPos + direction * (beamLength * 0.5f);
        
        Quaternion baseRot = Quaternion.LookRotation(direction).Conjugate();
        Quaternion x90 = Quaternion.MakeFromAxis(new Vector3(1, 0, 0), 90.0f * Mathf.Deg2Rad);
        beamEntity.transform.rotation = baseRot * x90;

        // 太さと長さを適用
        beamEntity.transform.scale = new Vector3(beamRadius * 2.0f, beamLength * 0.5f, beamRadius * 2.0f);

        // --- コライダー（当たり判定）の可視化 ---
        // 実際のコライダーの形状に合わせて、開始点、中間、終点に円を表示し、線で繋ぐ
        Vector3 beamEndPos = visualStartPos + direction * beamLength;
        Vector3 beamMidPos = visualStartPos + direction * (beamLength * 0.5f);

        // 判定範囲を赤色で強調
        Vector4 colliderColor = new Vector4(1, 0, 0, 1);
        GizmoBatch.DrawWireCircle(visualStartPos, beamRadius, colliderColor, 16, 12.0f);
        GizmoBatch.DrawWireCircle(beamMidPos, beamRadius, colliderColor, 16, 12.0f);
        GizmoBatch.DrawWireCircle(beamEndPos, beamRadius, colliderColor, 16, 12.0f);

        // 筒の側面を4本の線で表現
        Vector3 right = Vector3.Cross(direction, Vector3.up).Normalized() * beamRadius;
        Vector3 up = Vector3.Cross(right, direction).Normalized() * beamRadius;

        GizmoBatch.DrawLine(visualStartPos + right, beamEndPos + right, colliderColor, 12.0f);
        GizmoBatch.DrawLine(visualStartPos - right, beamEndPos - right, colliderColor, 12.0f);
        GizmoBatch.DrawLine(visualStartPos + up, beamEndPos + up, colliderColor, 12.0f);
        GizmoBatch.DrawLine(visualStartPos - up, beamEndPos - up, colliderColor, 12.0f);

        // デバッグ表示 (着弾点の地面マーカー)
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

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
            
            // 視覚演出の開始イベントを発行
            FrameEvent.EnqueueEffectEvent("BossBeam_Start", owner.Id, beamRadius, duration);
            
            // --- 予測線の削除 ---
            uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID");
            if (blackboard.HasKey(telegraphKey))
            {
                int telegraphId = blackboard.GetInt(telegraphKey);
                owner.Group.DestroyEntity(telegraphId);
                blackboard.Remove(telegraphKey);
            }
            
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (elapsed >= duration)
        {
            blackboard.Remove(startTimeKey);
            Debug.Log($"<color=red>[FireBeam]</color> {owner.name} finished FIRING beam.");
            
            // 視覚演出の終了イベントを発行
            FrameEvent.EnqueueNamedEvent("Effect_BossBeam_End", owner.Id);
            
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

            // C++側にビーム攻撃イベントを送信（毎フレームまたは一定間隔）
            FrameEvent.EnqueueAttackEvent(
                "BossBeam",
                owner.Id,
                damage * Time.deltaTime, // 持続ダメージ
                beamRadius,
                0.1f,
                0.0f,
                1.0f
            );
        }

        return NodeStatus.Running;
    }
}

using System;

/// <summary>
/// 攻撃開始前の「溜め（予兆）」時間を管理するノード。
/// 単なる待機ではなく、指定されたエフェクトの発行や、ターゲットへの継続的な追尾（必要なら）を行う。
/// </summary>
public class AttackTelegraphWaitNode : BehaviorNode
{
    /// <summary>
    /// 溜め時間（秒）。
    /// </summary>
    public float duration = 1.5f;

    [BlackboardKey]
    public string durationKey = "";

    /// <summary>
    /// 溜め開始時に発行する演出イベント名（例："Effect_BossCharge", "Voice_Roar"）。
    /// </summary>
    public string startEventName = "";

    /// <summary>
    /// 溜め中にターゲットを追従し続けるか。
    /// </summary>
    public bool trackTarget = true;

    /// <summary>
    /// 追従する場合の回転速度。
    /// </summary>
    public float rotationSpeed = 2.0f;

    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("TelegraphStart_" + NodeIdHash);
        float currentTime = Time.time;

        float finalDuration = duration;
        if (!string.IsNullOrEmpty(durationKey)) {
            uint keyHash = BehaviorTreeLoader.HashString(durationKey);
            if (blackboard.HasKey(keyHash)) {
                finalDuration = blackboard.GetFloat(keyHash, duration);
                if (finalDuration == duration) {
                    finalDuration = (float)blackboard.GetInt(keyHash, (int)duration);
                }
            }
        }

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            
            if (!string.IsNullOrEmpty(startEventName))
            {
                FrameEvent.EnqueueNamedEvent(startEventName, owner.Id);
            }

            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (elapsed >= finalDuration)
        {
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        // 追従ロジック
        if (trackTarget)
        {
            uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (blackboard.HasKey(posKeyHash))
            {
                Vector3 targetPos = blackboard.GetVector3(posKeyHash);
                Vector3 bossPos = owner.transform.position;
                Vector3 diff = new Vector3(targetPos.x - bossPos.x, 0.0f, targetPos.z - bossPos.z);
                
                if (diff.sqrMagnitude > 0.001f)
                {
                    Vector3 direction = diff.Normalized();
                    var intent = owner.GetComponent<AgentIntentComponent>();
                    if (intent != null)
                    {
                        intent.desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                        intent.rotationSpeed = rotationSpeed;
                        intent.useDesiredRotation = true;
                    }
                }
            }
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("TelegraphStart_" + NodeIdHash));
    }
}

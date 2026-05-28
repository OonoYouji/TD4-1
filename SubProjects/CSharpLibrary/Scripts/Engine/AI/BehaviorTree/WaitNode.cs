using System;

/// <summary>
/// 指定された時間だけ待機するシンプルなアクションノード。
/// </summary>
public class WaitNode : BehaviorNode
{
    /// <summary>
    /// 待機時間（秒）。
    /// </summary>
    public float duration = 1.0f;

    /// <summary>
    /// Blackboardから待機時間を取得する場合のキー名。
    /// 指定されている場合は duration よりもこちらが優先される。
    /// </summary>
    [BlackboardKey]
    public string durationKey = "";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("WaitStart_" + NodeIdHash);
        float currentTime = Time.time;

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            return NodeStatus.Running;
        }

        float finalDuration = duration;
        if (!string.IsNullOrEmpty(durationKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(durationKey);
            finalDuration = blackboard.GetFloat(keyHash, duration);
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        if (currentTime - startTime >= finalDuration)
        {
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("WaitStart_" + NodeIdHash));
    }
}

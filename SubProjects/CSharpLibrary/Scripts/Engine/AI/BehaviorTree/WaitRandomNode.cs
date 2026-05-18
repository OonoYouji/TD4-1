using System;

/// <summary>
/// 指定された範囲内のランダムな時間だけ待機するタスク。
/// </summary>
public class WaitRandomNode : BehaviorNode
{
    public float minDuration = 1.0f;
    public float maxDuration = 2.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("WaitStart_" + NodeIdHash);
        uint durationKey = BehaviorTreeLoader.HashString("WaitDur_" + NodeIdHash);

        float currentTime = Time.time;

        // 初回実行時
        if (!blackboard.HasKey(startTimeKey))
        {
            float duration = minDuration + (float)new Random().NextDouble() * (maxDuration - minDuration);
            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(durationKey, duration);
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float targetDuration = blackboard.GetFloat(durationKey);

        if (currentTime - startTime >= targetDuration)
        {
            blackboard.Remove(startTimeKey);
            blackboard.Remove(durationKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("WaitStart_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("WaitDur_" + NodeIdHash));
    }
}

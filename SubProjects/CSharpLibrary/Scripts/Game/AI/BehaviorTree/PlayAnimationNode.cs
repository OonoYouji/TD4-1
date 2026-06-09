using System;

/// <summary>
/// アニメーションを再生するアクションノード。
/// 指定された時間（duration）内にアニメーションが完了するように再生速度を自動調整します。
/// </summary>
public class PlayAnimationNode : BehaviorNode
{
    public string clipName = "";
    
    /// <summary>
    /// 0以上の場合は、この時間（秒）に合わせて再生速度を調整します。
    /// </summary>
    public float duration = 1.0f; 
    
    [BlackboardKey]
    public string durationKey = "";

    /// <summary>
    /// ループ再生するか。
    /// </summary>
    public bool isLoop = false;

    /// <summary>
    /// アニメーションの完了を待機するか。
    /// </summary>
    public bool isWait = true;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("PlayAnimStart_" + NodeIdHash);
        float currentTime = Time.time;

        if (!blackboard.HasKey(startTimeKey))
        {
            var animator = owner.GetComponent<Animator>();
            if (animator == null) return NodeStatus.Failure;

            // 1. 目標とする再生時間を決定
            float targetDuration = duration;
            if (!string.IsNullOrEmpty(durationKey))
            {
                uint keyHash = BehaviorTreeLoader.HashString(durationKey);
                if (blackboard.HasKey(keyHash))
                {
                    targetDuration = blackboard.GetFloat(keyHash, duration);
                    if (targetDuration == duration) targetDuration = (float)blackboard.GetInt(keyHash, (int)duration);
                }
            }

            // 2. 再生開始
            // Animator.cs に定義されている CrossFadeWithDuration を使用して、
            // 速度計算と再生を一括で行う
            animator.CrossFadeWithDuration(clipName, targetDuration, 0.1f);
            animator.SetLoop(isLoop, 0);

            if (!isWait)
            {
                return NodeStatus.Success;
            }

            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(BehaviorTreeLoader.HashString("PlayAnimTargetTime_" + NodeIdHash), targetDuration);
            
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float totalWait = blackboard.GetFloat(BehaviorTreeLoader.HashString("PlayAnimTargetTime_" + NodeIdHash));

        if (currentTime - startTime >= totalWait)
        {
            blackboard.Remove(startTimeKey);
            blackboard.Remove(BehaviorTreeLoader.HashString("PlayAnimTargetTime_" + NodeIdHash));
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("PlayAnimStart_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("PlayAnimTargetTime_" + NodeIdHash));
    }
}

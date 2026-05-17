using System;

/// <summary>
/// 実行後にクールダウン時間を設け、連続実行を制限するデコレーター。
/// </summary>
public class CooldownDecorator : BehaviorDecorator
{
    public float cooldownTime = 5.0f;

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString("LastExecution_" + NodeIdHash);
        float lastTime = blackboard.GetFloat(key, -100.0f);
        
        if (Time.time - lastTime >= cooldownTime)
        {
            // 実行許可。LastExecutionの更新はタスクが成功/終了したタイミングで行うのが理想だが、
            // 簡易化のため開始時にセット。
            blackboard.SetFloat(key, Time.time);
            return true;
        }
        return false;
    }
}

/// <summary>
/// 指定した回数だけノードをループ実行させるデコレーター。
/// </summary>
public class LoopDecorator : BehaviorDecorator
{
    public int loopCount = 3;
    public bool infinite = false;

    public override bool CalculateCondition(Blackboard blackboard, Entity owner) => true;

    public override NodeStatus PostProcessStatus(NodeStatus currentStatus, Blackboard blackboard)
    {
        if (currentStatus == NodeStatus.Running) return NodeStatus.Running;

        // 成功または失敗した際、カウントを進める
        uint key = BehaviorTreeLoader.HashString("LoopIdx_" + NodeIdHash);
        int currentIdx = blackboard.GetInt(key, 0);

        if (infinite) return NodeStatus.Running; // 無限ループなら常にRunningを返して再挑戦させる

        currentIdx++;
        if (currentIdx < loopCount)
        {
            blackboard.SetInt(key, currentIdx);
            return NodeStatus.Running; // まだ回数が残っているのでRunningを返して継続させる
        }

        // 終了
        blackboard.SetInt(key, 0);
        return currentStatus;
    }
}

/// <summary>
/// 子ノードの結果に関わらず、常に成功を返すデコレーター。
/// </summary>
public class ForceSuccessDecorator : BehaviorDecorator
{
    public override bool CalculateCondition(Blackboard blackboard, Entity owner) => true;

    public override NodeStatus PostProcessStatus(NodeStatus currentStatus, Blackboard blackboard)
    {
        if (currentStatus == NodeStatus.Running) return NodeStatus.Running;
        return NodeStatus.Success;
    }
}

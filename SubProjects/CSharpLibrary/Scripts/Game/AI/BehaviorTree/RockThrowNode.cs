using System;

/// <summary>
/// 持ち上げられた岩をターゲット地点へ投げ飛ばす（落下させる）アクションノード。
/// </summary>
public class RockThrowNode : BehaviorNode
{
    [BlackboardKey]
    public string objectKey = "TargetRock";

    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public float impactRadius = 3.0f;
    
    [BlackboardKey]
    public string radiusKey = "RockRadius";

    public float delay = 0.0f;

    [BlackboardKey]
    public string delayKey = "";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("RockThrowStart_" + NodeIdHash);
        float currentTime = Time.time;

        float finalDelay = delay;
        if (!string.IsNullOrEmpty(delayKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(delayKey);
            if (blackboard.HasKey(keyHash))
            {
                finalDelay = blackboard.GetFloat(keyHash, delay);
                if (finalDelay == delay) finalDelay = (float)blackboard.GetInt(keyHash, (int)delay);
            }
        }

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        if (currentTime - startTime < finalDelay)
        {
            return NodeStatus.Running;
        }

        blackboard.Remove(startTimeKey);

        Entity rock = blackboard.GetEntity(BehaviorTreeLoader.HashString(objectKey));
        if (rock == null) return NodeStatus.Failure;

        var fallingRock = rock.GetScript<FallingRock>();
        if (fallingRock == null)
        {
            fallingRock = rock.AddScript<FallingRock>();
        }

        if (fallingRock != null)
        {
            Vector3 targetPos = Vector3.zero;
            uint targetKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (blackboard.HasKey(targetKeyHash))
            {
                targetPos = blackboard.GetVector3(targetKeyHash);
            }

            float finalRadius = impactRadius;
            if (!string.IsNullOrEmpty(radiusKey))
            {
                uint keyHash = BehaviorTreeLoader.HashString(radiusKey);
                if (blackboard.HasKey(keyHash))
                {
                    finalRadius = blackboard.GetFloat(keyHash, impactRadius);
                    if (finalRadius == impactRadius) finalRadius = (float)blackboard.GetInt(keyHash, (int)impactRadius);
                }
            }

            fallingRock.impactRadius = finalRadius;
            fallingRock.Launch(targetPos);
        }

        FrameEvent.EnqueueNamedEvent("Effect_RockThrow", owner.Id);
        
        return NodeStatus.Success;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("RockThrowStart_" + NodeIdHash));
    }
}

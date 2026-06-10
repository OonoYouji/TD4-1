using System;

/// <summary>
/// 岩をボスの頭上まで持ち上げるアクションノード。
/// </summary>
public class RockLiftNode : BehaviorNode
{
    [BlackboardKey]
    public string objectKey = "TargetRock";

    public float liftHeight = 10.0f;
    public float liftDuration = 1.5f;

    [BlackboardKey]
    public string durationKey = "RockPrepTime";

    [BlackboardKey]
    public string forwardKey = "";

    public float forwardOffset = 0.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("RockLiftStart_" + NodeIdHash);
        float currentTime = Time.time;

        Entity rock = blackboard.GetEntity(BehaviorTreeLoader.HashString(objectKey));
        if (rock == null) return NodeStatus.Failure;

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            // 最初はボスの足元からスタート
            rock.transform.position = owner.transform.position;
            rock.parent = null;
        }

        float finalDuration = liftDuration;
        if (!string.IsNullOrEmpty(durationKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(durationKey);
            if (blackboard.HasKey(keyHash))
            {
                finalDuration = blackboard.GetFloat(keyHash, liftDuration);
                if (finalDuration == liftDuration) finalDuration = (float)blackboard.GetInt(keyHash, (int)liftDuration);
            }
        }

        Vector3 offset = new Vector3(0, liftHeight, forwardOffset);
        if (!string.IsNullOrEmpty(forwardKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(forwardKey);
            object val = blackboard.GetValueAsObject(keyHash);
            if (val is Vector3 v3)
            {
                // Vector3の場合は [Right, Up, Forward] のローカルオフセットとして扱う
                offset = v3;
            }
            else if (val is float f)
            {
                offset.z = f;
            }
            else if (val is int i)
            {
                offset.z = (float)i;
            }
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;
        float progress = Math.Min(elapsed / Math.Max(0.001f, finalDuration), 1.0f);

        // ローカルオフセットをワールド座標に変換してリフトアップ
        Vector3 targetPos = owner.transform.position 
            + owner.transform.right * offset.x 
            + owner.transform.up * offset.y 
            + owner.transform.forward * offset.z;

        rock.transform.position = Vector3.Lerp(owner.transform.position, targetPos, progress);

        if (progress >= 1.0f)
        {
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("RockLiftStart_" + NodeIdHash));
    }
}

using System;

/// <summary>
/// 指定されたエフェクト（GameEvents.json で定義されたもの）を生成するアクションノード。
/// 位置はオーナーの現在地、または指定されたBlackboardの座標を使用する。
/// </summary>
public class SpawnEffectNode : BehaviorNode
{
    public string effectName = "";
    public float scale = 1.0f;
    public float duration = 2.0f;

    [BlackboardKey]
    public string targetPosKey = "";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        if (string.IsNullOrEmpty(effectName)) return NodeStatus.Failure;

        Vector3 spawnPos = owner.transform.position;
        if (!string.IsNullOrEmpty(targetPosKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (blackboard.HasKey(keyHash))
            {
                spawnPos = blackboard.GetVector3(keyHash);
            }
        }

        // C++側のエフェクトシステムへ通知
        FrameEvent.EnqueueEffectEvent(effectName, owner.Id, scale, duration);
        
//         Debug.Log($"[SpawnEffect] '{effectName}' spawned at {Vector3.ToSimpleString(spawnPos)}");

        return NodeStatus.Success;
    }
}

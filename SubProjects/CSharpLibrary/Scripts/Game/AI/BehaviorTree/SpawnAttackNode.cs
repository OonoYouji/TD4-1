using System;

/// <summary>
/// 攻撃（当たり判定）を生成するためのノード。
/// C++側のイベントシステムに詳細なパラメータを送信する。
/// </summary>
public class SpawnAttackNode : BehaviorNode
{
    public string attackName = ""; // NEW: Preset name from Game Event Editor
    public float damage = 10.0f;
    public float radius = 2.0f;
    public float duration = 0.1f;
    public float offsetForward = 1.0f;
    public float offsetUp = 1.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        string logMsg = string.IsNullOrEmpty(attackName) 
            ? $"Damage={damage}, Radius={radius}" 
            : $"Preset={attackName}";

        
        FrameEvent.EnqueueAttackEvent(
            attackName,
            owner.Id, 
            damage, 
            radius, 
            duration, 
            offsetForward, 
            offsetUp
        );

        return NodeStatus.Success;
    }
}


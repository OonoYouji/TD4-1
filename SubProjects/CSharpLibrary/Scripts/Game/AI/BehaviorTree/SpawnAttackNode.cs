using System;

/// <summary>
/// 攻撃（当たり判定）を生成するためのノード。
/// C++側のイベントシステムに詳細なパラメータを送信する。
/// </summary>
public class SpawnAttackNode : BehaviorNode
{
    public float damage = 10.0f;
    public float radius = 2.0f;
    public float duration = 0.1f;
    public float offsetForward = 1.0f;
    public float offsetUp = 1.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        Debug.Log($"<color=red>[SpawnAttack]</color> {owner.name} spawning attack: <b>Damage={damage}, Radius={radius}</b>");
        
        FrameEvent.EnqueueAttackEvent(
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

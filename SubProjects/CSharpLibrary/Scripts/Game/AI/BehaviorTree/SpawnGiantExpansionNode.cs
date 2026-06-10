using System;

/// <summary>
/// ボスの足元に拡大する範囲攻撃（GiantExpansion）を生成するノード。
/// </summary>
public class SpawnGiantExpansionNode : BehaviorNode
{
    public float maxRadius = 15.0f;
    public float duration = 1.0f;
    public float knockbackForce = 15.0f;
    public int damage = 10;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        Entity area = owner.Group.CreateEntity("BossGiantArea");
        if (area != null)
        {
            area.parent = null; // 独立させる
            
            // ボスの足元に配置
            area.transform.position = owner.transform.position;
            
            // トリガー設定を適用（押し戻し防止）
            var sphere = area.GetComponent<SphereCollider>();
            if (sphere != null) sphere.isTrigger = true;
            var box = area.GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = true;
            
            // パラメータの上書き
            var expansion = area.GetScript<GiantExpansion>();
            if (expansion != null)
            {
                expansion.maxRadius = maxRadius;
                expansion.duration = duration;
                expansion.knockbackForce = knockbackForce;
                expansion.damage = damage;
            }

//             Debug.Log($"<color=magenta>[GiantAttack]</color> Spawned expansion area at {Vector3.ToSimpleString(owner.transform.position)}");
            return NodeStatus.Success;
        }

        return NodeStatus.Failure;
    }
}

using System;

/// <summary>
/// ボスの移動中、進行方向にある障害物（"Rock"タグのオブジェクト）を押し退けるサービス。
/// ボスが障害物に引っかかって停止するのを防ぐ。
/// </summary>
public class PhysicsPushService : BehaviorService
{
    public float pushForce = 10.0f;
    public float detectDistance = 3.0f;
    public string obstacleTag = "Rock";

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent == null || intent.desiredMoveDirection.sqrMagnitude < 0.01f)
            return;

        Vector3 moveDir = intent.desiredMoveDirection.Normalized();
        Vector3 checkPos = owner.transform.position + (moveDir * detectDistance);

        // フィールド上のエンティティを走査
        var entities = owner.Group.GetEntities();
        foreach (var entity in entities)
        {
            if (entity.Id == owner.Id) continue;
            if (!entity.name.Contains(obstacleTag)) continue;

            float dist = Vector3.Distance(owner.transform.position, entity.transform.position);
            if (dist < detectDistance)
            {
                // 簡易的な物理押し出し（本来はC++側の物理エンジンに任せるべきだが、
                // C#側からTransformを微操作して「動かしている」ように見せる）
                Vector3 pushDir = (entity.transform.position - owner.transform.position).Normalized();
                entity.transform.position += pushDir * pushForce * Interval;
                
                // Debug.Log($"[PhysicsPush] Pushing {entity.name} away from {owner.name}");
            }
        }
    }
}

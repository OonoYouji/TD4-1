using System;

/// <summary>
/// プレイヤーに向かって移動するアクションノード。
/// </summary>
public class MoveToPlayerNode : BehaviorNode
{
    public float StopDistance { get; set; } = 2.0f;

    public MoveToPlayerNode(float stopDistance = 2.0f)
    {
        StopDistance = stopDistance;
    }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // プレイヤーを検索（簡易的に名前やスクリプトで検索）
        // 本来はマネージャー等から取得すべきだが、一旦ECSGroupから全探索
        Entity player = FindPlayer(owner);
        if (player == null) return NodeStatus.Failure;

        Vector3 playerPos = player.transform.position;
        Vector3 ownerPos = owner.transform.position;
        
        Vector3 diff = playerPos - ownerPos;
        float distance = diff.Length();

        if (distance <= StopDistance)
        {
            // 到着
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null)
            {
                intent.desiredMoveDirection = Vector3.zero;
            }
            return NodeStatus.Success;
        }

        // 移動方向を設定
        var aiIntent = owner.GetComponent<AgentIntentComponent>();
        if (aiIntent != null)
        {
            aiIntent.desiredMoveDirection = diff.Normalized();
        }

        return NodeStatus.Running;
    }

    private Entity FindPlayer(Entity owner)
    {
        // とりあえず "Player" という名前のEntityを探す
        // EntityComponentSystem.GetECSGroup("Game") などから探す必要がある
        var group = EntityComponentSystem.GetECSGroup("Game");
        if (group == null) return null;

        return group.FindEntity("Player");
    }
}

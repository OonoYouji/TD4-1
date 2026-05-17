using System;

/// <summary>
/// プレイヤーに向かって移動するアクションノード。
/// </summary>
public class MoveToPlayerNode : BehaviorNode
{
    public float StopDistance { get; set; } = 2.0f;

    public MoveToPlayerNode() { }

    public MoveToPlayerNode(float stopDistance = 2.0f)
    {
        StopDistance = stopDistance;
    }

    public override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // プレイヤーを検索
        Entity player = FindPlayer(owner);
        if (player == null) {
            Debug.LogWarning($"MoveToPlayerNode: Player not found from owner {owner.Id}");
            return NodeStatus.Failure;
        }

        // position を使用（親がいない前提）
        Vector3 playerPos = player.transform.position;
        Vector3 ownerPos = owner.transform.position;

        Vector3 diff = playerPos - ownerPos;
        float distance = diff.Length();

        // 非常に詳細なログ
        Debug.Log($"MoveToPlayerNode: [Owner:{owner.name} ID:{owner.Id}] Pos={ownerPos}");
        Debug.Log($"MoveToPlayerNode: [Player:{player.name} ID:{player.Id}] Pos={playerPos}");
        Debug.Log($"MoveToPlayerNode: Diff={diff}, Dist={distance}");

        if (distance <= StopDistance)
        {
            // 到着
            Debug.Log($"MoveToPlayerNode: Arrived at player.");
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
            Vector3 dir = diff.Normalized();
            aiIntent.desiredMoveDirection = dir;
            Debug.Log($"MoveToPlayerNode: SET INTENT DIRECTION: {aiIntent.desiredMoveDirection}");
        }

        return NodeStatus.Running;
    }


    private Entity FindPlayer(Entity owner)
    {
        // まず、オーナーと同じグループから探す
        var group = owner.Group;
        if (group != null) {
            var p = group.FindEntity("Player");
            if (p != null) return p;
        }

        // 次に、一般的に使われるグループ名で探す
        string[] commonGroups = { "GameScene", "Game", "Debug", "PlayerDevelopScene", "Workspace_PlayerBullet" };
        foreach (var name in commonGroups) {
            var g = EntityComponentSystem.GetECSGroup(name);
            if (g != null) {
                var p = g.FindEntity("Player");
                if (p != null) return p;
            }
        }

        return null;
    }

}

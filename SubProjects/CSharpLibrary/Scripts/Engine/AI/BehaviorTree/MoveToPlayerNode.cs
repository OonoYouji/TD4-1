using System;

/// <summary>
/// プレイヤー（または指定された対象）に向かって移動を試みるアクションノード。
/// Transformを直接操作するのではなく、AIの「意図（AgentIntentComponent）」に移動方向を書き込み、
/// 実際の移動処理はC++側のMovementSystemに委譲するアーキテクチャを採用している。
/// </summary>
public class MoveToPlayerNode : BehaviorNode
{
    /// <summary>
    /// ターゲットにどれだけ近づいたら移動を完了（Success）とみなすかの距離。
    /// </summary>
    public float stopDistance = 2.0f;

    public MoveToPlayerNode() { }

    public MoveToPlayerNode(float stopDistance = 2.0f)
    {
        this.stopDistance = stopDistance;
    }

    /// <summary>
    /// プレイヤーの位置を特定し、そこへの方向ベクトルを計算してIntentに設定する。
    /// </summary>
    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        // 1. プレイヤーエンティティを検索
        Entity player = FindPlayer(owner);
        if (player == null) {
            Debug.LogWarning($"MoveToPlayerNode: Player not found from owner {owner.Id}");
            return NodeStatus.Failure;
        }

        Vector3 playerPos = player.transform.position;
        Vector3 ownerPos = owner.transform.position;

        // 2. プレイヤーへのベクトルと距離を計算
        Vector3 diff = playerPos - ownerPos;
        float distance = diff.Length();

        // （デバッグ用のログ出力）
        Debug.Log($"MoveToPlayerNode: [Owner:{owner.name} ID:{owner.Id}] Pos={ownerPos}");
        Debug.Log($"MoveToPlayerNode: [Player:{player.name} ID:{player.Id}] Pos={playerPos}");
        Debug.Log($"MoveToPlayerNode: Diff={diff}, Dist={distance}");

        // 3. 到着判定
        if (distance <= stopDistance)
        {
            Debug.Log($"MoveToPlayerNode: Arrived at player.");
            
            // 到着したため、移動の意図をリセットして停止させる
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null)
            {
                intent.desiredMoveDirection = Vector3.zero;
            }
            return NodeStatus.Success;
        }

        // 4. 移動方向の設定（Running状態）
        var aiIntent = owner.GetComponent<AgentIntentComponent>();
        if (aiIntent != null)
        {
            // プレイヤーの方向へ向かう正規化ベクトルをIntentに書き込む
            Vector3 dir = diff.Normalized();
            aiIntent.desiredMoveDirection = dir;
            Debug.Log($"MoveToPlayerNode: SET INTENT DIRECTION: {aiIntent.desiredMoveDirection}");
        }

        // まだ到着していないため、次フレームも継続して評価する
        return NodeStatus.Running;

    }

    /// <summary>
    /// プレイヤーエンティティをECSグループから検索する内部メソッド。
    /// </summary>
    private Entity FindPlayer(Entity owner)
    {
        // まず、オーナーと同じグループから探す
        var group = owner.Group;
        if (group != null) {
            var p = group.FindEntity("Player");
            if (p != null) return p;
        }

        // 見つからなければ、一般的なゲームシーンのグループから探す
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

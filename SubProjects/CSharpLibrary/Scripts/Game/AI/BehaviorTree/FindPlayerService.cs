using System;

/// <summary>
/// プレイヤーエンティティを定期的に検索し、見つかった場合にそのIDをBlackboardに書き込むサービス。
/// （※現在は名前ベースの運用に移行中だが、IDの直接参照が必要なケースで使用される）
/// </summary>
public class FindPlayerService : BehaviorService
{
    /// <summary>
    /// プレイヤーのIDを保存するBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetIdKey = "TargetId";

    /// <summary>
    /// 定期的に実行されるプレイヤー検索処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint idKeyHash = BehaviorTreeLoader.HashString(targetIdKey);
        
        // すでに有効なターゲットがいればスキップ（必要に応じて再検索ロジックを入れる）
        if (blackboard.HasKey(idKeyHash) && blackboard.GetInt(idKeyHash) != 0) return;

        // プレイヤーを検索
        Entity player = FindPlayer(owner);
        if (player != null)
        {
            blackboard.SetInt(idKeyHash, player.Id);
            Debug.Log($"<color=green>[FindPlayer]</color> Found 'Player' (ID:{player.Id}) in group '{player.Group.groupName}'");
        }
        else
        {
            // 定期的に警告を出す（デバッグ用）
            // Debug.LogWarning($"<color=red>[FindPlayer]</color> {owner.name} could not find 'Player' in any common groups.");
        }
    }

    private Entity FindPlayer(Entity owner)
    {
        // 1. 自分のグループを最優先
        Entity pInGroup = owner.Group.FindEntity("Player");
        if (pInGroup != null) return pInGroup;

        // 2. 全てのグループを検索
        foreach (var g in EntityComponentSystem.GetAllGroups())
        {
            if (g.groupName == owner.Group.groupName) continue; // すでに探したのでパス
            var p = g.FindEntity("Player");
            if (p != null) return p;
        }
        return null;
    }
}

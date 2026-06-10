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
    /// プレイヤーの現在座標を保存するBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    /// <summary>
    /// 定期的に実行されるプレイヤー検索と位置更新処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint idKeyHash = BehaviorTreeLoader.HashString(targetIdKey);
        uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);

        // プレイヤーを検索
        Entity player = FindPlayer(owner);
        if (player != null)
        {
            blackboard.SetInt(idKeyHash, player.Id);
            
            // ターゲット座標を更新
            Vector3 pos = player.transform.position;
            // プレイヤーが空中にいても、岩は地面（Y=0）を目指すように固定
            pos.y = 0.0f; 
            
            blackboard.SetVector3(posKeyHash, pos);
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

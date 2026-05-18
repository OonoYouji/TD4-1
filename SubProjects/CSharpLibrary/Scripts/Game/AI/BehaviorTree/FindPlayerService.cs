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
        // プレイヤーを検索
        Entity player = FindPlayer(owner);
        if (player != null)
        {
            // 見つかった場合、その一意なID（C++側のEntityId）を保存
            blackboard.SetInt(BehaviorTreeLoader.HashString(targetIdKey), player.Id);
        }
    }

    /// <summary>
    /// 複数の主要なECSグループから「Player」という名前のエンティティを探し出す内部メソッド。
    /// </summary>
    private Entity FindPlayer(Entity owner)
    {
        // プレイヤーが存在する可能性のある代表的なシーン（ECSグループ）のリスト
        string[] commonGroups = { "GameScene", "Game", "Debug", "PlayerDevelopScene", "Workspace_PlayerBullet" };
        
        foreach (var name in commonGroups)
        {
            var g = EntityComponentSystem.GetECSGroup(name);
            if (g != null)
            {
                var p = g.FindEntity("Player");
                if (p != null) return p;
            }
        }
        return null;
    }
}

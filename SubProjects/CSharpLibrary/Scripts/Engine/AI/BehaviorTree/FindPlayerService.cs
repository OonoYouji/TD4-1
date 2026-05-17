using System;

/// <summary>
/// プレイヤーを定期的に検索し、BlackboardにそのIDを書き込むサービス
/// </summary>
public class FindPlayerService : BehaviorService
{
    [BlackboardKey]
    public string targetIdKey = "TargetId";

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        // プレイヤーを検索
        Entity player = FindPlayer(owner);
        if (player != null)
        {
            blackboard.SetInt(BehaviorTreeLoader.HashString(targetIdKey), player.Id);
        }
    }

    private Entity FindPlayer(Entity owner)
    {
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

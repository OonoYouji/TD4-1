using System;

/// <summary>
/// 名前でエンティティを検索し、BlackboardにEntityオブジェクトとして保存するサービス。
/// 文字列（名前）をオブジェクト（Entity）に変換し、他のノードが利用しやすくする。
/// </summary>
public class ResolveTargetService : BehaviorService
{
    /// <summary>
    /// 検索する対象の名前が格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string targetNameKey = "Target";

    /// <summary>
    /// 見つかったEntityを保存するBlackboardキー（通常はtargetNameKeyと同じで上書き可）。
    /// </summary>
    [BlackboardKey]
    public string resultEntityKey = "Target";

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint nameKey = BehaviorTreeLoader.HashString(targetNameKey);
        if (!blackboard.HasKey(nameKey)) {
            Debug.Log($"[ResolveTarget] Key '{targetNameKey}' not found in blackboard.");
            return;
        }

        // 文字列として名前を取得
        string name = blackboard.GetString(nameKey);
        if (string.IsNullOrEmpty(name)) {
            Debug.Log($"[ResolveTarget] Name is empty for key '{targetNameKey}'.");
            return;
        }

        // エンティティを検索
        Entity found = FindEntityByName(name, owner);
        if (found != null)
        {
            Debug.Log($"[ResolveTarget] Successfully resolved '{name}' to Entity ID:{found.Id}");
            // オブジェクトとして保存
            blackboard.SetObject(BehaviorTreeLoader.HashString(resultEntityKey), found);
        }
        else {
            Debug.Log($"[ResolveTarget] Could not find entity with name '{name}'.");
        }
    }

    private Entity FindEntityByName(string name, Entity owner)
    {
        // まず同じグループ内を検索
        Entity target = owner.Group.FindEntity(name);
        if (target != null) return target;

        // なければ他の主要なグループを検索
        string[] groups = { "Game", "GameScene", "Debug", "PlayerDevelopScene" };
        foreach (var gName in groups)
        {
            var g = EntityComponentSystem.GetECSGroup(gName);
            if (g != null)
            {
                target = g.FindEntity(name);
                if (target != null) return target;
            }
        }

        return null;
    }
}

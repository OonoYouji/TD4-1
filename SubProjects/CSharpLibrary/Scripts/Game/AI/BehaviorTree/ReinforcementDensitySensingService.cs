using System;
using System.Collections.Generic;

/// <summary>
/// フィールド上の援軍（Reinforcement）をスキャンし、最も密集している地点を特定して
/// Blackboard にターゲット座標を書き込むサービス。
/// 「援軍の仕様」が未確定な段階でも動作するよう、名前（"Reinforcement"）
/// またはスクリプトの有無でターゲットを判別する抽象的な実装を行う。
/// </summary>
public class ReinforcementDensitySensingService : BehaviorService
{
    /// <summary>
    /// 検索対象とするエンティティの名前。
    /// </summary>
    public string targetNameFilter = "Reinforcement";

    /// <summary>
    /// 密度を計算するための近傍判定半径。
    /// この半径内にユニットが多いほど「密集地」とみなされる。
    /// </summary>
    public float searchRadius = 5.0f;

    /// <summary>
    /// 特定した密集地の座標を書き込むBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    /// <summary>
    /// 定期的にフィールドを走査し、ターゲット座標を更新する。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        var entities = owner.Group.GetEntities();
        
        // 最も密集している地点を特定するロジック
        Vector3 bestPos = Vector3.zero;
        int maxNeighbors = -1;
        bool reinforcementFound = false;

        // 1. まず援軍（Reinforcement）のみを対象に密集地を探す
        foreach (var target in entities)
        {
            if (target == null || target.Id == owner.Id) continue;
            
            // 援軍判定（名前またはスクリプト）
            bool isReinforcement = target.name.ToLower().Contains("reinforcement") || target.GetScript<Reinforcement>() != null;
            if (!isReinforcement) continue;

            reinforcementFound = true;
            Vector3 pos = target.transform.position;
            int neighbors = 0;

            // 周囲の「援軍」の数を数える
            foreach (var other in entities)
            {
                if (other == null || target.Id == other.Id) continue;
                
                bool isOtherReinforcement = other.name.ToLower().Contains("reinforcement") || other.GetScript<Reinforcement>() != null;
                if (!isOtherReinforcement) continue;

                if (Vector3.Distance(pos, other.transform.position) <= searchRadius)
                {
                    neighbors++;
                }
            }

            if (neighbors > maxNeighbors)
            {
                maxNeighbors = neighbors;
                bestPos = pos;
            }
        }

        // 2. 援軍が見つかった場合はその密集地をターゲットにする
        if (reinforcementFound)
        {
            blackboard.SetVector3(BehaviorTreeLoader.HashString(targetPosKey), bestPos);
        }
        else
        {
            // 3. 援軍が一人もいない場合のみ、プレイヤーをターゲットにする
            var player = FindPlayer(owner);
            if (player != null)
            {
                blackboard.SetVector3(BehaviorTreeLoader.HashString(targetPosKey), player.transform.position);
            }
        }
    }

    private Entity FindPlayer(Entity owner)
    {
        // まず自グループ内を探す
        var p = owner.Group.FindEntity("Player");
        if (p != null) return p;

        // いなければ全グループを検索（フォールバック）
        foreach (var g in EntityComponentSystem.GetAllGroups())
        {
            p = g.FindEntity("Player");
            if (p != null) return p;
        }
        return null;
    }
}

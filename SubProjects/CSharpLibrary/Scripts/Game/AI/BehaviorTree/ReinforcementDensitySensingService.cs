using System;
using System.Collections.Generic;
using System.Linq;

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
        
        // ターゲット候補（援軍）を抽出
        // 1. 名前が一致するか
        // 2. もしくは Reinforcement スクリプトを持っているか
        var targets = entities.Where(e => 
            e.name.Contains(targetNameFilter) || 
            e.GetScript<Reinforcement>() != null
        ).ToList();

        if (targets.Count == 0)
        {
            // ターゲットが見つからない場合、何もしない（あるいは特定のデフォルト値を設定）
            return;
        }

        // 最も密集している地点を特定するロジック（簡易版：各ユニットの周囲半径内の個数を数える）
        Vector3 bestPos = Vector3.zero;
        int maxNeighbors = -1;

        foreach (var target in targets)
        {
            Vector3 pos = target.transform.position;
            int neighbors = 0;

            foreach (var other in targets)
            {
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

        // 結果をBlackboardに反映
        blackboard.SetVector3(BehaviorTreeLoader.HashString(targetPosKey), bestPos);
        
        // Debug.Log($"[Sensing] Found dense area with {maxNeighbors} units at {bestPos}");
    }
}

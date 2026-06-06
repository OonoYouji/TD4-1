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

        // 全エンティティを走査してターゲット候補を探す
        foreach (var target in entities)
        {
            if (target.Id == owner.Id) continue;
            
            // フィルタリング (大文字小文字を区別せず、スクリプトの有無もチェック)
            bool isTarget = target.name.ToLower().Contains("reinforcement") || target.name.ToLower().Contains("player") || target.GetScript<Reinforcement>() != null;
            if (!isTarget) continue;

            Vector3 pos = target.transform.position;
            int neighbors = 0;

            // 各候補について、周囲の他ユニット数を数える
            foreach (var other in entities)
            {
                if (target.Id == other.Id) continue;
                
                // neighbors判定時もフィルタリングが必要
                bool isOtherTarget = other.name.ToLower().Contains("reinforcement") || other.name.ToLower().Contains("player") || other.GetScript<Reinforcement>() != null;
                if (!isOtherTarget) continue;

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

        if (maxNeighbors != -1)
        {
            blackboard.SetVector3(BehaviorTreeLoader.HashString(targetPosKey), bestPos);
            Debug.Log($"<color=green>[TargetSensing]</color> Target updated to {Vector3.ToSimpleString(bestPos)} (Cluster size: {maxNeighbors + 1} entities)");
        }
        else
        {
            // ターゲットが見つからない場合の明示的なログ（頻度を抑えるためDebug.Log）
            // Debug.Log("<color=yellow>[TargetSensing]</color> No suitable targets found. Defaulting to last known or zero.");
        }
    }
}

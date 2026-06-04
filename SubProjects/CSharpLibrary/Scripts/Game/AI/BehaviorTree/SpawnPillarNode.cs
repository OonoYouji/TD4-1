using System;

/// <summary>
/// プレイヤーの足元（またはターゲット座標）に予兆を表示した後、
/// 上空から巨大な柱を落下させるアクションノード。
/// </summary>
public class SpawnPillarNode : BehaviorNode
{
    public string pillarPrefab = "BossPillar";
    public string telegraphPrefab = "TelegraphCircle";
    public float telegraphDuration = 1.5f;
    public float spawnHeight = 40.0f;
    public float indicatorSize = 7.0f;

    // v2: 連続落下の設定
    public int dropCount = 3;
    public float dropInterval = 0.5f;
    public float dropDistanceInterval = 5.0f;

    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("PillarAttackStart_" + NodeIdHash);
        uint lastDropTimeKey = BehaviorTreeLoader.HashString("LastDropTime_" + NodeIdHash);
        uint currentDropIdxKey = BehaviorTreeLoader.HashString("DropIndex_" + NodeIdHash);
        float currentTime = Time.time;

        // 1. 初期化
        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(lastDropTimeKey, 0.0f);
            blackboard.SetInt(currentDropIdxKey, 0);
            return NodeStatus.Running;
        }

        int currentIndex = blackboard.GetInt(currentDropIdxKey);
        
        // 全弾落としきったら終了
        if (currentIndex >= dropCount)
        {
            blackboard.Remove(startTimeKey);
            blackboard.Remove(lastDropTimeKey);
            blackboard.Remove(currentDropIdxKey);
            return NodeStatus.Success;
        }

        float lastDropTime = blackboard.GetFloat(lastDropTimeKey);
        if (currentTime - lastDropTime >= dropInterval)
        {
            SpawnSequntialPillar(blackboard, owner, currentIndex);
            blackboard.SetFloat(lastDropTimeKey, currentTime);
            blackboard.SetInt(currentDropIdxKey, currentIndex + 1);
        }

        return NodeStatus.Running;
    }

    private void SpawnSequntialPillar(Blackboard blackboard, Entity owner, int index)
    {
        // ターゲット座標（通常はプレイヤーの現在地）を取得
        uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
        Vector3 baseTargetPos = owner.transform.position + owner.transform.forward * 10.0f;
        if (blackboard.HasKey(posKeyHash))
        {
            baseTargetPos = blackboard.GetVector3(posKeyHash);
        }

        // ボスからターゲットへの方向を計算し、インデックスに応じて位置をずらす
        Vector3 dirToTarget = (baseTargetPos - owner.transform.position).Normalized();
        Vector3 offset = dirToTarget * (index * dropDistanceInterval);
        Vector3 finalTargetPos = baseTargetPos + offset;

        // --- 予兆の生成 ---
        Entity telegraph = owner.Group.CreateEntity(telegraphPrefab);
        if (telegraph != null)
        {
            telegraph.parent = null;
            telegraph.transform.position = new Vector3(finalTargetPos.x, 0.05f, finalTargetPos.z);
            telegraph.transform.scale = new Vector3(indicatorSize, 0.05f, indicatorSize);
            
            // v2: 予兆表示終了後に柱を落とす
            var delay = telegraph.AddScript<DelayedAction>();
            if (delay != null) {
                delay.delay = telegraphDuration;
                delay.action = () => {
                    Entity pillar = owner.Group.CreateEntity(pillarPrefab);
                    if (pillar != null) {
                        pillar.transform.position = new Vector3(finalTargetPos.x, spawnHeight, finalTargetPos.z);
                        var script = pillar.GetScript<FallingPillar>();
                        if (script != null) script.Launch(pillar.transform.position, finalTargetPos);
                    }
                };
            }
        }

        Debug.Log($"[SpawnPillar] Sequential drop {index+1}/{dropCount} at {finalTargetPos}");
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("PillarAttackStart_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("LastDropTime_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("DropIndex_" + NodeIdHash));
    }
}

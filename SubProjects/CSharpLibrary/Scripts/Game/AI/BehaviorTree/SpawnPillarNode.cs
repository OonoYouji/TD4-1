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
    public float spawnHeight = 30.0f;
    public float indicatorSize = 5.0f;

    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("PillarAttackStart_" + NodeIdHash);
        uint telegraphIdKey = BehaviorTreeLoader.HashString("PillarTelegraphID_" + NodeIdHash);
        float currentTime = Time.time;

        // 1. 開始処理: 予兆の生成
        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);

            // ターゲット座標（通常はプレイヤーの現在地）を取得
            uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            Vector3 targetPos = owner.transform.position + owner.transform.forward * 5.0f; // デフォルト
            if (blackboard.HasKey(posKeyHash))
            {
                targetPos = blackboard.GetVector3(posKeyHash);
            }

            // インジケーター生成
            Entity telegraph = owner.Group.CreateEntity(telegraphPrefab);
            if (telegraph != null)
            {
                telegraph.parent = null;
                telegraph.transform.position = new Vector3(targetPos.x, 0.05f, targetPos.z);
                telegraph.transform.scale = new Vector3(indicatorSize, 0.05f, indicatorSize);
                
                var timed = telegraph.GetScript<TimedDestruction>();
                if (timed == null) timed = telegraph.AddScript<TimedDestruction>();
                if (timed != null) timed.lifeTime = telegraphDuration + 0.5f;

                blackboard.SetInt(telegraphIdKey, telegraph.Id);
            }

            Debug.Log($"[SpawnPillar] Telegraph spawned at {targetPos}. Waiting {telegraphDuration}s...");
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        // 2. 落下実行
        if (elapsed >= telegraphDuration)
        {
            uint posKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
            Vector3 targetPos = blackboard.GetVector3(posKeyHash);

            // 柱本体の生成
            Entity pillar = owner.Group.CreateEntity(pillarPrefab);
            if (pillar != null)
            {
                pillar.parent = null;
                Vector3 startPos = new Vector3(targetPos.x, spawnHeight, targetPos.z);
                
                var script = pillar.GetScript<FallingPillar>();
                if (script == null) script = pillar.AddScript<FallingPillar>();
                
                if (script != null)
                {
                    script.Launch(startPos, targetPos);
                }
            }

            // 状態リセット
            blackboard.Remove(startTimeKey);
            blackboard.Remove(telegraphIdKey);

            Debug.Log($"<color=orange>[SpawnPillar]</color> Pillar launched towards {targetPos}!");
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("PillarAttackStart_" + NodeIdHash));
        uint telegraphIdKey = BehaviorTreeLoader.HashString("PillarTelegraphID_" + NodeIdHash);
        if (blackboard.HasKey(telegraphIdKey))
        {
            owner.Group.DestroyEntity(blackboard.GetInt(telegraphIdKey));
            blackboard.Remove(telegraphIdKey);
        }
    }
}

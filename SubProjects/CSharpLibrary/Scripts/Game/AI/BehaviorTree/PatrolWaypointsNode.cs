using System;
using System.Collections.Generic;

/// <summary>
/// 複数の座標ポイントを順番に巡回するアクションノード。
/// ボス仕様書に基づき、ポイントを番号順（1→2→3→4）に周回し、
/// 目的地に到達するたびに次のポイントへ移行する。
/// </summary>
public class PatrolWaypointsNode : BehaviorNode
{
    /// <summary>
    /// 現在の巡回ポイントのインデックスを保存するBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string currentIdxKey = "PatrolIndex";

    /// <summary>
    /// ポイント間の移動を完了とみなす距離。
    /// </summary>
    public float stopDistance = 1.0f;

    /// <summary>
    /// 巡回ポイントのEntity名リスト（セミコロン区切りの文字列）。
    /// </summary>
    public string waypointsString = "";

    /// <summary>
    /// 動的にWaypointを検索するための名前の接頭辞（例: "BossWaypoint_P1_"）。
    /// 指定されている場合、この名前で始まる全てのEntityを名前順で巡回リストに登録します。
    /// </summary>
    public string waypointPrefix = "";

    private List<string> waypointNames_ = new List<string>();

    /// <summary>
    /// 巡回リストを構築。
    /// </summary>
    private void EnsureWaypointNames(Entity owner)
    {
        if (waypointNames_.Count > 0) return;

        // 1. 接頭辞による動的検索を優先
        if (!string.IsNullOrEmpty(waypointPrefix))
        {
            var allEntities = owner.Group.GetEntities();
            List<string> foundNames = new List<string>();
            foreach (var e in allEntities)
            {
                if (e != null && e.name.StartsWith(waypointPrefix))
                {
                    foundNames.Add(e.name);
                }
            }

            if (foundNames.Count > 0)
            {
                // 名前順でソート（Waypoint_01, Waypoint_02... と並ぶことを期待）
                foundNames.Sort();
                waypointNames_ = foundNames;
                Debug.Log($"[PatrolWaypoints] Discovered {waypointNames_.Count} waypoints with prefix '{waypointPrefix}'");
                return;
            }
        }

        // 2. 文字列指定によるフォールバック
        if (!string.IsNullOrEmpty(waypointsString))
        {
            string[] names = waypointsString.Split(';');
            foreach (var n in names)
            {
                string trimmed = n.Trim();
                if (!string.IsNullOrEmpty(trimmed)) waypointNames_.Add(trimmed);
            }
            Debug.Log($"[PatrolWaypoints] Loaded {waypointNames_.Count} waypoints from string.");
        }
    }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        EnsureWaypointNames(owner);

        if (waypointNames_.Count == 0)
        {
            return NodeStatus.Failure;
        }

        uint idxKeyHash = BehaviorTreeLoader.HashString(currentIdxKey);
        int currentIdx = blackboard.GetInt(idxKeyHash, 0);

        // インデックスが範囲外の場合はリセット
        if (currentIdx < 0 || currentIdx >= waypointNames_.Count)
        {
            currentIdx = 0;
            blackboard.SetInt(idxKeyHash, 0);
        }

        // 対象のEntityを検索
        string targetName = waypointNames_[currentIdx];
        Entity waypointEntity = owner.Group.FindEntity(targetName);
        
        if (waypointEntity == null)
        {
            Debug.LogWarning($"<color=yellow>[PatrolWaypoints]</color> Waypoint '{targetName}' NOT FOUND. Skipping index.");
            blackboard.SetInt(idxKeyHash, (currentIdx + 1) % waypointNames_.Count);
            return NodeStatus.Failure;
        }

        Vector3 targetPos = waypointEntity.transform.position;
        Vector3 ownerPos = owner.transform.position;
        Vector3 diff = new Vector3(targetPos.x - ownerPos.x, 0, targetPos.z - ownerPos.z);
        float distance = diff.Length();

        if (Tree != null && Tree.TickCount % 30 == 0)
        {
            Debug.Log($"<color=cyan>[PatrolWaypoints]</color> Moving to {targetName} ({currentIdx+1}/{waypointNames_.Count}). Dist: {distance:F2}");
        }

        // --- 到着判定 ---
        if (distance <= stopDistance)
        {
            Debug.Log($"<color=green>[PatrolWaypoints]</color> Arrived at '{targetName}'. Success.");
            
            // 次回実行時のためにインデックスを次へ進めておく
            int nextIdx = (currentIdx + 1) % waypointNames_.Count;
            blackboard.SetInt(idxKeyHash, nextIdx);
            
            // 移動停止
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null) intent.desiredMoveDirection = Vector3.zero;

            return NodeStatus.Success;
        }

        // --- 移動継続 ---
        var aiIntent = owner.GetComponent<AgentIntentComponent>();
        if (aiIntent != null)
        {
            Vector3 dir = diff.Normalized();
            aiIntent.desiredMoveDirection = dir;
            if (dir.sqrMagnitude > 0.001f)
            {
                aiIntent.desiredRotation = Quaternion.LookRotation(dir, Vector3.up);
                aiIntent.useDesiredRotation = true;
            }
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            intent.desiredMoveDirection = Vector3.zero;
            intent.useDesiredRotation = false;
        }
    }
}

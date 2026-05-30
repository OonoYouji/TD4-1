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
    /// 例: "Waypoint_A; Waypoint_B; Waypoint_C; Waypoint_D"
    /// </summary>
    public string waypointNamesString = "";

    private List<string> waypointNames_ = new List<string>();

    /// <summary>
    /// 文字列からEntity名リストを構築。
    /// </summary>
    private void EnsureWaypointNames()
    {
        if (waypointNames_.Count > 0) return;

        string[] names = waypointNamesString.Split(';');
        foreach (var n in names)
        {
            string trimmed = n.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                waypointNames_.Add(trimmed);
            }
        }
    }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        EnsureWaypointNames();

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

        // 対象のEntityを検索して座標を取得
        string targetName = waypointNames_[currentIdx];
        Entity waypointEntity = owner.Group.FindEntity(targetName);
        
        if (waypointEntity == null)
        {
            Debug.LogWarning($"<color=yellow>[PatrolWaypoints]</color> Waypoint entity '{targetName}' NOT FOUND. Skipping to next.");
            blackboard.SetInt(idxKeyHash, (currentIdx + 1) % waypointNames_.Count);
            return NodeStatus.Running;
        }

        Vector3 targetPos = waypointEntity.transform.position;
        Vector3 ownerPos = owner.transform.position;
        
        // 高度(Y)を無視した距離計算（移動は平面想定）
        Vector3 diff = new Vector3(targetPos.x - ownerPos.x, 0, targetPos.z - ownerPos.z);
        float distance = diff.Length();

        // ログ出力（デバッグ用）
        if (TickCount % 30 == 0)
        {
            Debug.Log($"<color=cyan>[PatrolWaypoints]</color> {owner.name} heading to '{targetName}' {Vector3.ToSimpleString(targetPos)}. Dist: {distance:F2}");
        }

        // 1. 到着判定
        if (distance <= stopDistance)
        {
            Debug.Log($"<color=green>[PatrolWaypoints]</color> {owner.name} REACHED '{targetName}'. Moving to next index.");
            // 次のポイントへ（循環する）
            int nextIdx = (currentIdx + 1) % waypointNames_.Count;
            blackboard.SetInt(idxKeyHash, nextIdx);
            
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null) intent.desiredMoveDirection = Vector3.zero;

            return NodeStatus.Success;
        }

        // 2. 移動方向と回転の設定
        var aiIntent = owner.GetComponent<AgentIntentComponent>();
        if (aiIntent != null)
        {
            Vector3 dir = diff.Normalized();
            aiIntent.desiredMoveDirection = dir;
            
            // 進行方向を向く
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

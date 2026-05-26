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
    /// 巡回ポイントの座標リスト（セミコロン区切りの文字列）。
    /// 例: "10,0,10; -10,0,10; -10,0,-10; 10,0,-10"
    /// ※BTエディタが配列プロパティをサポートしていない場合のための簡易実装。
    /// </summary>
    public string waypointsString = "0,0,0";

    private List<Vector3> waypoints_ = new List<Vector3>();

    /// <summary>
    /// 文字列から座標リストを構築。
    /// </summary>
    private void EnsureWaypoints()
    {
        if (waypoints_.Count > 0) return;

        string[] points = waypointsString.Split(';');
        foreach (var p in points)
        {
            string[] coords = p.Split(',');
            if (coords.Length >= 3)
            {
                if (float.TryParse(coords[0], out float x) &&
                    float.TryParse(coords[1], out float y) &&
                    float.TryParse(coords[2], out float z))
                {
                    waypoints_.Add(new Vector3(x, y, z));
                }
            }
        }

        if (waypoints_.Count == 0)
        {
            waypoints_.Add(Vector3.zero);
        }
    }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        EnsureWaypoints();

        uint idxKeyHash = BehaviorTreeLoader.HashString(currentIdxKey);
        int currentIdx = blackboard.GetInt(idxKeyHash, 0);

        // インデックスが範囲外の場合はリセット
        if (currentIdx < 0 || currentIdx >= waypoints_.Count)
        {
            currentIdx = 0;
            blackboard.SetInt(idxKeyHash, 0);
        }

        Vector3 targetPos = waypoints_[currentIdx];
        Vector3 ownerPos = owner.transform.position;
        Vector3 diff = targetPos - ownerPos;
        float distance = diff.Length();

        // 1. 到着判定
        if (distance <= stopDistance)
        {
            // 次のポイントへ（循環する）
            int nextIdx = (currentIdx + 1) % waypoints_.Count;
            blackboard.SetInt(idxKeyHash, nextIdx);
            
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null) intent.desiredMoveDirection = Vector3.zero;

            // 1地点に到着したらSuccessを返し、Sequence等で繋げられるようにする
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

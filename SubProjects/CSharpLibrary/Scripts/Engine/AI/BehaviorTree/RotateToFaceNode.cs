using System;

/// <summary>
/// Blackboard上のターゲット（Entity）を向き続ける、あるいは瞬間的に向くアクションノード。
/// 攻撃開始前のエイムや、追従時の方向転換に使用する。
/// </summary>
public class RotateToFaceNode : BehaviorNode
{
    /// <summary>
    /// 向き先となるターゲットが格納されているBlackboardのキー。
    /// Entityオブジェクトが格納されていることを期待する。
    /// </summary>
    [BlackboardKey]
    public string targetKey = "Target";

    /// <summary>
    /// 回転速度（度/秒）。0を指定すると即座に向く。
    /// </summary>
    public float rotationSpeed = 360.0f;

    /// <summary>
    /// 目標方向との角度差がこれ以下になればSuccessを返す閾値。
    /// </summary>
    public float precisionAngle = 5.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetKey);
        Entity target = blackboard.GetEntity(key);

        if (target == null)
        {
            Debug.LogWarning($"RotateToFaceNode: Target '{targetKey}' is null.");
            return NodeStatus.Failure;
        }

        Vector3 ownerPos = owner.transform.position;
        Vector3 targetPos = target.transform.position;

        // 水平面（XZ）での向きを計算
        Vector3 diff = targetPos - ownerPos;
        diff.y = 0;

        if (diff.Length() < 0.01f) return NodeStatus.Success;

        Quaternion targetRot = Quaternion.LookRotation(diff.Normalized(), Vector3.up);
        
        if (rotationSpeed <= 0)
        {
            owner.transform.rotation = targetRot;
            return NodeStatus.Success;
        }

        // 現在の回転から徐々に向ける
        owner.transform.rotation = Quaternion.RotateTowards(
            owner.transform.rotation, 
            targetRot, 
            rotationSpeed * Time.deltaTime
        );

        // 角度差をチェック
        float angle = Quaternion.Angle(owner.transform.rotation, targetRot);
        if (angle <= precisionAngle)
        {
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }
}

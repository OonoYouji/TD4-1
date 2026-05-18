using System;

/// <summary>
/// 指定されたBlackboard上のターゲット（Vector3 または Entity）を向くタスク。
/// </summary>
public class RotateToFaceNode : BehaviorNode
{
    [BlackboardKey]
    public string targetKey = "";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetKey);
        if (!blackboard.HasKey(key)) return NodeStatus.Failure;

        Vector3 targetPos = Vector3.zero;
        object val = blackboard.GetValueAsObject(key);

        if (val is Vector3 v) targetPos = v;
        else if (val is Entity e) targetPos = e.transform.position;
        else return NodeStatus.Failure;

        Vector3 ownerPos = owner.transform.position;
        Vector3 diff = targetPos - ownerPos;
        diff.y = 0; // 高さは無視

        if (diff.Length() < 0.01f) return NodeStatus.Success;

        Vector3 targetDir = diff.Normalized();

        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            // 移動方向として設定することで、MovementSystemに回転を促す
            intent.desiredMoveDirection = targetDir;
        }

        // 現在の向きとターゲット方向の角度差をチェック（簡易的に内積を使用）
        // forward = rotate * (0, 0, 1)
        Vector3 forward = owner.transform.rotate * new Vector3(0, 0, 1);
        float dot = Vector3.Dot(forward.Normalized(), targetDir);

        if (dot > 0.99f) // 約8度以内
        {
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }
}

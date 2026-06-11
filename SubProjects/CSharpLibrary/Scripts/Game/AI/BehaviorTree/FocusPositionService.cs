using System;

/// <summary>
/// Blackboard上の座標（Vector3）に向かって、エンティティを回転させるサービス。
/// </summary>
public class FocusPositionService : BehaviorService
{
    /// <summary>
    /// 向き先となる座標が格納されているBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    /// <summary>
    /// 回転速度。
    /// </summary>
    public float rotationSpeed = 5.0f;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetPosKey);
        if (!blackboard.HasKey(key)) return;

        Vector3 targetPos = blackboard.GetVector3(key);
        Vector3 bossPos = owner.transform.position;
        Vector3 direction = (targetPos - bossPos);
        direction.y = 0; 

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.Normalized());
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
    }
}

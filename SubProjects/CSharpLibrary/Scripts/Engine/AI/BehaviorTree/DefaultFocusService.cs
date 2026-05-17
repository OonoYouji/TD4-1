using System;

/// <summary>
/// 指定したターゲット（Blackboard変数）の方向を自動的に向くサービス。
/// </summary>
public class DefaultFocusService : BehaviorService
{
    [BlackboardKey]
    public string targetNameKey = "TargetName";

    public float rotationSpeed = 5.0f;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetNameKey);
        string targetName = blackboard.GetString(key, "");

        if (string.IsNullOrEmpty(targetName)) return;

        Entity target = owner.Group.FindEntity(targetName);
        if (target != null)
        {
            Vector3 targetPos = target.transform.position;
            Vector3 direction = (targetPos - owner.transform.position);
            direction.y = 0; // 高さは無視

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }
}

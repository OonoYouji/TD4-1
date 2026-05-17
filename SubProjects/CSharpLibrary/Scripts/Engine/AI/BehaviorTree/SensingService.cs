using System;

/// <summary>
/// ターゲットとの距離を測定し、索敵範囲内かどうかをBlackboardのBool変数に書き込むサービス。
/// </summary>
public class SensingService : BehaviorService
{
    [BlackboardKey]
    public string targetNameKey = "TargetName";

    [BlackboardKey]
    public string resultBoolKey = "IsPlayerDetected";

    public float detectionRange = 15.0f;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint nameKeyHash = BehaviorTreeLoader.HashString(targetNameKey);
        string targetName = blackboard.GetString(nameKeyHash, "");

        if (string.IsNullOrEmpty(targetName)) return;

        Entity target = owner.Group.FindEntity(targetName);
        bool detected = false;

        if (target != null)
        {
            float dist = Vector3.Distance(owner.transform.position, target.transform.position);
            detected = (dist <= detectionRange);
        }

        blackboard.SetBool(BehaviorTreeLoader.HashString(resultBoolKey), detected);
    }
}

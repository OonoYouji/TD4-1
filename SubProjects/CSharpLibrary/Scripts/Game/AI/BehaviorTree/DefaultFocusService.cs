using System;

/// <summary>
/// 指定したターゲット（Blackboard変数に格納された名前）の方向を、
/// ノード実行中に自動的に向き続ける（LookAt）ためのサービス。
/// アクションノードの処理を邪魔することなく、バックグラウンドで滑らかな回転を提供する。
/// </summary>
public class DefaultFocusService : BehaviorService
{
    /// <summary>
    /// 対象となるエンティティの名前が格納されているBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string targetNameKey = "TargetName";

    /// <summary>
    /// ターゲットの方へ向く（回転する）速度。
    /// </summary>
    public float rotationSpeed = 5.0f;

    /// <summary>
    /// 指定された Interval が経過するたびに呼び出される定期更新処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint key = BehaviorTreeLoader.HashString(targetNameKey);
        string targetName = blackboard.GetString(key, "");

        // ターゲット名が設定されていない場合は何もしない
        if (string.IsNullOrEmpty(targetName)) return;

        // ターゲットエンティティをECSグループから検索
        Entity target = owner.Group.FindEntity(targetName);
        if (target != null)
        {
            // ターゲットへの方向ベクトルを計算（Y軸の高さは無視して水平方向のみ）
            Vector3 targetPos = target.transform.position;
            Vector3 direction = (targetPos - owner.transform.position);
            direction.y = 0; 

            // ターゲットとの距離が近すぎる場合（ベクトルの長さがほぼ0）は回転計算をスキップ
            if (direction.sqrMagnitude > 0.001f)
            {
                // ターゲットの方向を向くクォータニオンを生成し、Slerpで滑らかに回転させる
                Quaternion targetRot = Quaternion.LookRotation(direction);
                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
        }
    }
}

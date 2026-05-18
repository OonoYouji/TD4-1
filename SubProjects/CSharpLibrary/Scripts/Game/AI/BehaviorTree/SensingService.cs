using System;

/// <summary>
/// ターゲットとの距離を測定し、その結果（索敵範囲内にいるか）を Blackboard の Bool 変数に
/// 自動的に書き込み続けるサービスモジュール。
/// これと BlackboardDecorator を組み合わせることで、「近づいたら攻撃」「離れたら追跡をやめる」
/// といったイベント駆動（Observer Aborts）の挙動を簡単に構築できる。
/// </summary>
public class SensingService : BehaviorService
{
    /// <summary>
    /// 距離を測る対象となるエンティティの名前が格納されたBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string targetNameKey = "TargetName";

    /// <summary>
    /// 判定結果（範囲内にいればTrue、いなければFalse）を書き込むBlackboardのキー。
    /// </summary>
    [BlackboardKey]
    public string resultBoolKey = "IsPlayerDetected";

    /// <summary>
    /// 索敵範囲の半径（距離）。
    /// </summary>
    public float detectionRange = 15.0f;

    /// <summary>
    /// 指定された Interval 経過時に呼び出される索敵処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint nameKeyHash = BehaviorTreeLoader.HashString(targetNameKey);
        string targetName = blackboard.GetString(nameKeyHash, "");

        if (string.IsNullOrEmpty(targetName)) return;

        // 対象エンティティを検索
        Entity target = owner.Group.FindEntity(targetName);
        bool detected = false;

        // 対象が存在する場合のみ距離を計算
        if (target != null)
        {
            float dist = Vector3.Distance(owner.transform.position, target.transform.position);
            // 指定範囲内かどうかを判定
            detected = (dist <= detectionRange);
        }

        // 判定結果をBlackboardに書き込む。
        // 値が変わった場合はBlackboard内でイベントが発火し、ツリーの再評価（Abort処理など）がトリガーされる。
        blackboard.SetBool(BehaviorTreeLoader.HashString(resultBoolKey), detected);
    }
}

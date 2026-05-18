using System;

/// <summary>
/// ターゲット周辺の最適な位置を計算し、Blackboardに保存するサービス。
/// UEのEQS（Environment Query System）の非常に簡易的な実装。
/// </summary>
public class SimpleEQSService : BehaviorService
{
    /// <summary>
    /// 検索の基準となるターゲットエンティティのBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string targetKey = "Target";

    /// <summary>
    /// 計算結果（目標座標）を保存するBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string resultPosKey = "MoveToPos";

    /// <summary>
    /// ターゲットから維持したい理想的な距離。
    /// </summary>
    public float preferredDistance = 5.0f;

    /// <summary>
    /// ターゲットの正面からの角度オフセット（度数法）。
    /// 180なら背後に回り込む。
    /// </summary>
    public float angleOffset = 0.0f;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint tKey = BehaviorTreeLoader.HashString(targetKey);
        if (!blackboard.HasKey(tKey)) return;

        Entity target = blackboard.GetValueAsObject(tKey) as Entity;
        if (target == null) return;

        Vector3 targetPos = target.transform.position;
        
        // ターゲットの向き（Forward）を取得
        Vector3 targetForward = target.transform.rotate * new Vector3(0, 0, 1);
        
        // オフセット角度を計算
        float rad = (angleOffset) * (float)Math.PI / 180.0f;
        
        // ターゲット正面ベクトルを回転させて目標方向を決定
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);
        
        // Y軸周りの回転
        Vector3 offsetDir = new Vector3(
            targetForward.x * cos - targetForward.z * sin,
            0,
            targetForward.x * sin + targetForward.z * cos
        ).Normalized();

        // 目標地点を算出
        Vector3 goalPos = targetPos + offsetDir * preferredDistance;

        // 結果をBlackboardに保存
        blackboard.SetVector3(BehaviorTreeLoader.HashString(resultPosKey), goalPos);
    }
}

using System;

/// <summary>
/// 特定の形状の「予兆（Indicator）」を生成または制御するアクションノード。
/// ビームの予測線や、岩落としの落下地点円などを AI からトリガーするために使用。
/// </summary>
public class ShowIndicatorNode : BehaviorNode
{
    public enum Shape
    {
        Line,   // ビーム用予測線
        Circle  // 落下・範囲用
    }

    /// <summary>
    /// 表示する形状。
    /// </summary>
    public Shape shape = Shape.Line;

    /// <summary>
    /// 表示時間（秒）。0以下の場合は明示的に非表示にするまで継続。
    /// </summary>
    public float duration = 2.0f;

    /// <summary>
    /// 線の太さや円の半径。
    /// </summary>
    public float size = 1.0f;

    /// <summary>
    /// Blackboard上のターゲット座標キー（そこに向かって線を描く、またはそこに円を描く）。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
        if (!blackboard.HasKey(keyHash)) return NodeStatus.Failure;

        Vector3 targetPos = blackboard.GetVector3(keyHash);
        
        // C++側の演出システムへイベントを投げる
        // 例: "ShowIndicator_Line", "ShowIndicator_Circle"
        string eventName = "ShowIndicator_" + shape.ToString();
        
        // エンジン側で解析しやすいよう、単純な名前付きイベントとして発行
        // 実際には、専用のコンポーネントを介して詳細な座標を送るのが理想。
        // ここでは暫定的に FrameEvent を利用。
        FrameEvent.EnqueueNamedEvent(eventName, owner.Id);

        Debug.Log($"<color=cyan>[Indicator]</color> Showing {shape} indicator at {targetPos} for {duration}s");

        return NodeStatus.Success;
    }
}

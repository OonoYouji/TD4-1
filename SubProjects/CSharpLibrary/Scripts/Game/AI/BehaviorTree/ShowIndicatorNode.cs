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
        uint startTimeKey = BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash);
        float currentTime = Time.time;

        if (!blackboard.HasKey(startTimeKey))
        {
            // 初回実行: 演出イベントを発行
            uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
            if (!blackboard.HasKey(keyHash)) return NodeStatus.Failure;

            Vector3 targetPos = blackboard.GetVector3(keyHash);
            
            // --- プレハブを使用してメッシュベースの予測線を生成 ---
            // ※ECSGroup.CreateEntity を使用して TelegraphLine.prefab を生成
            Entity telegraph = owner.Group.CreateEntity("TelegraphLine");
            if (telegraph != null)
            {
                // ボスからターゲットへの方向に配置
                Vector3 bossPos = owner.transform.position;
                Vector3 diff = targetPos - bossPos;
                Vector3 direction = diff.Normalized();
                
                telegraph.transform.position = bossPos + (direction * 10.0f); // 20mの線の中心
                telegraph.transform.rotation = Quaternion.LookRotation(direction);
                
                // --- ボス自身もターゲットの方向を向くように設定 ---
                var intent = owner.GetComponent<AgentIntentComponent>();
                if (intent != null)
                {
                    intent.desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
                    intent.useDesiredRotation = true;
                }

                // Blackboardに保存（後で消すため）
                blackboard.SetInt(BehaviorTreeLoader.HashString("TelegraphEntityID"), telegraph.Id);
            }

            blackboard.SetFloat(startTimeKey, currentTime);

            Debug.Log($"<color=cyan>[Indicator]</color> {owner.name} spawned mesh telegraph for {duration}s");
            return NodeStatus.Running;
        }

        // 待機処理
        float startTime = blackboard.GetFloat(startTimeKey);
        if (currentTime - startTime >= duration)
        {
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash));
        
        // アボート時も予測線を消す
        uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID");
        if (blackboard.HasKey(telegraphKey))
        {
            int telegraphId = blackboard.GetInt(telegraphKey);
            owner.Group.DestroyEntity(telegraphId);
            blackboard.Remove(telegraphKey);
        }
    }
}

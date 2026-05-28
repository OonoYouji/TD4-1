using System;

public enum IndicatorShape
{
    Line,   // ビーム用予測線
    Circle  // 落下・範囲用
}

public enum IndicatorCenterType
{
    Target, // ターゲット地点
    Boss,   // ボスの位置
    Other   // 自由指定
}

/// <summary>
/// 特定の形状の「予兆（Indicator）」を生成または制御するアクションノード。
/// ビームの予測線や、岩落としの落下地点円などを AI からトリガーするために使用。
/// </summary>
public class ShowIndicatorNode : BehaviorNode
{
    /// <summary>
    /// 表示する形状。
    /// </summary>
    public IndicatorShape shape = IndicatorShape.Line;

    /// <summary>
    /// 中心位置の基準。
    /// </summary>
    public IndicatorCenterType centerType = IndicatorCenterType.Target;

    /// <summary>
    /// 表示時間（秒）。0以下の場合は明示的に非表示にするまで継続。
    /// </summary>
    public float duration = 2.0f;

    /// <summary>
    /// Blackboardから表示時間を取得する場合のキー名。
    /// </summary>
    [BlackboardKey]
    public string durationKey = "";

    /// <summary>
    /// 線の太さや円の半径。
    /// </summary>
    public float size = 1.0f;

    /// <summary>
    /// 線の長さ（Lineの場合）。
    /// </summary>
    public float length = 20.0f;

    /// <summary>
    /// 予兆の色。
    /// </summary>
    public Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 0.5f);

    /// <summary>
    /// Blackboardからサイズを取得する場合のキー名。
    /// </summary>
    [BlackboardKey]
    public string sizeKey = "";

    /// <summary>
    /// Blackboard上のターゲット座標キー（そこに向かって線を描く、またはそこに円を描く）。
    /// </summary>
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash);
        float currentTime = Time.time;

        float finalDuration = duration;
        if (!string.IsNullOrEmpty(durationKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(durationKey);
            finalDuration = blackboard.GetFloat(keyHash, duration);
        }

        float finalSize = size;
        if (!string.IsNullOrEmpty(sizeKey))
        {
            uint keyHash = BehaviorTreeLoader.HashString(sizeKey);
            finalSize = blackboard.GetFloat(keyHash, size);
        }

        if (!blackboard.HasKey(startTimeKey))
        {
            // 初回実行
            Vector3 originPos = owner.transform.position;
            if (centerType == IndicatorCenterType.Target)
            {
                uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
                if (blackboard.HasKey(keyHash)) originPos = blackboard.GetVector3(keyHash);
            }

            // --- プレハブを使用してメッシュベースの予測線を生成 ---
            string prefabName = (shape == IndicatorShape.Line) ? "TelegraphLine" : "TelegraphCircle";
            Entity telegraph = owner.Group.CreateEntity(prefabName);
            if (telegraph != null)
            {
                // 色の設定（自身または子供のMeshRendererを探す）
                var renderer = telegraph.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    for (uint i = 0; i < telegraph.GetChildCount(); i++)
                    {
                        var child = telegraph.GetChild(i);
                        if (child != null)
                        {
                            renderer = child.GetComponent<MeshRenderer>();
                            if (renderer != null) break;
                        }
                    }
                }
                if (renderer != null) renderer.color = color;

                uint targetKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
                Vector3 currentTarget = blackboard.GetVector3(targetKeyHash);
                
                // 超詳細ログ
                Debug.Log($"<color=cyan>[TRACE:Indicator]</color> {owner.name}(ID:{owner.Id}) READ {targetPosKey}({targetKeyHash}) = {Vector3.ToSimpleString(currentTarget)}");

                // サンリティチェック
                if (currentTarget.sqrMagnitude < 0.0001f)
                {
                    Debug.LogWarning($"<color=red>[TRACE:Indicator]</color> {owner.name} read ZERO-COORDINATE. Fallback to owner front.");
                    currentTarget = owner.transform.position + owner.transform.rotate * Vector3.forward * 10.0f;
                }

                UpdateTelegraphTransform(telegraph, owner.transform.position, originPos, currentTarget, finalSize);

                // Blackboardに保存（後で消すため。ノードごとにユニークなキーにする）
                blackboard.SetInt(BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash), telegraph.Id);
            }

            blackboard.SetFloat(startTimeKey, currentTime);

            Debug.Log($"<color=cyan>[Indicator]</color> {owner.name} spawned {shape} telegraph for {finalDuration}s");
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);

        // 毎フレーム、ターゲット位置とボスの位置関係から予兆を更新する
        uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
        if (blackboard.HasKey(telegraphKey))
        {
            int telegraphId = blackboard.GetInt(telegraphKey);
            if (telegraphId != 0)
            {
                Entity telegraph = owner.Group.GetEntity(telegraphId);
                if (telegraph != null)
                {
                    uint targetKeyHash = BehaviorTreeLoader.HashString(targetPosKey);
                    Vector3 currentTarget = blackboard.HasKey(targetKeyHash) ? blackboard.GetVector3(targetKeyHash) : owner.transform.position;

                    Vector3 originPos = owner.transform.position;
                    if (centerType == IndicatorCenterType.Target)
                    {
                        originPos = currentTarget;
                    }

                    UpdateTelegraphTransform(telegraph, owner.transform.position, originPos, currentTarget, finalSize);
                }
            }
        }

        if (currentTime - startTime >= finalDuration)
        {
            // 終了時にエンティティを削除
            if (blackboard.HasKey(telegraphKey))
            {
                int telegraphId = blackboard.GetInt(telegraphKey);
                if (telegraphId != 0)
                {
                    owner.Group.DestroyEntity(telegraphId);
                }
                blackboard.Remove(telegraphKey);
            }

            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

                private void UpdateTelegraphTransform(Entity telegraph, Vector3 bossPos, Vector3 originPos, Vector3 targetPos, float finalSize)
                {
                // 高度差を無視した水平方向のベクトルを計算
                Vector3 diff = new Vector3(targetPos.x - bossPos.x, 0.0f, targetPos.z - bossPos.z);
                Vector3 direction = (diff.sqrMagnitude > 0.001f) ? diff.Normalized() : Vector3.forward;

                if (shape == IndicatorShape.Line)
                {
                // --- 仕様に基づいた配置：ボスの足元付近からターゲット方向へ ---
                // ボスの足元中心から少し前方にずらした位置を起点にする
                Vector3 startPos = new Vector3(bossPos.x, 0.1f, bossPos.z) + direction * 3.0f;

                telegraph.transform.position = startPos;
                // 既存の LookRotation は逆回転を返す既知の問題があるため、Conjugate() で反転して正しい向きにする
                telegraph.transform.rotation = Quaternion.LookRotation(direction).Conjugate();

                // ターゲットまでの距離を計算（水平距離）
                float currentDist = (new Vector3(targetPos.x, 0.1f, targetPos.z) - startPos).Length();
                float finalLength = Math.Max(currentDist, length); 
                telegraph.transform.scale = new Vector3(finalSize, 1.0f, finalLength);

                // デバッグログ追加
                // Debug.Log($"[Indicator:Line] ID:{telegraph.Id} Pos:{Vector3.ToSimpleString(startPos)} Scale:{finalSize},1,{finalLength}");

                // --- デバッグ用描画 ---
                GizmoBatch.DrawRay(startPos, direction * finalLength, color);
                }
                else if (shape == IndicatorShape.Circle)
                {
                // 円は指定された originPos (Target地点 or ボス地点) の足元に配置
                telegraph.transform.position = new Vector3(originPos.x, 0.05f, originPos.z);
                // Z方向に引き延ばされるのを防ぐため、XZに同じサイズを適用し、Y(高さ)は極めて薄くする
                telegraph.transform.scale = new Vector3(finalSize, 0.01f, finalSize);

                // デバッグログ追加
                Debug.Log($"[Indicator:Circle] ID:{telegraph.Id} Name:{name} Pos:{Vector3.ToSimpleString(originPos)} Scale:{finalSize},0.01,{finalSize}");

                // --- デバッグ用描画 ---
                GizmoBatch.DrawLine(new Vector3(originPos.x - finalSize*0.5f, 0.1f, originPos.z), new Vector3(originPos.x + finalSize*0.5f, 0.1f, originPos.z), color);
                GizmoBatch.DrawLine(new Vector3(originPos.x, 0.1f, originPos.z - finalSize*0.5f), new Vector3(originPos.x, 0.1f, originPos.z + finalSize*0.5f), color);
                }
                }

                public override void OnAbort(Blackboard blackboard, Entity owner)
                {
                blackboard.Remove(BehaviorTreeLoader.HashString("IndicatorStart_" + NodeIdHash));

                // アボート時も予測線を消す
                uint telegraphKey = BehaviorTreeLoader.HashString("TelegraphEntityID_" + NodeIdHash);
                if (blackboard.HasKey(telegraphKey))
                {
                int telegraphId = blackboard.GetInt(telegraphKey);
                owner.Group.DestroyEntity(telegraphId);
                blackboard.Remove(telegraphKey);
                }
                }
}

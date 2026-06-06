using System;

/// <summary>
/// Blackboard上のターゲット座標またはエンティティが有効（非ゼロ、非ヌル）であるかを判定するデコレーター。
/// 有効なターゲットがいない場合に攻撃アクションが実行されるのを防ぎ、
/// 予測線が原点(0,0,0)に表示されるなどの不具合を回避するために使用する。
/// </summary>
public class CheckTargetValidDecorator : BehaviorDecorator
{
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    /// <summary>
    /// trueの場合、(0,0,0) を無効なターゲットとみなす。
    /// </summary>
    public bool checkZeroVector = true;

    public override bool CalculateCondition(Blackboard blackboard, Entity owner)
    {
        uint keyHash = BehaviorTreeLoader.HashString(targetPosKey);
        
        if (!blackboard.HasKey(keyHash))
        {
            return false;
        }

        if (checkZeroVector)
        {
            Vector3 pos = blackboard.GetVector3(keyHash);
            // ほぼゼロベクトルなら無効とみなす
            if (pos.sqrMagnitude < 0.001f)
            {
                return false;
            }
        }

        return true;
    }
}

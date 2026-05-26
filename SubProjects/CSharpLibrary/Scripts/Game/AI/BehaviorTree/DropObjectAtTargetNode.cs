using System;

/// <summary>
/// 指定されたオブジェクトを持ち上げ、ターゲット地点へ投げ飛ばす（あるいは落とす）ノード。
/// </summary>
public class DropObjectAtTargetNode : BehaviorNode
{
    [BlackboardKey]
    public string objectKey = "TargetRock";
    [BlackboardKey]
    public string targetPosKey = "TargetPosition";

    public float liftHeight = 5.0f;
    public float liftDuration = 1.0f;
    public float throwSpeed = 20.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint stateKey = BehaviorTreeLoader.HashString("RockAttackState_" + NodeIdHash);
        uint startTimeKey = BehaviorTreeLoader.HashString("RockStartTime_" + NodeIdHash);
        float currentTime = Time.time;

        Entity rock = blackboard.GetEntity(BehaviorTreeLoader.HashString(objectKey));
        if (rock == null) return NodeStatus.Failure;

        int state = blackboard.GetInt(stateKey, 0); // 0: Lifting, 1: Ready/Telegraph, 2: Thrown

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            Debug.Log($"<color=brown>[DropRock]</color> Starting attack with {rock.name}");
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (state == 0) // Lifting
        {
            // 岩をボスの頭上に移動させる
            Vector3 targetUpPos = owner.transform.position + Vector3.up * liftHeight;
            rock.transform.position = Vector3.Lerp(rock.transform.position, targetUpPos, elapsed / liftDuration);

            if (elapsed >= liftDuration)
            {
                blackboard.SetInt(stateKey, 1);
                blackboard.SetFloat(startTimeKey, currentTime); // 状態遷移用にリセット
                Debug.Log("[DropRock] Rock lifted. Ready to throw.");
            }
            return NodeStatus.Running;
        }
        else if (state == 1) // Ready/Telegraph (SubBT側で待機時間を制御するならここはSuccessでも良いが、簡略化のためここで投げる)
        {
            // ターゲット位置を再取得
            Vector3 targetPos = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
            
            // 投擲！
            Vector3 throwDir = (targetPos - rock.transform.position).Normalized();
            // 本来は物理コンポーネントに初速を与えるが、ここでは簡易的に「位置の更新」の意図をログに
            Debug.Log($"<color=brown>[DropRock]</color> THROWING {rock.name} towards {targetPos}");
            
            // 実際の移動はC++側や弾道計算スクリプトに任せるのが理想だが、
            // ここでは攻撃成功として終了
            FrameEvent.EnqueueNamedEvent("Effect_RockThrow", owner.Id);
            
            blackboard.Remove(stateKey);
            blackboard.Remove(startTimeKey);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("RockAttackState_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("RockStartTime_" + NodeIdHash));
    }
}

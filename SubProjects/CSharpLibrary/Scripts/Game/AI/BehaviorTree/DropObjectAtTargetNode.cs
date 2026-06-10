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
        
        // フォールバック：岩が指定されていない場合はプレハブから生成
        if (rock == null)
        {
            rock = owner.Group.CreateEntity("BossRock");
            if (rock != null)
            {
                rock.parent = null; // 独立させる
                rock.transform.position = owner.transform.position;
                blackboard.SetObject(BehaviorTreeLoader.HashString(objectKey), rock);
//                 Debug.Log("[DropRock] Fallback: Created BossRock prefab.");
            }
            else
            {
                return NodeStatus.Failure;
            }
        }

        int state = blackboard.GetInt(stateKey, 0); // 0: Lifting, 1: Ready/Telegraph, 2: Thrown

        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
//             Debug.Log($"<color=brown>[DropRock]</color> Starting attack with {rock.name}");
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        if (state == 0) // Lifting
        {
            // 岩をボスの頭上に移動させる
            Vector3 targetUpPos = owner.transform.position + Vector3.up * liftHeight;
            rock.transform.position = Vector3.Lerp(rock.transform.position, targetUpPos, Math.Min(elapsed / liftDuration, 1.0f));

            if (elapsed >= liftDuration)
            {
                blackboard.SetInt(stateKey, 1);
//                 Debug.Log("[DropRock] Rock lifted. Ready to throw.");
                
                // 次のフレームですぐに投げるように startTime を更新せず、そのまま state 1 に遷移させる
                // もしくはここでそのまま処理を続行させることも可能だが、一旦 Running を返す
            }
            return NodeStatus.Running;
        }
        else if (state == 1) // Ready/Telegraph
        {
            // ターゲット位置を取得
            Vector3 targetPos = blackboard.GetVector3(BehaviorTreeLoader.HashString(targetPosKey));
            
            // 投擲/落下開始！
            var fallingRock = rock.GetScript<FallingRock>();
            if (fallingRock != null)
            {
                // 落下開始（垂直落下の要件に合わせ、ターゲット位置を渡す）
                fallingRock.Launch(targetPos);
                
                // 岩をターゲットの真上に瞬間移動させる（演出の簡略化のため）
                rock.transform.position = new Vector3(targetPos.x, targetPos.y + 20.0f, targetPos.z);
            }
            else
            {
                // スクリプトがない場合は簡易的に飛ばす演出
//                 Debug.Log($"<color=brown>[DropRock]</color> THROWING {rock.name} towards {targetPos}");
            }
            
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

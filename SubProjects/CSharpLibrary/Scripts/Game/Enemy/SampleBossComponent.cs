using System;

/// <summary>
/// ボスの物理的な状態（HPなど）を管理し、Behavior Treeと連携するコンポーネント。
/// </summary>
public class SampleBossComponent : MonoScript
{
    public float maxHp = 1000.0f;
    public float currentHp = 1000.0f;

    // BTのフェーズ定数
    public const int Phase_Normal = 1;
    public const int Phase_Anger = 2;

    private bool _isAngerTriggered = false;

    public override void Initialize()
    {
//         Debug.Log($"[SampleBoss] Initialized on {entity.name}(ID:{entity.Id})");
        currentHp = maxHp;
    }

    public override void Update()
    {
        var agentIntent = entity.GetComponent<AgentIntentComponent>();
        if (agentIntent == null || agentIntent.behaviorTree == null) return;

        var bb = agentIntent.behaviorTree.Blackboard;

        // HPが半分以下になったら「狂暴フェーズ」へ移行するトリガー
        if (!_isAngerTriggered && (currentHp / maxHp) <= 0.5f)
        {
            _isAngerTriggered = true;
            bb.SetInt(BehaviorTreeLoader.HashString("CombatPhase"), Phase_Anger);
            bb.SetBool(BehaviorTreeLoader.HashString("IsAnger"), true);
//             Debug.Log($"<color=red>[Boss]</color> HP Half! Transition to ANGER PHASE.");
        }

        // デバッグ用：Kキーでダメージを受ける
        if (Input.TriggerKey(KeyCode.K) || Input.TriggerKey(KeyCode.H))
        {
//             Debug.Log("[SampleBoss] H/K key TRIGGERED!");
            float damageAmount = 50.0f;
            currentHp -= damageAmount;
            
            // 物理的なHPコンポーネントがある場合はそちらも同期
            HP hpComp = entity.GetScript<HP>();
            if (hpComp != null)
            {
                hpComp.TakeDamage((int)damageAmount);
//                 Debug.Log($"[SampleBoss] HP.TakeDamage called. New HP: {hpComp.currentHp}");
            }

//             Debug.Log($"[Boss] Damaged by Debug Key! HP: {currentHp}/{maxHp}");
        }
    }
}

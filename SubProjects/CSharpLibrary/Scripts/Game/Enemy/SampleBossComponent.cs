using System;

/// <summary>
/// ボスの物理的な状態（HPなど）を管理し、Behavior Treeと連携するコンポーネント。
/// </summary>
public class SampleBossComponent : Component
{
    public float maxHp = 100.0f;
    public float currentHp = 100.0f;

    // BTのフェーズ定数
    public const int Phase_Normal = 1;
    public const int Phase_Anger = 2;

    private bool _isAngerTriggered = false;

    public void Start()
    {
        currentHp = maxHp;
    }

    public void Update()
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
            Debug.Log($"<color=red>[Boss]</color> HP Half! Transition to ANGER PHASE.");
        }

        // デバッグ用：Kキーでダメージを受ける
        if (Input.TriggerKey(KeyCode.K))
        {
            currentHp -= 10.0f;
            Debug.Log($"[Boss] Damaged! HP: {currentHp}/{maxHp}");
        }
    }
}

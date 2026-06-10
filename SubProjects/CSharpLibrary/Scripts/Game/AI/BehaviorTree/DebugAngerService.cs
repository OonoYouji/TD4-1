using System;

/// <summary>
/// 一定時間経過後に強制的に狂暴化フェーズへ移行させるデバッグ用サービス。
/// タイマーをBlackboardで管理し、リロード時にも継続するように改善。
/// </summary>
public class DebugAngerService : BehaviorService
{
    public float delay = 5.0f;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        uint triggeredKey = BehaviorTreeLoader.HashString("DebugAnger_Triggered");
        uint timerKey = BehaviorTreeLoader.HashString("DebugAnger_Timer");

        if (blackboard.GetBool(triggeredKey)) return;

        float timer = blackboard.GetFloat(timerKey, 0f);
        timer += Interval; // サービスの実行間隔を加算
        blackboard.SetFloat(timerKey, timer);

        // 2秒おきに進捗をログ出力
        if ((int)(timer * 2) % 4 == 0)
        {
//             Debug.Log($"[DebugAnger] Countdown: {timer:F1} / {delay:F1}");
        }

        if (timer >= delay)
        {
            blackboard.SetBool(triggeredKey, true);
            blackboard.SetInt(BehaviorTreeLoader.HashString("CombatPhase"), 2);
//             Debug.Log("<color=red>[DebugAnger]</color> Force triggered ANGER PHASE (Phase 2)!");
        }
    }
}

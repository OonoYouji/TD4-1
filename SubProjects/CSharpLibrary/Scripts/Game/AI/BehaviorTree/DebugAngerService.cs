using System;

/// <summary>
/// 一定時間経過後に強制的に狂暴化フェーズへ移行させるデバッグ用サービス。
/// </summary>
public class DebugAngerService : BehaviorService
{
    public float delay = 5.0f;
    private float _timer = 0;
    private bool _triggered = false;

    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        if (_triggered) return;

        _timer += Interval; // サービスの実行間隔を加算
        if (_timer >= delay)
        {
            _triggered = true;
            blackboard.SetInt(BehaviorTreeLoader.HashString("CombatPhase"), 2);
            Debug.Log("<color=red>[DebugAnger]</color> Force triggered ANGER PHASE (Phase 2)!");
        }
    }
}

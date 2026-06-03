using System;

/// <summary>
/// プレイヤー（または指定された対象）に向かって移動を試みるアクションノード。
/// 拡張：アニメーションクリップの時間に基づいた自動待機に対応。
/// </summary>
public class MoveToPlayerNode : BehaviorNode
{
    public float stopDistance = 2.0f;

    public string startAnim = "";
    public string loopAnim = "";
    public string endAnim = "";

    /// <summary>
    /// trueの場合、インスペクタでの待機時間を無視し、
    /// アニメーションクリップの実際の長さを使用します。
    /// </summary>
    public bool useClipDuration = true;
    public float startWaitTime = 0.0f;
    public float endWaitTime = 0.0f;

    private enum MoveState
    {
        Start,
        Moving,
        End
    }

    public MoveToPlayerNode() { }

    public MoveToPlayerNode(float stopDistance = 2.0f)
    {
        this.stopDistance = stopDistance;
    }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint stateKey = BehaviorTreeLoader.HashString("MoveState_" + NodeIdHash);
        uint timerKey = BehaviorTreeLoader.HashString("MoveTimer_" + NodeIdHash);
        uint durationKey = BehaviorTreeLoader.HashString("WaitDur_" + NodeIdHash);

        MoveState state = (MoveState)blackboard.GetInt(stateKey, (int)MoveState.Start);
        float currentTime = Time.time;

        var animator = owner.GetComponent<Animator>();
        var aiIntent = owner.GetComponent<AgentIntentComponent>();

        // 1. 開始演出フェーズ
        if (state == MoveState.Start)
        {
            if (!blackboard.HasKey(timerKey))
            {
                float waitTime = startWaitTime;
                if (!string.IsNullOrEmpty(startAnim) && animator != null)
                {
                    animator.CrossFade(startAnim, 0.1f);
                    if (useClipDuration) waitTime = animator.GetAnimationDuration(startAnim);
                }
                blackboard.SetFloat(timerKey, currentTime);
                blackboard.SetFloat(durationKey, waitTime);
            }

            if (currentTime - blackboard.GetFloat(timerKey) >= blackboard.GetFloat(durationKey))
            {
                blackboard.Remove(timerKey);
                blackboard.Remove(durationKey);
                blackboard.SetInt(stateKey, (int)MoveState.Moving);
                state = MoveState.Moving;
                
                if (!string.IsNullOrEmpty(loopAnim) && animator != null)
                {
                    animator.CrossFade(loopAnim, 0.2f);
                }
            }
            else return NodeStatus.Running;
        }

        Entity player = FindPlayer(owner);
        if (player == null) return NodeStatus.Failure;

        Vector3 playerPos = player.transform.position;
        Vector3 ownerPos = owner.transform.position;
        Vector3 diff = playerPos - ownerPos;
        float distance = diff.Length();

        // 2. 移動フェーズ
        if (state == MoveState.Moving)
        {
            if (distance <= stopDistance)
            {
                blackboard.SetInt(stateKey, (int)MoveState.End);
                state = MoveState.End;
                
                if (!string.IsNullOrEmpty(endAnim) && animator != null)
                {
                    animator.CrossFade(endAnim, 0.1f);
                }
                
                if (aiIntent != null) aiIntent.desiredMoveDirection = Vector3.zero;
            }
            else
            {
                if (aiIntent != null)
                {
                    Vector3 dir = diff.Normalized();
                    aiIntent.desiredMoveDirection = dir;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        aiIntent.desiredRotation = Quaternion.LookRotation(dir, Vector3.up);
                        aiIntent.useDesiredRotation = true;
                    }
                }
                return NodeStatus.Running;
            }
        }

        // 3. 終了演出フェーズ
        if (state == MoveState.End)
        {
            if (!blackboard.HasKey(timerKey))
            {
                float waitTime = endWaitTime;
                if (!string.IsNullOrEmpty(endAnim) && animator != null && useClipDuration)
                {
                    waitTime = animator.GetAnimationDuration(endAnim);
                }
                blackboard.SetFloat(timerKey, currentTime);
                blackboard.SetFloat(durationKey, waitTime);
            }

            if (currentTime - blackboard.GetFloat(timerKey) >= blackboard.GetFloat(durationKey))
            {
                blackboard.Remove(stateKey);
                blackboard.Remove(timerKey);
                blackboard.Remove(durationKey);
                return NodeStatus.Success;
            }
            else return NodeStatus.Running;
        }

        return NodeStatus.Running;
    }

    private Entity FindPlayer(Entity owner)
    {
        var group = owner.Group;
        if (group != null) {
            var p = group.FindEntity("Player");
            if (p != null) return p;
        }
        string[] commonGroups = { "GameScene", "Game", "Debug", "PlayerDevelopScene", "Workspace_PlayerBullet" };
        foreach (var name in commonGroups) {
            var g = EntityComponentSystem.GetECSGroup(name);
            if (g != null) {
                var p = g.FindEntity("Player");
                if (p != null) return p;
            }
        }
        return null;
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("MoveState_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("MoveTimer_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("WaitDur_" + NodeIdHash));

        var intent = owner.GetComponent<AgentIntentComponent>();
        if (intent != null)
        {
            intent.desiredMoveDirection = Vector3.zero;
            intent.useDesiredRotation = false;
        }
    }
}

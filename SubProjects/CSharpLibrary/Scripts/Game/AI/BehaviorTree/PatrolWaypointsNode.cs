using System;
using System.Collections.Generic;

/// <summary>
/// 複数の座標ポイントを順番に巡回するアクションノード。
/// 拡張：アニメーションクリップの時間に基づいた自動待機に対応。
/// </summary>
public class PatrolWaypointsNode : BehaviorNode
{
    [BlackboardKey]
    public string currentIdxKey = "PatrolIndex";

    public float stopDistance = 1.0f;
    public string waypointsString = "";
    public string waypointPrefix = "";

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

    private List<string> waypointNames_ = new List<string>();

    private enum MoveState
    {
        Start,
        Moving,
        End
    }

    private void EnsureWaypointNames(Entity owner)
    {
        if (waypointNames_.Count > 0) return;

        if (!string.IsNullOrEmpty(waypointPrefix))
        {
            var allEntities = owner.Group.GetEntities();
            List<string> foundNames = new List<string>();
            foreach (var e in allEntities)
            {
                if (e != null && e.name.StartsWith(waypointPrefix)) foundNames.Add(e.name);
            }

            if (foundNames.Count > 0)
            {
                foundNames.Sort();
                waypointNames_ = foundNames;
                return;
            }
        }

        if (!string.IsNullOrEmpty(waypointsString))
        {
            string[] names = waypointsString.Split(';');
            foreach (var n in names)
            {
                string trimmed = n.Trim();
                if (!string.IsNullOrEmpty(trimmed)) waypointNames_.Add(trimmed);
            }
        }
    }

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        EnsureWaypointNames(owner);
        if (waypointNames_.Count == 0) return NodeStatus.Failure;

        uint idxKeyHash = BehaviorTreeLoader.HashString(currentIdxKey);
        int currentIdx = blackboard.GetInt(idxKeyHash, 0);

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

        if (currentIdx < 0 || currentIdx >= waypointNames_.Count)
        {
            currentIdx = 0;
            blackboard.SetInt(idxKeyHash, 0);
        }

        string targetName = waypointNames_[currentIdx];
        Entity waypointEntity = owner.Group.FindEntity(targetName);
        
        if (waypointEntity == null)
        {
            blackboard.SetInt(idxKeyHash, (currentIdx + 1) % waypointNames_.Count);
            return NodeStatus.Failure;
        }

        Vector3 targetPos = waypointEntity.transform.position;
        Vector3 ownerPos = owner.transform.position;
        Vector3 diff = new Vector3(targetPos.x - ownerPos.x, 0, targetPos.z - ownerPos.z);
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
                int nextIdx = (currentIdx + 1) % waypointNames_.Count;
                blackboard.SetInt(idxKeyHash, nextIdx);
                
                blackboard.Remove(stateKey);
                blackboard.Remove(timerKey);
                blackboard.Remove(durationKey);

                return NodeStatus.Success;
            }
            else return NodeStatus.Running;
        }

        return NodeStatus.Running;
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

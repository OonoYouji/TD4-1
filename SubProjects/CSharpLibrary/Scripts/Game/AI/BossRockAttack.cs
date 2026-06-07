using System;
using System.Collections.Generic;

/// <summary>
/// ボスの「岩持ち上げ攻撃」を制御するクラス。
/// フィールドの岩を拾い上げ、援軍に向けて落とす。
/// </summary>
public class BossRockAttack : MonoScript
{
    [SerializeField] public int attackCount = 3;
    [SerializeField] public float liftTime = 1.0f;
    [SerializeField] public float aimTime = 1.0f;
    [SerializeField] public float dropTime = 0.5f;
    [SerializeField] public string rockTag = "Rock";
    [SerializeField] public string targetTag = "Reinforcement";

    private enum State { Idle, Picking, Lifting, Aiming, Dropping }
    private State currentState = State.Idle;
    private float stateTimer = 0.0f;
    private int currentAttackRemaining = 0;

    private Entity targetRock;
    private Vector3 rockStartPos;
    private Vector3 dropTargetPos;
    private Animator animator;
    private string currentAnim = "";

    public override void Initialize()
    {
        animator = entity.GetComponent<Animator>();
    }

    private void PlayAnimation(string clipName)
    {
        if (animator == null || currentAnim == clipName) return;
        Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
        animator.CrossFade(clipName, 0.15f);
        currentAnim = clipName;
    }

    public override void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                if (Input.TriggerKey(KeyCode.R))
                {
                    StartAttack();
                }
                break;

            case State.Picking:
                PlayAnimation("rock_start");
                if (PickRandomRock())
                {
                    currentState = State.Lifting;
                    stateTimer = liftTime;
                    rockStartPos = targetRock.transform.position;
                    Debug.Log($"[BossRockAttack] Lifting rock: {targetRock.name}");
                }
                else
                {
                    currentState = State.Idle;
                    Debug.Log("[BossRockAttack] No rocks found to pick.");
                }
                break;

            case State.Lifting:
                PlayAnimation("rock");
                stateTimer -= Time.deltaTime;
                float liftRatio = 1.0f - (stateTimer / liftTime);
                if (targetRock != null)
                {
                    targetRock.transform.position = rockStartPos + Vector3.up * (liftRatio * 500.0f);
                }

                if (stateTimer <= 0)
                {
                    currentState = State.Aiming;
                    stateTimer = aimTime;
                    UpdateDropTarget();
                }
                break;

            case State.Aiming:
                PlayAnimation("rock");
                stateTimer -= Time.deltaTime;
                UpdateDropTarget(); // 常に更新し続ける（狙い続ける）
                
                // 狙いの演出（予測線など）
                GizmoBatch.DrawLine(targetRock.transform.position, dropTargetPos, new Vector4(1, 0, 0, 1));
                GizmoBatch.DrawWireCircle(dropTargetPos, 100.0f, new Vector4(1, 0, 0, 1));

                if (stateTimer <= 0)
                {
                    currentState = State.Dropping;
                    stateTimer = dropTime;
                    rockStartPos = targetRock.transform.position;
                }
                break;

            case State.Dropping:
                PlayAnimation("rock_end");
                stateTimer -= Time.deltaTime;
                float dropRatio = 1.0f - (stateTimer / dropTime);
                
                if (targetRock != null)
                {
                    targetRock.transform.position = Vector3.Lerp(rockStartPos, dropTargetPos, dropRatio);
                }

                if (stateTimer <= 0)
                {
                    OnRockImpact();
                    currentAttackRemaining--;
                    if (currentAttackRemaining > 0)
                    {
                        currentState = State.Picking;
                    }
                    else
                    {
                        currentState = State.Idle;
                    }
                }
                break;
        }
    }

    public void StartAttack()
    {
        if (currentState != State.Idle) return;
        currentAttackRemaining = attackCount;
        currentState = State.Picking;
        Debug.Log("[BossRockAttack] Sequence started.");
    }

    private bool PickRandomRock()
    {
        List<Entity> rocks = new List<Entity>();
        foreach (var entity in ecsGroup.GetEntities())
        {
            if (entity.name.Contains(rockTag))
            {
                rocks.Add(entity);
            }
        }

        if (rocks.Count > 0)
        {
            // 本来は RandomUtil などを使う
            targetRock = rocks[0]; 
            return true;
        }
        return false;
    }

    private void UpdateDropTarget()
    {
        // 最も援軍が多い地点を狙う（簡略化して最初の援軍の位置）
        foreach (var entity in ecsGroup.GetEntities())
        {
            if (entity.name.Contains(targetTag))
            {
                dropTargetPos = entity.transform.position;
                return;
            }
        }
        // 見つからなければ現在の位置
        dropTargetPos = transform.position + transform.forward * 500.0f;
    }

    private void OnRockImpact()
    {
        Debug.Log("[BossRockAttack] Impact!");
        // 衝撃波エフェクトやダメージ処理
    }
}

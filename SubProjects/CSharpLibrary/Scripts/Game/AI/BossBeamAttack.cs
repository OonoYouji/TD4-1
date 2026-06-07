using System;
using System.Collections.Generic;

/// <summary>
/// ボスのビーム攻撃を制御するクラス。
/// 援軍が集まっている場所を狙い、予兆線の後にビームを照射する。
/// </summary>
public class BossBeamAttack : MonoScript
{
    [SerializeField] public float waitTime = 2.0f;
    [SerializeField] public float firingDuration = 3.0f;
    [SerializeField] public float scanRadius = 500.0f; // 密集地を探す半径
    [SerializeField] public string targetTag = "Reinforcement";

    private enum State { Idle, Waiting, Firing }
    private State currentState = State.Idle;
    private float stateTimer = 0.0f;
    private Vector3 currentTargetPos;
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
                if (Input.TriggerKey(KeyCode.B))
                {
                    StartAttack();
                }
                break;

            case State.Waiting:
                PlayAnimation("beam_start");
                UpdateTarget();
                stateTimer -= Time.deltaTime;
                
                // 予測線の描画
                DrawPredictionLine();

                if (stateTimer <= 0)
                {
                    currentState = State.Firing;
                    stateTimer = firingDuration;
                    Debug.Log("[BossBeamAttack] Firing Beam!");
                }
                break;

            case State.Firing:
                PlayAnimation("beam");
                stateTimer -= Time.deltaTime;
                
                // ビームの描画（太い線で代用）
                DrawBeam();

                // 範囲ダメージ処理（簡易実装）
                ApplyBeamDamage();

                if (stateTimer <= 0)
                {
                    PlayAnimation("beam_end");
                    currentState = State.Idle;
                    Debug.Log("[BossBeamAttack] Attack Finished.");
                }
                break;
        }
    }

    public void StartAttack()
    {
        if (currentState != State.Idle) return;
        
        currentState = State.Waiting;
        stateTimer = waitTime;
        UpdateTarget();
        Debug.Log("[BossBeamAttack] Starting Attack Sequence (Waiting...)");
    }

    private void UpdateTarget()
    {
        // 援軍の密集地を探す
        Vector3 bestPos = Vector3.zero;
        int maxNeighbors = -1;

        foreach (var entity in ecsGroup.GetEntities())
        {
            if (!entity.name.Contains(targetTag)) continue;

            Vector3 pos = entity.transform.position;
            int neighbors = 0;
            foreach (var other in ecsGroup.GetEntities())
            {
                if (entity.Id == other.Id) continue;
                if (!other.name.Contains(targetTag)) continue;

                if (Vector3.Distance(pos, other.transform.position) <= scanRadius)
                {
                    neighbors++;
                }
            }

            if (neighbors > maxNeighbors)
            {
                maxNeighbors = neighbors;
                bestPos = pos;
            }
        }

        if (maxNeighbors != -1)
        {
            currentTargetPos = bestPos;
        }
    }

    private void DrawPredictionLine()
    {
        // 赤い細い線
        GizmoBatch.DrawLine(transform.position + Vector3.up * 100.0f, currentTargetPos, new Vector4(1, 0, 0, 0.5f));
    }

    private void DrawBeam()
    {
        // 太いビームを表現（複数の線や、特定のパーティクルなどで表現するのが望ましいが、簡易的に）
        for (int i = 0; i < 5; i++)
        {
            float offset = (i - 2) * 10.0f;
            GizmoBatch.DrawLine(transform.position + Vector3.up * 100.0f + new Vector3(offset, 0, 0), currentTargetPos, new Vector4(1, 1, 0, 1));
        }
    }

    private void ApplyBeamDamage()
    {
        // ビーム近傍のエンティティにダメージを与える
        // 実際には円柱やレイキャストでの判定が必要
    }
}

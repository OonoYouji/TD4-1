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

    // --- 調整用項目 ---
    [SerializeField] public float beamHeight = 8.0f;      // 発射の高さ
    [SerializeField] public float beamThickness = 2.0f;   // 当たり判定の太さ（半径）
    [SerializeField] public int beamWidthCount = 5;       // 見た目の線の本数
    [SerializeField] public float beamVisualWidth = 0.2f; // 見た目の線の広がり
    [SerializeField] public float damagePerSecond = 100.0f; // 1秒あたりのダメージ

    private enum State { Idle, Waiting, Firing }
    private State currentState = State.Idle;
    private float stateTimer = 0.0f;
    private Vector3 currentTargetPos;
    private Animator animator;
    private string currentAnim = "";

    public bool IsActive => currentState != State.Idle;

    public override void Initialize()
    {
        animator = entity.GetComponent<Animator>();
    }

    private void PlayAnimation(string clipName)
    {
        if (animator == null || currentAnim == clipName) return;
//         Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
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
                if (currentAnim != "beam_start") {
                    animator.CrossFadeWithDuration("beam_start", waitTime);
                    currentAnim = "beam_start";
                }
                UpdateTarget();
                
                // ターゲットの方向を向く
                RotateToTarget();

                stateTimer -= Time.deltaTime;
                
                // 予測線の描画
                DrawPredictionLine();

                if (stateTimer <= 0)
                {
                    currentState = State.Firing;
                    stateTimer = firingDuration;
//                     Debug.Log("[BossBeamAttack] Firing Beam!");
                }
                break;

            case State.Firing:
                if (currentAnim != "beam") {
                    animator.CrossFadeWithDuration("beam", firingDuration);
                    currentAnim = "beam";
                }

                // 発射中も追従して向く
                RotateToTarget();

                stateTimer -= Time.deltaTime;
                
                // ビームの描画（太い線で代用）
                DrawBeam();

                // 範囲ダメージ処理（簡易実装）
                ApplyBeamDamage();

                if (stateTimer <= 0)
                {
                    PlayAnimation("beam_end");
                    currentState = State.Idle;
//                     Debug.Log("[BossBeamAttack] Attack Finished.");
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
//         Debug.Log("[BossBeamAttack] Starting Attack Sequence (Waiting...)");
    }

    private void RotateToTarget()
    {
        Vector3 diff = currentTargetPos - transform.position;
        diff.y = 0;
        if (diff.sqrMagnitude > 0.001f)
        {
            Vector3 dir = diff.Normalized();
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            
            // 直接Transformを更新
            transform.rotate = targetRot;

            // AI Intentにも反映（念のため）
            var aiIntent = entity.GetComponent<AgentIntentComponent>();
            if (aiIntent != null)
            {
                aiIntent.desiredRotation = targetRot;
                aiIntent.useDesiredRotation = true;
            }
        }
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
        // 調整可能な高さを使用
        GizmoBatch.DrawLine(transform.position + Vector3.up * beamHeight, currentTargetPos, new Vector4(1, 0, 0, 0.5f));
    }

    private void DrawBeam()
    {
        Vector3 origin = transform.position + Vector3.up * beamHeight;
        Vector3 dir = (currentTargetPos - origin).Normalized();

        // 1. Gizmoによる当たり判定範囲の可視化 (デバッグ用)
        // 始点と終点に円を表示し、それをつなぐ
        GizmoBatch.DrawWireCircle(origin, beamThickness, new Vector4(1, 0, 0, 1), 16, 5.0f);
        GizmoBatch.DrawWireCircle(currentTargetPos, beamThickness, new Vector4(1, 0, 0, 1), 16, 5.0f);
        GizmoBatch.DrawLine(origin, currentTargetPos, new Vector4(1, 0, 0, 1));

        // 2. 外部プレハブ（赤い筒など）が生成されている場合、そのサイズを同期
        foreach (var entity in ecsGroup.GetEntities())
        {
            if (entity.name.Contains("BossBeam"))
            {
                // 高さを合わせる
                entity.transform.position = origin;
                
                // ターゲットの方向を向かせる
                entity.transform.rotate = Quaternion.LookRotation(dir, Vector3.up);
                
                // 太さ(X, Y)と長さ(Z)を同期
                float distance = (currentTargetPos - origin).Length();
                // プレハブの元のサイズが1x1x1と仮定してスケールを計算
                // Z方向は筒の長さ、X/Yは厚み
                entity.transform.scale = new Vector3(beamThickness * 2.0f, beamThickness * 2.0f, distance);
            }
        }

        // 3. 従来のラインによる描画
        for (int i = 0; i < beamWidthCount; i++)
        {
            float offset = (i - (beamWidthCount / 2)) * beamVisualWidth;
            GizmoBatch.DrawLine(origin + new Vector3(offset, 0, 0), currentTargetPos, new Vector4(1, 1, 0, 1));
        }
    }

    private void ApplyBeamDamage()
    {
        Vector3 origin = transform.position + Vector3.up * beamHeight;
        Vector3 beamDir = (currentTargetPos - origin).Normalized();
        float damage = damagePerSecond * Time.deltaTime;

        foreach (var target in ecsGroup.GetEntities())
        {
            // 自分自身は無視
            if (target.Id == entity.Id) continue;
            
            // ターゲットタグを持つもの（援軍など）またはプレイヤーを対象にする
            if (!target.name.Contains(targetTag) && !target.name.Contains("Player")) continue;

            // ビームの直線とターゲットの距離を計算
            Vector3 toTarget = target.transform.position - origin;
            float projection = Vector3.Dot(toTarget, beamDir);
            
            // ビームの前方にいる場合のみ判定
            if (projection > 0) {
                Vector3 closestPoint = origin + beamDir * projection;
                float distSq = (target.transform.position - closestPoint).sqrMagnitude;

                // 指定した太さ（半径）以内ならヒット
                if (distSq <= beamThickness * beamThickness)
                {
                    var hp = target.GetScript<HP>();
                    if (hp != null) {
                        hp.TakeDamage((int)damage);
                    }
                }
            }
        }
    }
}

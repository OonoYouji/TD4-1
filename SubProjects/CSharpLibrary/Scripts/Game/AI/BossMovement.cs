using System;
using System.Collections.Generic;

/// <summary>
/// ボスの移動を制御するクラス。
/// ウェイポイント巡回と、岩を押し退ける「ブルドーザー」挙動を実装する。
/// </summary>
public class BossMovement : MonoScript
{
    [SerializeField]
    public List<Vector3> waypoints = new List<Vector3>();
    [SerializeField]
    public float speed = 300.0f;
    [SerializeField]
    public float waitTimeAtWaypoint = 2.0f;
    [SerializeField]
    public float pushRadius = 200.0f; // 岩を押し出す範囲
    [SerializeField]
    public string rockTag = "Rock"; // 岩を識別するためのタグ（名前の一部など）

    private int currentWaypointIndex = 0;
    private float waitTimer = 0.0f;
    private bool isMoving = false;
    private Animator animator;
    private string currentAnim = "";
    private Vector3 currentMoveDir = Vector3.zero;
    private int logCounter = 0;

    public override void Initialize()
    {
        animator = entity.GetComponent<Animator>();
        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0];
            isMoving = false;
            waitTimer = waitTimeAtWaypoint;
        }
        PlayAnimation("idle");
    }

    private void PlayAnimation(string clipName)
    {
        if (animator == null || currentAnim == clipName) return;
        
        // 攻撃中は移動スクリプトからのアニメーション上書きを禁止する
        if (IsAnyAttackActive()) return;

//         Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
        animator.CrossFade(clipName, 0.2f);
        currentAnim = clipName;
    }

    private bool IsAnyAttackActive()
    {
        if (entity.GetScript<BossBeamAttack>()?.IsActive ?? false) return true;
        if (entity.GetScript<BossRockAttack>()?.IsActive ?? false) return true;
        if (entity.GetScript<BossBombBarrage>()?.IsActive ?? false) return true;
        if (entity.GetScript<BossClogAttack>()?.IsActive ?? false) return true;
        if (entity.GetScript<BossPillarAttack>()?.IsActive ?? false) return true;
        if (entity.GetScript<BossVortexAttack>()?.IsActive ?? false) return true;
        return false;
    }

    public override void Update()
    {
        // 向きのデバッグ表示 (太さ12)
        // 見えない問題を解決するため、極端なサイズ(長さ200)にし、移動していなくても前方ベクトルを表示する
        Vector3 gizmoBaseGreen = transform.position + Vector3.up * 40.0f;
        Vector3 gizmoBaseBlue = transform.position + Vector3.up * 60.0f;
        
        GizmoBatch.DrawRay(gizmoBaseGreen, transform.forward * 200.0f, new Vector4(0, 1, 0, 1), 12.0f); // 緑: モデルの前方 (高さ40)
        
        Vector3 blueDir = (currentMoveDir.sqrMagnitude > 0.001f) ? currentMoveDir : transform.forward;
        GizmoBatch.DrawRay(gizmoBaseBlue, blueDir * 200.0f, new Vector4(0, 1, 1, 1), 12.0f); // 水色: 移動方向 (高さ60)
            
        // 常にログを出力 (検証用)
        if (logCounter++ % 60 == 0) {
//             Debug.Log($"[GizmoDebug] Blue Line (Movement): Dir({blueDir.x:F1}, {blueDir.y:F1}, {blueDir.z:F1}) isMoving:{isMoving}");
        }

        // --- 自動移動の停止 ---
        // AIと競合するため、Updateでの自動的な巡回移動を一時的に無効化します。
        // 移動は PatrolWaypointsNode などのBTノードから制御されるべきです。
        /*
        if (waypoints.Count >= 2 && isMoving)
        {
            MoveToWaypoint();
            PlayAnimation("movement");
        }
        else
        {
            if (waypoints.Count >= 2)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isMoving = true;
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                }
            }
        }
        */

        // 岩を押し退ける処理は継続
        BulldozeRocks();
    }

    private void MoveToWaypoint()
    {
        // 競合防止: AIが動作している場合は、このスクリプトによる直接移動をスキップする
        if (entity.GetScript<BossAI>() != null) return;

        Vector3 target = waypoints[currentWaypointIndex];
        Vector3 pos = transform.position;
        Vector3 dir = target - pos;
        float distance = dir.Length();

        if (distance < speed * Time.deltaTime)
        {
            transform.position = target;
            isMoving = false;
            waitTimer = waitTimeAtWaypoint;
//             Debug.Log($"[BossMovement] Reached Waypoint {currentWaypointIndex}. Waiting...");
        }
        else
        {
            Vector3 moveDir = dir.Normalized();
            currentMoveDir = moveDir;
            transform.position += moveDir * speed * Time.deltaTime;
            
            // 移動方向を向く
            if (moveDir.sqrMagnitude > 0.001f)
            {
                // モデルの向きを移動方向に合わせる
                transform.rotate = Quaternion.LookRotation(moveDir, Vector3.up);
            }
        }
    }

    private void BulldozeRocks()
    {
        // フィールド上の岩を探して押し出す
        // パフォーマンスのため、本来は近傍探索が必要だが、
        // ここでは ECSGroup から全エンティティを走査する簡易実装とする
        foreach (Entity other in ecsGroup.GetEntities())
        {
            if (other == entity) continue;
            if (!other.name.Contains(rockTag)) continue;

            Vector3 diff = other.transform.position - transform.position;
            float distSq = diff.sqrMagnitude;
            float combinedRadius = pushRadius; // ボスの押し出し範囲

            if (distSq < combinedRadius * combinedRadius)
            {
                float dist = Mathf.Sqrt(distSq);
                if (dist < 0.001f) continue;

                // 押し出すベクトル
                Vector3 pushDir = diff / dist;
                float pushDistance = combinedRadius - dist;

                // 岩を移動させる
                other.transform.position += pushDir * pushDistance;
                
                // Debug.Log($"[BossMovement] Pushed rock: {other.name}");
            }
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
}

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
        
        Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
        animator.CrossFade(clipName, 0.2f);
        currentAnim = clipName;
    }

    public override void Update()
    {
        if (waypoints.Count < 2) return;

        if (isMoving)
        {
            MoveToWaypoint();
            PlayAnimation("movement");
        }
        else
        {
            waitTimer -= Time.deltaTime;
            PlayAnimation("idle");
            
            if (waitTimer <= 0)
            {
                isMoving = true;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                Debug.Log($"[BossMovement] Moving to Waypoint {currentWaypointIndex}");
            }
        }

        // 岩を押し退ける処理
        BulldozeRocks();
    }

    private void MoveToWaypoint()
    {
        Vector3 target = waypoints[currentWaypointIndex];
        Vector3 pos = transform.position;
        Vector3 dir = target - pos;
        float distance = dir.Length();

        if (distance < speed * Time.deltaTime)
        {
            transform.position = target;
            isMoving = false;
            waitTimer = waitTimeAtWaypoint;
            Debug.Log($"[BossMovement] Reached Waypoint {currentWaypointIndex}. Waiting...");
        }
        else
        {
            Vector3 moveDir = dir.Normalized();
            transform.position += moveDir * speed * Time.deltaTime;
            
            // 移動方向を向く
            if (moveDir.sqrMagnitude > 0.001f)
            {
                // 簡単な前方方向の設定（rotateを直接更新）
                // 本来は Quaternion.LookRotation などが必要だが、簡易的に
                // transform.rotate = Quaternion.LookAt(pos, target); // もしあれば
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

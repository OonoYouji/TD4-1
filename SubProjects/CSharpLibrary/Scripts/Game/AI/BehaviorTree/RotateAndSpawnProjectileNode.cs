using System;

/// <summary>
/// ボスを一定時間回転させながら、その間に指定された間隔でプロジェクタイル（弾）を生成するノード。
/// ボスの「回転投擲攻撃」の中核ロジック。
/// </summary>
public class RotateAndSpawnProjectileNode : BehaviorNode
{
    /// <summary>
    /// 生成する弾のプレハブ名。
    /// </summary>
    public string projectilePrefab = "EnemyBullet";

    /// <summary>
    /// 回転にかける合計時間（秒）。
    /// </summary>
    public float totalDuration = 3.0f;

    /// <summary>
    /// 1秒間に何回転するか（度/秒）。
    /// </summary>
    public float rotationSpeed = 360.0f;

    /// <summary>
    /// 弾を発射する間隔（秒）。
    /// </summary>
    public float fireInterval = 0.2f;

    /// <summary>
    /// 弾の初速。
    /// </summary>
    public float projectileSpeed = 15.0f;

    protected override NodeStatus Execute(Blackboard blackboard, Entity owner)
    {
        uint startTimeKey = BehaviorTreeLoader.HashString("RotateAttackStart_" + NodeIdHash);
        uint lastFireTimeKey = BehaviorTreeLoader.HashString("LastFireTime_" + NodeIdHash);
        float currentTime = Time.time;

        // 1. 開始処理
        if (!blackboard.HasKey(startTimeKey))
        {
            blackboard.SetFloat(startTimeKey, currentTime);
            blackboard.SetFloat(lastFireTimeKey, 0.0f); // 初回はすぐ撃つ
            
            // 回転モードへ移行（C++側のIntentに書き込む）
            var intent = owner.GetComponent<AgentIntentComponent>();
            if (intent != null)
            {
                intent.useDesiredRotation = false; // 自前でTransformを回すか、あるいは別の制御
            }

            Debug.Log($"<color=orange>[RotatingAttack]</color> {owner.name} started rotating and firing!");
            return NodeStatus.Running;
        }

        float startTime = blackboard.GetFloat(startTimeKey);
        float elapsed = currentTime - startTime;

        // 2. 終了判定
        if (elapsed >= totalDuration)
        {
            blackboard.Remove(startTimeKey);
            blackboard.Remove(lastFireTimeKey);
            Debug.Log($"<color=orange>[RotatingAttack]</color> {owner.name} finished rotating attack.");
            return NodeStatus.Success;
        }

        // 3. 回転処理（Transformを直接回す）
        float deltaRot = rotationSpeed * Time.deltaTime;
        owner.transform.rotate *= Quaternion.MakeFromAxis(Vector3.up, deltaRot * Mathf.Deg2Rad);

        // 4. 発射処理
        float lastFireTime = blackboard.GetFloat(lastFireTimeKey);
        if (elapsed - lastFireTime >= fireInterval)
        {
            SpawnProjectile(owner);
            blackboard.SetFloat(lastFireTimeKey, elapsed);
        }

        return NodeStatus.Running;
    }

    private void SpawnProjectile(Entity owner)
    {
        Entity projectile = owner.Group.CreateEntity(projectilePrefab);
        if (projectile != null)
        {
            // ボスの前方（回転しているので常に変わる）に向けて発射
            Vector3 fireDir = owner.transform.rotate * Vector3.forward;
            
            // 少し浮かせた位置から発射
            projectile.transform.position = owner.transform.position + Vector3.up * 1.0f + fireDir * 2.0f;
            
            // 弾のスクリプト等に速度を伝える（名前ベースの簡易実装）
            // 本来は Projectile コンポーネント等を取得して設定する
            Debug.Log($"[RotatingAttack] Fired projectile at {fireDir}");
            
            // 演出イベント
            FrameEvent.EnqueueNamedEvent("Effect_BossFire", owner.Id);
        }
    }

    public override void OnAbort(Blackboard blackboard, Entity owner)
    {
        blackboard.Remove(BehaviorTreeLoader.HashString("RotateAttackStart_" + NodeIdHash));
        blackboard.Remove(BehaviorTreeLoader.HashString("LastFireTime_" + NodeIdHash));
    }
}

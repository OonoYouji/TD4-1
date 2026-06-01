using System;

/// <summary>
/// プレイヤーの援軍専用のダメージ受取コンポーネント。
/// ボスの攻撃（EnemyBulletレイヤーなど）からの衝突を受け取り、HPを減らす。
/// </summary>
public class ReinforcementDamageHandler : MonoScript
{
    private HP hp;
    private Reinforcement reinforcement;

    [SerializeField]
    public float damageCooldownTime = 0.1f;
    private float cooldownTimer = 0f;

    public override void Initialize()
    {
        hp = entity.GetScript<HP>();
        reinforcement = entity.GetScript<Reinforcement>();
    }

    public override void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 外部（DamageTriggerなど）からダメージを与えるためのメソッド
    /// </summary>
    public void ApplyDamage(int damage, Vector3 attackerPosition)
    {
        if (cooldownTimer > 0 || hp == null) return;

        Debug.Log($"<color=orange>[ReinforcementDamageHandler]</color> Received {damage} damage from boss attack.");

        hp.TakeDamage(damage);
        cooldownTimer = damageCooldownTime;

        // 注意: HP 0 時の破棄は Reinforcement.Update() 側で行う
        if (hp.currentHp <= 0 && reinforcement != null)
        {
            reinforcement.TakeDamage(); // ここではフラグを立てるのみ
        }
    }

    public override void OnCollisionEnter(Entity other)
    {
        // 相手が無効、または既に破棄されている場合は無視
        if (other == null || other.Id == 0) return;
        
        HandleCollision(other);
    }

    public override void OnCollisionStay(Entity other)
    {
        // 相手が無効、または既に破棄されている場合は無視
        if (other == null || other.Id == 0) return;

        HandleCollision(other);
    }

    private void HandleCollision(Entity other)
    {
        if (cooldownTimer > 0) return;
        if (other.transform == null) return;

        // ボスの弾（EnemyBullet）との直接衝突をハンドリングする場合
        // ※基本はDamageTrigger側からApplyDamageが呼ばれる想定
        EnemyBullet bullet = other.GetScript<EnemyBullet>();
        if (bullet != null)
        {
            ApplyDamage(10, other.transform.position);
        }
    }
}

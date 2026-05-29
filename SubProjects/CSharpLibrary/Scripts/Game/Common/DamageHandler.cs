using System;

/// <summary>
/// 衝突判定を受け取り、HPを減少させるコンポーネント。
/// ボスを含む全てのダメージを受けるエンティティで使用可能。
/// </summary>
public class DamageHandler : MonoScript
{
    private HP hp;
    private Knockback knockback;

    [SerializeField]
    public float damageCooldownTime = 0.2f;
    private float cooldownTimer = 0f;

    public override void Initialize()
    {
        hp = entity.GetScript<HP>();
        if (hp == null)
        {
            hp = entity.AddScript<HP>();
        }
        knockback = entity.GetScript<Knockback>();
    }

    public override void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 外部（プレイヤー側）から直接ダメージを与えるための公開メソッド。
    /// </summary>
    /// <param name="damage">ダメージ量</param>
    public void ApplyDamage(int damage)
    {
        if (cooldownTimer > 0) return;

        // Debug.Log($"[DamageHandler] Applying {damage} damage to {entity.name}. Current HP: {hp.currentHp}");
        hp.TakeDamage(damage);
        cooldownTimer = damageCooldownTime;
    }

    /// <summary>
    /// 外部（プレイヤー側）から直接ダメージとノックバックを与えるための公開メソッド。
    /// </summary>
    /// <param name="damage">ダメージ量</param>
    /// <param name="attackerPosition">攻撃者の位置（ノックバック方向の計算用）</param>
    public void ApplyDamage(int damage, Vector3 attackerPosition)
    {
        if (cooldownTimer > 0) return;

        // Debug.Log($"[DamageHandler] Applying {damage} damage (with pos) to {entity.name}. Current HP: {hp.currentHp}");
        hp.TakeDamage(damage);
        cooldownTimer = damageCooldownTime;

        if (knockback != null)
        {
            Vector3 direction = transform.position - attackerPosition;
            direction.y = 0;
            if (direction.Length() > 0.001f)
            {
                knockback.ApplyKnockback(direction.Normalized());
            }
        }
    }

    public override void OnCollisionEnter(Entity other)
    {
        // Debug.Log($"[DamageHandler] {entity.name} OnCollisionEnter with {other.name}");
        HandleCollision(other);
    }

    public override void OnCollisionStay(Entity other)
    {
        HandleCollision(other);
    }

    private void HandleCollision(Entity other)
    {
        if (cooldownTimer > 0) return;

        // プレイヤーの弾との衝突判定
        PlayerBullet bullet = other.GetScript<PlayerBullet>();
        if (bullet != null)
        {
            // Debug.Log($"[DamageHandler] Hit by PlayerBullet: {other.name}");
            ApplyDamage(45, other.transform.position);
            return;
        }

        // 援軍との衝突判定
        Reinforcement reinforcement = other.GetScript<Reinforcement>();
        if (reinforcement != null)
        {
            if (reinforcement.isCollisionEnabled)
            {
                // Debug.Log($"[DamageHandler] Hit by Reinforcement: {other.name}");
                // ダメージ適用
                ApplyDamage((int)reinforcement.damage, other.transform.position);
                // 援軍側の攻撃後処理（退散など）を呼ぶ
                reinforcement.AttackBoss();
            }
        }
    }
}

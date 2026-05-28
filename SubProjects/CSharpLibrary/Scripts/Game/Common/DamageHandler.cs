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

        // デバッグ用: HキーでApplyDamageを呼び出してHPを減らすテスト
        if (Input.TriggerKey(KeyCode.H))
        {
            ApplyDamage(10);
            Debug.Log($"[Debug] H key pressed. Applied 10 damage to {entity.name}. Current HP: {hp.currentHp}");
        }
    }

    /// <summary>
    /// 外部（プレイヤー側）から直接ダメージを与えるための公開メソッド。
    /// </summary>
    /// <param name="damage">ダメージ量</param>
    public void ApplyDamage(int damage)
    {
        if (cooldownTimer > 0) return;

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

        hp.TakeDamage(damage);
        cooldownTimer = damageCooldownTime;

        if (knockback != null)
        {
            Vector3 direction = transform.worldPosition - attackerPosition;
            direction.y = 0;
            if (direction.Length() > 0.001f)
            {
                knockback.ApplyKnockback(direction.Normalized());
            }
        }
    }

    public override void OnCollisionEnter(Entity other)
    {
        if (cooldownTimer > 0) return;

        // プレイヤーの弾との衝突判定
        PlayerBullet bullet = other.GetScript<PlayerBullet>();
        if (bullet != null)
        {
            Debug.Log($"DamageHandler: {entity.name} hit by PlayerBullet.");
            
            // 弾にダメージ値が定義されていない場合は仮の値を設定
            int damage = 10; 
            
            hp.TakeDamage(damage);
            cooldownTimer = damageCooldownTime;

            // ノックバック処理
            if (knockback != null)
            {
                Vector3 direction = transform.worldPosition - other.transform.worldPosition;
                direction.y = 0;
                knockback.ApplyKnockback(direction.Normalized());
            }

            // 弾を消滅させる（必要に応じて）
            // other.Destroy();
        }
    }
}

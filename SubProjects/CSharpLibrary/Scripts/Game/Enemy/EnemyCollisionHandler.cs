class EnemyCollisionHandler : MonoScript
{
    [SerializeField]
    public int MAX_HITPOINTS = 1000;
    public int hitpoints;

    [SerializeField]
    public float DAMAGE_COOLDOWN_TIME = 0.5f;
    float damageCooldown = 0f;

    [SerializeField]
    public float KNOCKBACK_FORCE_STRENGTH = 100f;
    [SerializeField]
    public float KNOCKBACK_DECAY = 0.99f;
    Vector3 knockbackVelocity = Vector3.zero;
    float knockbackThreshold = 0.01f;

    EnemyUIHandler uiHandler;

    bool isDestory = false;

    public override void Initialize()
    {
        hitpoints = MAX_HITPOINTS;
        uiHandler = entity.GetScript<EnemyUIHandler>();
        if (uiHandler == null)
        {
            Debug.LogError("Failed to find EnemyUIHandler script");
        }
    }

    public override void Update()
    {
        damageCooldown -= Time.deltaTime;
        transform.position += knockbackVelocity * Time.deltaTime;
        knockbackVelocity -= knockbackVelocity * KNOCKBACK_DECAY * Time.deltaTime;

        if (isDestory)
        {
            entity.Destroy();
        }
    }

    public override void OnCollisionEnter(Entity collider)
    {
        if (collider == null) return;
        if (damageCooldown > 0f) return; // ダメージクールダウン中は無効
        // 衝突対象がプレイヤーの弾かどうかを判定
        //PlayerBullet bullet = collider.GetScript<PlayerBullet>();
        if (true)
        {
            Debug.Log("Enemy hit by bullet!");
            //int damage = bullet.damage;
            int damage = 100; // 仮
            TakeDamage(damage);
            if (uiHandler != null)
            {
                uiHandler.OnDamaged((float)hitpoints / (float)MAX_HITPOINTS);
            }

            // ダメージを連続で受けないよう無敵設定
            damageCooldown = DAMAGE_COOLDOWN_TIME;

            // ノックバック処理
            Vector3 direction = transform.position - collider.transform.position;
            if (direction.Length() > knockbackThreshold)
            {
                direction = direction.Normalized();
                knockbackVelocity = direction * KNOCKBACK_FORCE_STRENGTH;
            }
            else
            {
                // 後ろ
                Vector3 backword = Matrix4x4.Transform(Vector3.back, Matrix4x4.Rotate(transform.rotate));
                knockbackVelocity = backword * KNOCKBACK_FORCE_STRENGTH;
            }
        }
    }


    public void TakeDamage(int damage)
    {
        Debug.Log($"Enemy takes {damage} damage!");
        hitpoints -= damage;
        if (hitpoints <= 0)
        {
            Debug.Log("Enemy destroyed!");
            // HOTIFX: OnCollisiton内でDestoryを呼ぶとクラッシュするので、現在はフラグを立ててUpdate内でDestroyするようにしている
            // Update内でなら大丈夫とのこと
            isDestory = true;
        }
    }
}
class EnemyCollisionHandler : MonoScript
{
    [SerializeField]
    public int MAX_HITPOINTS = 1000;
    public int hitpoints;

    [SerializeField]
    public float DAMAGE_COOLDOWN_TIME = 0.5f;
    float damageCooldown = 0f;

    Knockback knockback;
    float knockbackSafetyThreshold = 0.1f;

    EnemyUIHandler uiHandler;

    bool isDestroy = false;

    public override void Initialize()
    {
        Debug.LogInfo("EnemyCollisionHandler Initializing");
        hitpoints = MAX_HITPOINTS;
        uiHandler = entity.GetScript<EnemyUIHandler>();
        if (uiHandler == null)
        {
            Debug.LogError("Failed to find EnemyUIHandler script");
        }
        knockback = entity.GetScript<Knockback>();
        if (knockback == null)
        {
            Debug.LogError("Failed to find Knockback script");
        }
        Debug.LogInfo("EnemyCollisionHandler initialized");
    }

    public override void Update()
    {
        damageCooldown -= Time.deltaTime;

        if (isDestroy)
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
            // メモ: colliderのtransformが正しく受け取れてない可能性
            Vector3 direction = transform.worldPosition - collider.transform.worldPosition;

            direction.y = 0.0f;
            if (direction.Length() > knockbackSafetyThreshold)
            {
                direction = direction.Normalized();
            }
            else
            { // 後ろ
                Vector3 backword = Matrix4x4.Transform(Vector3.back, Matrix4x4.Rotate(transform.rotate));
                direction = backword.Normalized();
            }
            if (knockback != null)
            {
                knockback.ApplyKnockback(direction);
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
            isDestroy = true;
        }
    }
}
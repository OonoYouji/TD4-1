class HP : MonoScript
{
    [SerializeField]
    public int MAX_HP = 100;
    [SerializeField]
    public int currentHp = 0;
    [SerializeField]
    public bool disableAutoDestruction = false;

    private bool _isDead = false;

    public override void Initialize()
    {
        currentHp = MAX_HP;
        _isDead = false;
    }

    public override void Update()
    {
        if (_isDead && !disableAutoDestruction)
        {
            entity.Destroy();
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            OnDead();
        }
    }

    private void OnDead()
    {
        // 死亡フラグを立ててUpdateで破棄するようにする（衝突中のクラッシュ防止）
        _isDead = true;
    }
    public void Heal(int healAmount)
    {
        currentHp += healAmount;
        if (currentHp > MAX_HP)
        {
            currentHp = MAX_HP;
        }
    }

    public float CurrentHpRatio()
    {
        return Mathf.Clamp01((float)currentHp / MAX_HP);
    }
}
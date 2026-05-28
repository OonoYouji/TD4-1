class HP : MonoScript
{
    [SerializeField]
    public int MAX_HP = 1000;
    [SerializeField]
    public int currentHp = 0;

    public override void Initialize()
    {
        currentHp = MAX_HP;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            OnDead();
        }
    }

    private void OnDead()
    {
        // 死亡時にエンティティを削除
        // これにより、ReinforcementManager などの OnDestroy コールバックが走る
        entity.Destroy();
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
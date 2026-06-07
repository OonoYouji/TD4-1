public partial class Reinforcement
{
    // =========================================================
    // 戦闘
    // =========================================================

    private bool isDestroyReserved = false;

    // Update から呼ぶ: 予約済みなら即削除して true を返す
    private bool CheckDestroyReserved()
    {
        if (!isDestroyReserved)
        {
            return false;
        }
        entity.Destroy();
        return true;
    }

    public override void OnDestroy()
    {
        if (!isRetreating)
        {
            Entity effect = ecsGroup.CreateEntity("ReinforcementDeathEffect");
            if (effect != null)
            {
                effect.transform.position = transform.position;
            }
            onDied?.Invoke(this);
        }
    }

    public void TakeDamage()
    {
        if (isDestroyReserved || (fallIn_ != null && fallIn_.IsActive))
        {
            return;
        }
        Debug.Log($"<color=red>[Reinforcement:Hit]</color> {entity.name} (ID:{entity.Id}) was DESTROYED by an attack.");
        isDestroyReserved = true;
    }

    public void AttackBoss()
    {
        onAttackBoss?.Invoke((int)damage, transform.position);
        Retreat();
    }
}

class Knockback : MonoScript
{
    [SerializeField]
    public float KNOCKBACK_FORCE_STRENGTH = 10f;
    [SerializeField]
    public float KNOCKBACK_DECAY = 5f;
    Vector3 knockbackVelocity = Vector3.zero;
    float knockbackStopThreshold = 0.01f;

    public override void Initialize()
    {
    }

    public void ApplyKnockback(Vector3 direction)
    {
        enable = true;
        knockbackVelocity += direction * KNOCKBACK_FORCE_STRENGTH;
    }

    public override void Update()
    {
        transform.position += knockbackVelocity * Time.deltaTime;
        // 減衰
        knockbackVelocity -= knockbackVelocity * KNOCKBACK_DECAY * Time.deltaTime;
        // 閾値以下になったら停止
        if (knockbackVelocity.Length() < knockbackStopThreshold)
        {
            knockbackVelocity = Vector3.zero;
            enable = false;
        }
    }
}


class Knockback : MonoScript
{
    [SerializeField]
    public float KNOCKBACK_FORCE_STRENGTH = 10f;
    [SerializeField]
    public float KNOCKBACK_DECAY = 0.9f;
    Vector3 knockbackVelocity = Vector3.zero;
    float knockbackStopThreshold = 0.01f;

    public void Initialize()
    {
        Debug.LogInfo("Knockback Initializing");
        Debug.LogInfo("Knockback Initialized");
    }

    public void ApplyKnockback(Vector3 direction)
    {
        knockbackVelocity += direction * KNOCKBACK_FORCE_STRENGTH;
        Debug.LogInfo($"Knockback applied: ({knockbackVelocity.x}, {knockbackVelocity.y}, {knockbackVelocity.z})");
    }

    public void Update()
    {
        transform.position += knockbackVelocity * Time.deltaTime;
        // 減衰
        knockbackVelocity -= knockbackVelocity * KNOCKBACK_DECAY * Time.deltaTime;
        // 閾値以下になったら停止
        if (knockbackVelocity.Length() < knockbackStopThreshold)
        {
            knockbackVelocity = Vector3.zero;
        }
    }
}
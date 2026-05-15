using System;
public class Reinforcement : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    [SerializeField] public float retreatSpeed = 20.0f;
    [SerializeField] public float lifeTime     = 10.0f;

    // =========================================================
    // 外部から設定
    // =========================================================

    public Vector3 startPosition = Vector3.zero;
    public Vector3 velocity      = Vector3.forward;

    // =========================================================
    // 内部状態
    // =========================================================

    private bool    positionApplied = false;
    private bool    isRetreating    = false;
    private Vector3 retreatVelocity = Vector3.zero;
    private float   timer           = 0.0f;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        positionApplied = false;
        isRetreating    = false;
        timer           = 0.0f;
    }

    public override void Update()
    {
        if (!positionApplied)
        {
            transform.position = startPosition;
            positionApplied = true;
        }

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            entity.Destroy();
            return;
        }

        if (isRetreating)
        {
            transform.position += retreatVelocity * Time.deltaTime;
        }
        else
        {
            transform.position += velocity * Time.deltaTime;
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    public void Retreat()
    {
        if (isRetreating) { return; }
        isRetreating    = true;
        retreatVelocity = -velocity.Normalized() * retreatSpeed;
    }
}

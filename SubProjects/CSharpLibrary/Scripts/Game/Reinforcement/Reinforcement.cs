using System;
public class Reinforcement : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 移動速度 (m/s)
    [SerializeField] public float moveSpeed = 8.0f;
    // 質量 (kg)
    [SerializeField] public float mass = 1.0f;
    // 消えるまでの秒数 (s)
    [SerializeField] public float lifeTime = 10.0f;
    // ダメージ量
    [SerializeField] public float damage = 10.0f;

    // 退散スピード
    [SerializeField] public float retreatSpeed = 20.0f;

    // =========================================================
    // 外部から設定
    // =========================================================

    // 出現位置、進行方向
    public Vector3 startPosition = Vector3.zero;
    public Vector3 direction     = Vector3.forward;

    // =========================================================
    // 内部状態
    // =========================================================

    // 位置適用済みフラグ
    private bool    positionApplied = false;
    // 退散中フラグ
    private bool    isRetreating    = false;
    // 退散速度
    private Vector3 retreatVelocity = Vector3.zero;
    // タイマー
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
        // 位置適用
        if (!positionApplied)
        {
            // スタートPositionに適応
            transform.position = startPosition;
            positionApplied = true;
        }

        // タイマー加算
        timer += Time.deltaTime;

        // 存在時間経過で削除
        if (timer >= lifeTime)
        {
            entity.Destroy();
            return;
        }

        // 退散
        if (isRetreating)
        {
            // 退散速度で移動
            transform.position += retreatVelocity * Time.deltaTime;
        }
        else
        {
            // 通常移動
            transform.position += direction.Normalized() * moveSpeed * Time.deltaTime;
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    public void Retreat()
    {
        // すでに退散中なら何もしない
        if (isRetreating) { 
            return;
        }

        // 退散開始
        isRetreating = true;
        retreatVelocity = -direction.Normalized() * retreatSpeed;
    }
}

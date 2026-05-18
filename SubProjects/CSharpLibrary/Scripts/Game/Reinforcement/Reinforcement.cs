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

    // 画面内判定用のカメラ視野角 (度)
    [SerializeField] public float viewAngle = 60.0f;

    // =========================================================
    // 外部から設定
    // =========================================================

    // 出現位置、進行方向
    public Vector3 startPosition = Vector3.zero;
    public Vector3 direction     = Vector3.forward;

    // 当たり判定有効フラグ
    public bool isCollisionEnabled = false;

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

    // カメラのEntity
    private Entity  cameraEntity    = null;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        positionApplied = false;
        isRetreating    = false;
        isCollisionEnabled = false;
        timer           = 0.0f;
        cameraEntity    = ecsGroup.FindEntity("Camera");
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

        // 画面内に完全に入ったかチェックして当たり判定を有効化
        if (!isCollisionEnabled && !isRetreating)
        {
            if (cameraEntity == null)
            {
                cameraEntity = ecsGroup.FindEntity("Camera");
            }
            if (cameraEntity != null)
            {
                Vector3 cameraPos = cameraEntity.transform.position;
                Vector3 toTarget = transform.position - cameraPos;
                
                // カメラの前方ベクトルを計算
                Vector3 cameraForward = cameraEntity.transform.rotate * Vector3.forward;

                float distance = toTarget.Length();
                if (distance > 0.1f)
                {
                    toTarget = toTarget.Normalized();
                    float dot = Vector3.Dot(cameraForward, toTarget);
                    
                    // 視野角の半分から閾値を計算
                    float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
                    float threshold = Mathf.Cos(halfAngleRad);

                    // 視界内に入ったら当たり判定有効化
                    if (dot >= threshold)
                    {
                        isCollisionEnabled = true;

                        // 視野角に入ったら色を変更してフィードバック
                        MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
                        if (meshRenderer != null)
                        {
                            // 例として少し赤っぽくする (1, 0.5, 0.5, 1)
                            meshRenderer.color = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
                        }
                    }
                }
            }
        }

        // 退散
        if (isRetreating)
        {
            // 退散中は当たり判定を切る
            isCollisionEnabled = false;
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

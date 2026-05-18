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

    // 元の色を保持
    private Vector4 originalColor   = Vector4.one;
    private bool    colorSaved      = false;

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
        colorSaved      = false;
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

        // 画面内判定して当たり判定と色を切り替え
        if (!isRetreating)
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
                bool inFrustum = false;
                if (distance > 0.1f)
                {
                    toTarget = toTarget.Normalized();
                    float dot = Vector3.Dot(cameraForward, toTarget);
                    
                    // 視野角の半分から閾値を計算
                    float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
                    float threshold = Mathf.Cos(halfAngleRad);

                    if (dot >= threshold)
                    {
                        inFrustum = true;
                    }
                }

                MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
                if (meshRenderer != null && !colorSaved)
                {
                    originalColor = meshRenderer.color;
                    colorSaved = true;
                }

                if (inFrustum)
                {
                    isCollisionEnabled = true;
                    if (meshRenderer != null)
                    {
                        meshRenderer.color = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
                    }
                }
                else
                {
                    isCollisionEnabled = false;
                    if (meshRenderer != null && colorSaved)
                    {
                        meshRenderer.color = originalColor;
                    }
                }
            }
        }

        // 退散
        if (isRetreating)
        {
            // 退散中は当たり判定を切り、色を元に戻す
            if (isCollisionEnabled)
            {
                isCollisionEnabled = false;
                MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
                if (meshRenderer != null && colorSaved)
                {
                    meshRenderer.color = originalColor;
                }
            }

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

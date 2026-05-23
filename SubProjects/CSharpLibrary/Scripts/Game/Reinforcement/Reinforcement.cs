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
    // プレイヤーのEntity
    private Entity  playerEntity    = null;

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
        playerEntity    = ecsGroup.FindEntity("Player");
        colorSaved      = false;
    }

    public override void Update()
    {
        // カメラ・プレイヤーEntityの再取得
        if (cameraEntity == null)
            cameraEntity = ecsGroup.FindEntity("Camera");
        if (playerEntity == null)
            playerEntity = ecsGroup.FindEntity("Player");

        // 位置適用
        if (!positionApplied)
        {
            // スタートPositionに適応
            transform.position = startPosition;
            positionApplied = true;
        }

        // タイマー加算
        timer += Time.deltaTime;

        // 退散するまでの秒数を経過したら退散 or 消滅
        if (timer >= lifeTime)
        {
            if (isCollisionEnabled)
            {
                // 画面内なら退散開始
                Retreat();
            }
            else
            {
                // 画面外にいたら即消滅
                entity.Destroy();
                return;
            }
        }

        // 画面内判定して当たり判定と色を切り替え
        if (!isRetreating)
        {
            if (cameraEntity == null)
            {
                cameraEntity = ecsGroup.FindEntity("Camera");
            }

            MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
            if (meshRenderer != null && !colorSaved)
            {
                originalColor = meshRenderer.color;
                colorSaved = true;
            }

            bool inFrustum = IsInScreenFrustum();
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

            // 退散中にカメラの視野角から外れたら消滅
            if (!IsInScreenFrustum())
            {
                entity.Destroy();
                return;
            }
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

        // プレイヤーから自分へのベクトル方向に退散
        Vector3 retreatDir = direction.Normalized(); // fallback
        if (playerEntity != null)
        {
            Vector3 toSelf = transform.position - playerEntity.transform.position;
            if (toSelf.Length() > 0.001f)
            {
                retreatDir = toSelf.Normalized();
            }
        }
        retreatVelocity = retreatDir * retreatSpeed;
    }

    // =========================================================
    // ヘルパー
    // =========================================================

    private bool IsInScreenFrustum()
    {
        if (cameraEntity == null) { return false; }

        Vector3 cameraPos = cameraEntity.transform.position;
        Vector3 toTarget = transform.position - cameraPos;
        float distance = toTarget.Length();
        if (distance <= 0.1f) { return true; }

        // カメラのrotationに依存せず、カメラ→プレイヤー方向をカメラforwardとして使う
        Vector3 cameraForward;
        if (playerEntity != null)
        {
            Vector3 camToPlayer = playerEntity.transform.position - cameraPos;
            float camToPlayerLen = camToPlayer.Length();
            cameraForward = camToPlayerLen > 0.001f ? camToPlayer.Normalized() : new Vector3(0, -1, 0);
        }
        else
        {
            cameraForward = new Vector3(0, -1, 0);
        }

        toTarget = toTarget.Normalized();
        float dot = Vector3.Dot(cameraForward, toTarget);
        float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
        float threshold = Mathf.Cos(halfAngleRad);
        return dot >= threshold;
    }
}

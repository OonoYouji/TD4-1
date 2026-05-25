using System;
public class Reinforcement : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 移動速度
    [SerializeField] public float moveSpeed = 8.0f;
    // 質量 
    [SerializeField] public float mass = 1.0f;
    // 消えるまでの秒数
    [SerializeField] public float lifeTime = 10.0f;
    // ダメージ量
    [SerializeField] public float damage = 10.0f;

    // 退散スピード
    [SerializeField] public float retreatSpeed = 20.0f;

    // 画面内判定用のカメラ視野角
    [SerializeField] public float viewAngle = 60.0f;

    // =========================================================
    // 外部から設定
    // =========================================================

    // 出現位置、進行方向
    public Vector3 startPosition = Vector3.zero;
    public Vector3 direction = Vector3.forward;

    // 当たり判定有効フラグ
    public bool isCollisionEnabled = false;

    // =========================================================
    // 内部状態
    // =========================================================

    // 位置適用済みフラグ
    private bool positionApplied = false;
    // 退散中フラグ
    private bool isRetreating = false;
    // 退散速度
    private Vector3 retreatVelocity = Vector3.zero;
    // タイマー
    private float timer = 0.0f;

    // カメラのEntity
    private Entity cameraEntity = null;
    // プレイヤーのEntity
    private Entity playerEntity = null;

    // 元の色を保持
    private Vector4 originalColor = Vector4.one;
    private bool colorSaved = false;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        positionApplied = false;
        isRetreating = false;
        isCollisionEnabled = false;
        timer = 0.0f;
        colorSaved = false;
        cameraEntity = ecsGroup.FindEntity("Camera");
        playerEntity = ecsGroup.FindEntity("Player");
    }

    public override void Update()
    {
        // 取得できてないEntityを再取得しておく
        ReacquireEntities();
        // 初期位置を適用
        ApplyInitialPosition();

        // タイマー更新と寿命チェック
        if (UpdateTimer())
        {
            return;
        }

        // 画面内判定と当たり判定の有効化
        UpdateFrustumVisibility();
        // 移動更新
        UpdateMovement();
    }

    // =========================================================
    // Update サブルーチン
    // =========================================================

    private void ReacquireEntities()
    {
        if (cameraEntity == null)
        {
            cameraEntity = ecsGroup.FindEntity("Camera");
        }
        if (playerEntity == null)
        {
            playerEntity = ecsGroup.FindEntity("Player");
        }
    }

    private void ApplyInitialPosition()
    {
        // すでに適用済みなら何もしない
        if (positionApplied)
        {
            return;
        }
        //  初期位置の適応
        transform.position = startPosition;
        positionApplied = true;
    }

    private bool UpdateTimer()
    {
        // タイマー更新
        timer += Time.deltaTime;

        // 寿命チェック
        if (timer < lifeTime)
        {
            return false;
        }

        // 寿命切れ
        if (isCollisionEnabled)
        {
            // 退散開始
            Retreat();
        }
        else
        {
            // 画面外なのですぐ消す
            entity.Destroy();
            return true;
        }
        return false;
    }

    private void UpdateFrustumVisibility()
    {
        // 退散中は当たり判定無効のまま
        if (isRetreating)
        {
            return;
        }

        //　色の保存
        MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
        TrySaveOriginalColor(meshRenderer);

        // 画面内判定
        bool inFrustum = IsInScreenFrustum();

        // 当たり判定の有効化と色の変更
        if (inFrustum)
        {
            // 画面内なので当たり判定有効化
            isCollisionEnabled = true;
            if (meshRenderer != null)
            {
                meshRenderer.color = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
            }
        }
        else
        {
            // 画面外なので当たり判定無効化
            isCollisionEnabled = false;
            if (meshRenderer != null && colorSaved)
            {
                meshRenderer.color = originalColor;
            }
        }
    }

    private void TrySaveOriginalColor(MeshRenderer meshRenderer)
    {
        // すでに保存済み、またはMeshRendererがない場合は何もしない
        if (colorSaved || meshRenderer == null)
        {
            return;
        }

        // 元の色を保存
        originalColor = meshRenderer.color;
        colorSaved = true;
    }

    private void UpdateMovement()
    {
        // 退散中は退散移動
        if (isRetreating)
        {
            UpdateRetreatMovement();
        }
        else
        {
            // 通常移動
            transform.position += direction.Normalized() * moveSpeed * Time.deltaTime;
        }
    }

    private void UpdateRetreatMovement()
    {
        // 退散中は当たり判定無効のまま移動
        DisableCollisionAndRestoreColor();
        // 退散移動
        transform.position += retreatVelocity * Time.deltaTime;

        // 画面外に出たら消す
        if (!IsInScreenFrustum())
        {
            entity.Destroy();
        }
    }

    private void DisableCollisionAndRestoreColor()
    {
        if (!isCollisionEnabled)
        {
            return;
        }

        // 当たり判定無効化
        isCollisionEnabled = false;

        // 元の色に戻す
        MeshRenderer meshRenderer = entity.GetComponent<MeshRenderer>();
        if (meshRenderer != null && colorSaved)
        {
            meshRenderer.color = originalColor;
        }
    }

    // =========================================================
    // 退散
    // =========================================================

    public void Retreat()
    {
        // すでに退散中なら何もしない
        if (isRetreating)
        {
            return;
        }

        // 退散開始
        isRetreating = true;
        retreatVelocity = ComputeRetreatVelocity();
    }

    private Vector3 ComputeRetreatVelocity()
    {

        Vector3 retreatDir = direction.Normalized();
        if (playerEntity != null)
        {
            // プレイヤーから自分へのベクトルを計算して退散方向に加算
            Vector3 toSelf = transform.position - playerEntity.transform.position;
            if (toSelf.Length() > 0.001f)
            {
                retreatDir = toSelf.Normalized();
            }
        }

        // 退散速度を計算して返す
        return retreatDir * retreatSpeed;
    }

    // =========================================================
    // ヘルパー
    // =========================================================

    private bool IsInScreenFrustum()
    {
        // カメラのEntityがない場合は画面内とみなす
        if (cameraEntity == null)
        {
            return false;
        }

        // カメラ位置とカメラからターゲットへのベクトルを計算
        Vector3 cameraPos = cameraEntity.transform.position;
        Vector3 toTarget = transform.position - cameraPos;

        // ターゲットがカメラに近い場合は画面内とみなす
        float distance = toTarget.Length();
        if (distance <= 0.1f)
        {
            return true;
        }

        // カメラの前方向とターゲットへのベクトルの角度を計算して画面内判定
        Vector3 cameraForward = GetCameraForward(cameraPos);

        // ターゲットへのベクトルを正規化
        toTarget = toTarget.Normalized();

        // カメラの前方向とターゲットへのベクトルのドット積を計算
        float dot = Vector3.Dot(cameraForward, toTarget);
        // カメラの視野角から画面内判定の閾値を計算
        float halfAngleRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;
        // 閾値を計算
        float threshold = Mathf.Cos(halfAngleRad);

        // ドット積が閾値以上なら画面内とみなす
        return dot >= threshold;
    }

    private Vector3 GetCameraForward(Vector3 cameraPos)
    {
        if (playerEntity != null)
        {
            // カメラからプレイヤーへのベクトルを計算して正規化
            Vector3 camToPlayer = playerEntity.transform.position - cameraPos;
            if (camToPlayer.Length() > 0.001f)
            {
                return camToPlayer.Normalized();
            }
        }
        return new Vector3(0, -1, 0);
    }
}

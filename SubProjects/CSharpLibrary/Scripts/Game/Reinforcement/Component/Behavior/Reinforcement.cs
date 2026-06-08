using System;

// 援軍の現在の状態
public enum ReinforcementState
{
    Normal,     // 通常
    Supported,  // 援護バフ適用中
}

public partial class Reinforcement : MonoScript
{
    // =========================================================
    // 通常状態パラメーター
    // =========================================================

    // 通常状態のスケール
    [SerializeField] public float normalScale = 1.0f;
    // 移動速度
    [SerializeField] public float moveSpeed = 8.0f;
    // 質量
    [SerializeField] public float mass = 1.0f;
    // 生存時間
    [SerializeField] public float lifeTime = 10.0f;
    // ボスへのダメージ量
    [SerializeField] public float damage = 10.0f;
    // 退散時の速度
    [SerializeField] public float retreatSpeed = 20.0f;
    // 画面内判定に使う視野角
    [SerializeField] public float viewAngle = 60.0f;

    // =========================================================
    // 援護バフ設定
    // =========================================================

    // 穴にはまった時に周囲を強化する範囲
    [SerializeField] public float supportBuffRadius;
    // 床の元位置より何単位下にはまるか
    [SerializeField] public float sinkDepth;
    // 援護バフを受けた時のスケール
    [SerializeField] public float supportedScale;
    // 援護バフを受けた時の攻撃力
    [SerializeField] public float supportedDamage;

    // =========================================================
    // 外部から設定
    // =========================================================

    // スポーン位置と進行方向はCallingReinforcementがセットする
    public Vector3 startPosition = Vector3.zero;
    public Vector3 direction = Vector3.forward;
    // FieldFallとの当たり判定フラグ
    public bool isCollisionEnabled = false;

    // =========================================================
    // コールバック
    // =========================================================

    // 退散でなく倒された時に呼ばれる
    public Action<Reinforcement> onDied = null;
    // ボスに攻撃が当たった時に呼ばれる
    public Action<int, Vector3> onAttackBoss = null;

    // =========================================================
    // 内部状態
    // =========================================================

    // 現在の状態（Normal / Supported）
    private ReinforcementState state_ = ReinforcementState.Normal;
    public ReinforcementState State => state_;

    // 初期位置の適用が済んでいるか
    private bool positionApplied = false;
    // 退散中かどうか
    private bool isRetreating = false;
    // 退散時の移動ベクトル
    private Vector3 retreatVelocity = Vector3.zero;
    // 生存タイマー
    private float timer = 0.0f;
    // スポーン直後の衝突を防ぐフレームカウンター
    private int spawnDelayFrames = 5;
    // バフ前の色を保持しておく
    private Vector4 originalColor = Vector4.one;
    private bool colorSaved = false;
    // バフを一度だけ発動するためのフラグ
    private bool supportBuffApplied_ = false;

    // 各種参照
    private Entity cameraEntity = null;
    private Entity playerEntity = null;
    private FieldManager fieldManager_ = null;

    public bool IsRetreating => isRetreating;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {

        // 各フラグの初期化
        positionApplied = false;
        isRetreating = false;
        isCollisionEnabled = false;
        isDestroyReserved = false;
        supportBuffApplied_ = false;
        colorSaved = false;

        // 状態の初期化
        state_ = ReinforcementState.Normal;

        // スケールの初期化
        transform.scale = new Vector3(normalScale, normalScale, normalScale);
        // タイマーの初期化
        timer = 0.0f;
        // スポーン直後の衝突を防ぐフレームカウンターの初期化
        spawnDelayFrames = 2;

        // エンティティの取得
        cameraEntity = ecsGroup.FindEntity("Camera");
        playerEntity = ecsGroup.FindEntity("Player");

        // FieldManagerの取得
        Entity fmEntity = ecsGroup.FindEntity("FieldManager");
        if (fmEntity != null)
        {
            fieldManager_ = fmEntity.GetScript<FieldManager>();
        }

        // ReinforcementFallInの取得
        fallIn_ = entity.GetScript<ReinforcementFallIn>();

        // HPの初期化
        HP hp = entity.GetScript<HP>();
        if (hp != null)
        {
            hp.MAX_HP = 10;
            hp.currentHp = 10;
        }
    }

    public override void Update()
    {

        if (entity.Id == 0)
        {
            return;
        }
        if (CheckDestroyReserved())
        {
            return;
        }

        // Entityの再取得
        ReacquireEntities();
        // 初期位置の適応
        ApplyInitialPosition();

        if (spawnDelayFrames > 0)
        {
            spawnDelayFrames--;
            isCollisionEnabled = false;
            return;
        }

        if (!isRetreating && !isCollisionEnabled)
        {
//             Debug.Log($"<color=green>[Reinforcement:Enable]</color> Collision enabled for {entity.name} (ID:{entity.Id}) after delay.");
            isCollisionEnabled = true;
        }

        // 打ち上げ中なら早期リターン
        if (CheckLaunchExit())
        {
            return;
        }

        // 床に挟まってたら早期りたーん
        if (CheckTrappedActive())
        {
            return;
        }

        // 寿命切れなら早期リターン
        if (UpdateTimer())
        {
            return;
        }

        // 乗ってるフィールド床が落ちてるかチェック
        CheckFieldFall();
        // カメラの視野内にいるか判定して色を変える
        UpdateFrustumVisibility();
        //  移動更新
        UpdateMovement();
    }

    // =========================================================
    // Update サブルーチン
    // =========================================================

    private void ReacquireEntities()
    {
        if (cameraEntity == null) { cameraEntity = ecsGroup.FindEntity("Camera"); }
        if (playerEntity == null) { playerEntity = ecsGroup.FindEntity("Player"); }
    }

    private void ApplyInitialPosition()
    {
        if (positionApplied || startPosition.Length() < 0.001f)
        {
            return;
        }
        transform.position = startPosition;
        positionApplied = true;
    }

    private bool UpdateTimer()
    {

        // タイム更新
        timer += Time.deltaTime;
        if (timer < lifeTime)
        {
            return false;
        }
        // 画面内なら退散
        if (isCollisionEnabled)
        {
            Retreat();
        }
        else
        {
            entity.Destroy();
            return true;
        }
        return false;
    }
}

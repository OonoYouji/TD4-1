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

    // 移動速度
    [SerializeField] public float moveSpeed    = 8.0f;
    // 質量
    [SerializeField] public float mass         = 1.0f;
    // 生存時間
    [SerializeField] public float lifeTime     = 10.0f;
    // ボスへのダメージ量
    [SerializeField] public float damage       = 10.0f;
    // 退散時の速度
    [SerializeField] public float retreatSpeed = 20.0f;
    // 画面内判定に使う視野角
    [SerializeField] public float viewAngle    = 60.0f;

    // =========================================================
    // 援護バフ設定（プレハブごとに調整）
    // =========================================================

    // 穴にはまった時に周囲を強化する範囲
    [SerializeField] public float supportBuffRadius = 5.0f;
    // 援護バフを受けた時のスケール（絶対値）
    [SerializeField] public float supportedScale    = 2.0f;
    // 援護バフを受けた時の攻撃力（絶対値）
    [SerializeField] public float supportedDamage   = 15.0f;

    // =========================================================
    // 外部から設定
    // =========================================================

    // スポーン位置と進行方向はCallingReinforcementがセットする
    public Vector3 startPosition   = Vector3.zero;
    public Vector3 direction       = Vector3.forward;
    // FieldFallとの当たり判定フラグ
    public bool isCollisionEnabled = false;

    // =========================================================
    // コールバック
    // =========================================================

    // 自然死した時（退散でなく倒された時）に呼ばれる
    public Action<Reinforcement> onDied       = null;
    // ボスに攻撃が当たった時に呼ばれる
    public Action<int, Vector3>  onAttackBoss = null;

    // =========================================================
    // 内部状態
    // =========================================================

    // 現在の状態（Normal / Supported）
    private ReinforcementState state_ = ReinforcementState.Normal;
    public  ReinforcementState State  => state_;

    // 初期位置の適用が済んでいるか
    private bool    positionApplied   = false;
    // 退散中かどうか
    private bool    isRetreating      = false;
    // 退散時の移動ベクトル
    private Vector3 retreatVelocity   = Vector3.zero;
    // 生存タイマー
    private float   timer             = 0.0f;
    // スポーン直後の衝突を防ぐフレームカウンター
    private int     spawnDelayFrames  = 5;
    // 同フレーム内での多重Destroy防止フラグ
    private bool    isDestroyReserved = false;
    // バフ前の色を保持しておく
    private Vector4 originalColor     = Vector4.one;
    private bool    colorSaved        = false;
    // バフを一度だけ発動するためのフラグ
    private bool    supportBuffApplied_ = false;
    // スケール変化のログを1回だけ出すためのフラグ
    private bool    scaleLoggedOnce_    = false;
    // 前回のスケール適用状態
    private ReinforcementState lastAppliedState_ = ReinforcementState.Normal;

    // 各種参照
    private Entity              cameraEntity  = null;
    private Entity              playerEntity  = null;
    private FieldManager        fieldManager_ = null;
    // はまり〜打ち上げを担うスクリプト
    private ReinforcementFallIn fallIn_       = null;

    // FieldFall が参照するプロパティ
    public bool IsTrapped    => fallIn_ != null && fallIn_.IsActive;
    public bool IsRetreating => isRetreating;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        positionApplied     = false;
        isRetreating        = false;
        isCollisionEnabled  = false;
        isDestroyReserved   = false;
        supportBuffApplied_ = false;
        scaleLoggedOnce_     = false;
        lastAppliedState_    = ReinforcementState.Normal;
        state_               = ReinforcementState.Normal;
        timer               = 0.0f;
        spawnDelayFrames    = 2;
        colorSaved          = false;

        // 参照の取得
        cameraEntity = ecsGroup.FindEntity("Camera");
        playerEntity = ecsGroup.FindEntity("Player");

        Entity fmEntity = ecsGroup.FindEntity("FieldManager");
        if (fmEntity != null)
        {
            fieldManager_ = fmEntity.GetScript<FieldManager>();
        }

        // 同じEntityにアタッチされているはまり処理スクリプトを取得
        fallIn_ = entity.GetScript<ReinforcementFallIn>();

        // HPを10に設定
        HP hp = entity.GetScript<HP>();
        if (hp != null)
        {
            hp.MAX_HP = 10;
            hp.currentHp = 10;
        }
    }

    public override void Update()
    {
        // すでにDestroy済みなら何もしない
        if (entity.Id == 0)
        {
            return;
        }

        // Destroy予約済みなら即削除
        if (isDestroyReserved)
        {
            entity.Destroy();
            return;
        }

        ReacquireEntities();
        ApplyInitialPosition();

        // スポーン直後は当たり判定を無効にして数フレーム待つ
        if (spawnDelayFrames > 0)
        {
            spawnDelayFrames--;
            isCollisionEnabled = false;
            return;
        }

        // 通常状態に戻ったら当たり判定を有効化
        if (!isRetreating && !isCollisionEnabled)
        {
            Debug.Log($"<color=green>[Reinforcement:Enable]</color> Collision enabled for {entity.name} (ID:{entity.Id}) after delay.");
            isCollisionEnabled = true;
        }

        // 打ち上げ中は画面外に出たら消える
        if (fallIn_ != null && fallIn_.IsLaunched)
        {
            if (!IsInScreenFrustum())
            {
                isRetreating = true;
                entity.Destroy();
            }
            return;
        }

        // 穴にはまってる間は他の処理をしない
        if (fallIn_ != null && fallIn_.IsActive)
        {
            return;
        }

        if (UpdateTimer())
        {
            return;
        }

        CheckFieldFall();
        ApplyStateScale();
        UpdateFrustumVisibility();
        UpdateMovement();
    }

    // =========================================================
    // Update サブルーチン
    // =========================================================

    // 途中でnullになったEntityを再取得する
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

    // startPositionが確定したら1回だけ座標を適用する
    private void ApplyInitialPosition()
    {
        // 既に座標が適用されている場合は何もしない
        if (positionApplied)
        {
            return;
        }
        if (startPosition.Length() < 0.001f)
        {
            return;
        }

        transform.position = startPosition;
        positionApplied = true;
    }

    // 生存時間を管理して期限切れで退散させる
    private bool UpdateTimer()
    {
        timer += Time.deltaTime;
        if (timer < lifeTime)
        {
            return false;
        }

        // 画面内なら退散、画面外ならそのまま消す
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

    // 状態が変化した時だけスケールを書き込む（毎フレーム書くとエディタのコマンド履歴がたまってクラッシュする）
    private void ApplyStateScale()
    {
        if (state_ == lastAppliedState_)
        {
            return;
        }

        float target = 1.0f;
        if (state_ == ReinforcementState.Supported)
        {
            target = supportedScale;

            if (!scaleLoggedOnce_)
            {
                Debug.Log($"<color=orange>[ApplyStateScale]</color> {entity.name} scale → {supportedScale}");
                scaleLoggedOnce_ = true;
            }
        }

        transform.scale = new Vector3(target, target, target);
        lastAppliedState_ = state_;
    }

    // =========================================================
    // コールバック
    // =========================================================

    public override void OnDestroy()
    {
        // 退散で消えた場合は死亡扱いにしない
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

    // =========================================================
    // ダメージ / 攻撃
    // =========================================================

    // 被弾時に呼ばれる
    public void TakeDamage()
    {
        // 穴にはまってる間はダメージを受けない
        if (isDestroyReserved || (fallIn_ != null && fallIn_.IsActive))
        {
            return;
        }

        Debug.Log($"<color=red>[Reinforcement:Hit]</color> {entity.name} (ID:{entity.Id}) was DESTROYED by an attack.");
        isDestroyReserved = true;
    }

    // ボスへの攻撃を通知して退散する
    public void AttackBoss()
    {
        onAttackBoss?.Invoke((int)damage, transform.position);
        Retreat();
    }

    // =========================================================
    // フィールド落下インターフェース
    // =========================================================

    // 自分がいるセルが落下中かどうかを毎フレームチェック
    private void CheckFieldFall()
    {

        // FieldManagerが存在しない場合は何もしない
        if (fieldManager_ == null)
        {
            return;
        }

        // グリッド内かのチェック
        int row, col;
        if (!fieldManager_.WorldToGrid(transform.position, out row, out col))
        {
            return;
        }

        // 落下中のセルかのチェック
        Entity cellEntity = fieldManager_.GetCellAt(row, col);
        if (cellEntity == null)
        {
            return;
        }

        // 落下中のセルにいる場合ははまり処理を開始
        FieldFall fieldFall = cellEntity.GetScript<FieldFall>();
        if (fieldFall == null || !fieldFall.IsPlaying)
        {
            return;
        }

        // 落下中のセルに乗ってたらはまり判定を開始
        fieldFall.TrapReinforcement(this);
    }

    // FieldFallから呼ばれる、ReinforcementFallInにはまり処理を委譲してバフも発動
    public void TrapToCell(float stuckY, float cellCenterX, float cellCenterZ)
    {
        fallIn_?.StartFallIn(stuckY, cellCenterX, cellCenterZ);
        isCollisionEnabled = false;

        // 1回だけバフを発動する
        if (!supportBuffApplied_)
        {
            ApplySupportBuff();
            supportBuffApplied_ = true;
        }
    }

    // FieldFallから呼ばれる、床が戻るタイミングで上に打ち上げる
    public void LaunchUpward()
    {
        isCollisionEnabled = false;
        fallIn_?.Launch();
    }
}

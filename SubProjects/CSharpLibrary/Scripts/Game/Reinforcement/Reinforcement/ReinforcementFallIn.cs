// 穴にはまってから打ち上げられるまでの一連の挙動を管理するスクリプト
public class ReinforcementFallIn : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    // 挟まり位置へ移動するLerp時間
    [SerializeField] public float lerpDuration = 0.3f;
    // 打ち上げ速度
    [SerializeField] public float launchSpeed  = 20.0f;

    // =========================================================
    // 内部状態
    // =========================================================

    // 穴にはまっている状態かどうか
    private bool  isActive_   = false;
    // 打ち上げ中かどうか
    private bool  isLaunched_ = false;
    // 挟まり位置へ向かってLerp中かどうか
    private bool  isLerping_  = false;
    // Lerpのタイマー
    private float lerpTimer_  = 0f;
    // Lerp開始時のY座標
    private float startY_     = 0f;
    // 挟まり位置（目標座標）
    private float targetY_    = 0f;
    private float targetX_    = 0f;
    private float targetZ_    = 0f;

    // Reinforcement.cs が参照するプロパティ
    public bool IsActive   => isActive_;
    public bool IsLaunched => isLaunched_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        isActive_   = false;
        isLaunched_ = false;
        isLerping_  = false;
        lerpTimer_  = 0f;
    }

    public override void Update()
    {
        // すでにDestroy済みなら何もしない
        if (entity.Id == 0)
        {
            return;
        }

        // 何もしていない時は早期リターン
        if (!isActive_ && !isLaunched_)
        {
            return;
        }

        // transformがnullの場合もスキップ
        if (transform == null)
        {
            return;
        }

        // 打ち上げ中は上方向に飛ばすだけ
        if (isLaunched_)
        {
            transform.position += new Vector3(0, launchSpeed, 0) * Time.deltaTime;
            return;
        }

        // 挟まり位置へLerpで移動する
        if (isLerping_)
        {
            lerpTimer_ += Time.deltaTime;
            float t = Mathf.Clamp01(lerpTimer_ / lerpDuration);
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(startY_, targetY_, t);
            pos.x = Mathf.Lerp(pos.x, targetX_, t);
            pos.z = Mathf.Lerp(pos.z, targetZ_, t);
            transform.position = pos;
            // Lerpが終わったら静止
            if (t >= 1f)
            {
                isLerping_ = false;
            }
        }
    }

    // =========================================================
    // 外部から呼ぶ
    // =========================================================

    // FieldFallのTrapReinforcementから呼ばれる、はまり開始
    public void StartFallIn(float stuckY, float cellCenterX, float cellCenterZ)
    {
        isActive_   = true;
        isLaunched_ = false;
        isLerping_  = true;
        lerpTimer_  = 0f;
        startY_     = transform.position.y;
        targetY_    = stuckY;
        targetX_    = cellCenterX;
        targetZ_    = cellCenterZ;
    }

    // FieldFallのWaitingフェーズ終了時に呼ばれる、床が戻るタイミングで打ち上げ
    public void Launch()
    {
        isActive_   = false;
        isLerping_  = false;
        isLaunched_ = true;
    }
}

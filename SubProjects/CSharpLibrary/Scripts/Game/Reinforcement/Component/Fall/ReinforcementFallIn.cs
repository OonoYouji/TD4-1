// 穴にはまってから打ち上げられるまでの一連の挙動を管理するスクリプト
public class ReinforcementFallIn : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    // 挟まり位置へ移動するLerp時間
    [SerializeField] public float lerpDuration    = 0.3f;
    // 打ち上げ速度
    [SerializeField] public float launchSpeed     = 20.0f;
    // はまり中の上下シェイク幅
    [SerializeField] public float shakeAmplitude  = 0.05f;
    // はまり中の上下シェイク速度
    [SerializeField] public float shakeFrequency  = 20.0f;

    // 打ち上げ中のY回転速度（度/秒）
    private const float LAUNCH_ROTATE_SPEED = 360.0f;

    // =========================================================
    // 内部状態
    // =========================================================

    private System.Action currentPhase_ = null;

    // Lerpのタイマー
    private float lerpTimer_  = 0f;
    // シェイク用タイマー
    private float shakeTimer_ = 0f;
    // Lerp開始時のY座標
    private float startY_     = 0f;
    // 挟まり位置（目標座標）
    private float targetY_    = 0f;
    private float targetX_    = 0f;
    private float targetZ_    = 0f;

    // Reinforcement.cs が参照するプロパティ
    public bool IsActive   => currentPhase_ == PhaseFallingIn || currentPhase_ == PhaseShaking;
    public bool IsLaunched => currentPhase_ == PhaseLaunching;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        currentPhase_ = null;
        lerpTimer_    = 0f;
        shakeTimer_   = 0f;
    }

    public override void Update()
    {
        // すでにDestroy済みなら何もしない
        if (entity.Id == 0)
        {
            return;
        }

        // transformがnullの場合もスキップ
        if (transform == null)
        {
            return;
        }

        // 現在のフェーズの処理を呼び出す
        currentPhase_?.Invoke();
    }

    // =========================================================
    // フェーズ処理
    // =========================================================

    // 挟まり位置へLerpで移動する
    private void PhaseFallingIn()
    {

        // タイマーを更新し、Tを計算
        lerpTimer_ += Time.deltaTime;
        float t = Mathf.Clamp01(lerpTimer_ / lerpDuration);

        // 現在のY座標を計算し、適応
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(startY_, targetY_, t);
        pos.x = Mathf.Lerp(pos.x, targetX_, t);
        pos.z = Mathf.Lerp(pos.z, targetZ_, t);
        transform.position = pos;

        // Lerpが完了したら次のフェーズへ
        if (t >= 1f)
        {
            shakeTimer_   = 0f;
            currentPhase_ = PhaseShaking;
        }
    }


    private void PhaseShaking()
    {
        shakeTimer_ += Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y = targetY_ + Mathf.Sin(shakeTimer_ * shakeFrequency) * shakeAmplitude;
        transform.position = pos;
    }

    private void PhaseLaunching()
    {
        // 上方向に飛ばしながらY回転する
        transform.position += new Vector3(0, launchSpeed, 0) * Time.deltaTime;
        transform.rotation = transform.rotation * Quaternion.MakeFromAxis(Vector3.up, LAUNCH_ROTATE_SPEED * Mathf.Deg2Rad * Time.deltaTime);
    }

    // =========================================================
    // 外部から呼ぶ
    // =========================================================

    // FieldFallのTrapReinforcementから呼ばれる、はまり開始
    public void StartFallIn(float stuckY, float cellCenterX, float cellCenterZ)
    {
        targetY_ = stuckY;
        targetX_ = cellCenterX;
        targetZ_ = cellCenterZ;

        startY_    = transform.position.y;
        lerpTimer_ = 0f;

        currentPhase_ = PhaseFallingIn;
    }

    // FieldFallのWaitingフェーズ終了時に呼ばれる、床が戻るタイミングで打ち上げ
    public void Launch()
    {
        currentPhase_ = PhaseLaunching;
    }
}

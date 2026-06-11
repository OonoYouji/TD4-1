using System;
using System.Reflection;
public class PlayerSmashUp : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 叩きつけモーション

    // 上昇速度と目標位置
    [SerializeField] public float riseHeight = 3.0f;
    [SerializeField] public float riseDuration = 0.2f;
    [SerializeField] public float fallDuration = 0.1f;
    [SerializeField] public float fallPosY = 0.1f;
    [SerializeField] public float returnDuration = 0.1f;

    // 待機時間
    [SerializeField] public float waitTimeAfterRise = 0.1f;

    // =========================================================
    // 内部状態
    // =========================================================
    private Vector3 originPos_;
    private float timer_;

    // 再生フラグ
    private bool isPlaying_;

    public bool IsPlaying => isPlaying_;
    private System.Action currentPhase_;
    private FieldManager fieldManager_;
    private PlayerAudio playerAudio_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    // 叩きつけの間隔（クールタイム）
    [SerializeField] public float smashInterval = 1.0f;

    public float smashTimer_ { get; private set; } = 0.0f;
    private Player player_;

    public override void Initialize()
    {

        // FieldManagerの取得
        Entity managerEntity = ecsGroup.FindEntity("FieldManager");
        if (managerEntity != null)
        {
            fieldManager_ = managerEntity.GetScript<FieldManager>();
        }

        // プレイヤーの取得
        player_ = entity.GetScript<Player>();
        smashTimer_ = 0.0f;
        playerAudio_ = entity.GetScript<PlayerAudio>();
    }

    public override void Update()
    {
        smashTimer_ += Time.deltaTime;
        HandleSmash();

        if (!isPlaying_)
        {
            return;
        }

        // タイマー更新
        timer_ += Time.deltaTime;

        // 現在のフェーズを実行
        currentPhase_?.Invoke();
    }

    // Phase 2 以降のみ、右クリック / X ボタンで叩きつけ
    private void HandleSmash()
    {

        // フェーズ2以降から使える
        string phase = AIUpdater.lastBossPhase;
        if (phase != "Phase 2" && phase != "Phase 3")
        {
            return;
        }

        // 右クリックとXボタンの入力をチェック
        bool wantSmash =
            Input.TriggerKey(KeyCode.Space) ||
            Input.PressGamepad(Gamepad.LeftThumb)||
            Input.PressGamepad(Gamepad.LeftShoulder);

        // クールタイム後に叩きつけ
        if (smashTimer_ >= smashInterval && wantSmash)
        {
            smashTimer_ = 0.0f;
            Play();
        }
    }
    public void Play()
    {
        if (player_ != null && player_.IsFrozen) {
            return;
        }

        // 元の位置を保存
        originPos_ = transform.position;
        timer_ = 0f;

        // フラグ初期化
        isPlaying_ = true;

        // 最初のフェーズを上昇に設定
        currentPhase_ = Rising;
    }

    private void Rising()
    {
        // 上昇の動き
        float t = Mathf.Clamp01(timer_ / riseDuration);
        transform.position = originPos_ + new Vector3(0, riseHeight * t, 0);

        // 上昇が終わってから待機する
        if (timer_ >= riseDuration)
        {
            timer_ = 0f;
            currentPhase_ = Falling;
        }
    }

    private void Falling()
    {
        // 落下の動き
        float t = Mathf.Clamp01(timer_ / fallDuration);
        transform.position = transform.position = new Vector3(originPos_.x, fallPosY * t, originPos_.z);

        // 落下が終わったら元の位置に戻す
        if (timer_ >= fallDuration)
        {
            timer_ = 0f;
            currentPhase_ = Returning;
            fieldManager_?.TriggerCellAt(originPos_);
            playerAudio_?.PlayHit();
        }
    }

    private void Returning()
    {
        // 落下の動き
        float t = Mathf.Clamp01(timer_ / returnDuration);
        transform.position = new Vector3(originPos_.x, originPos_.y * t, originPos_.z);

        // 戻るのが終わったら終わり
        if (timer_ >= returnDuration)
        {
            currentPhase_ = null;
            transform.position = originPos_;
            isPlaying_ = false;

        }
    }


}

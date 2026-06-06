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

    // =========================================================
    // ライフサイクル
    // =========================================================

    private Player player_;

    public override void Initialize()
    {
        Entity managerEntity = ecsGroup.FindEntity("FieldManager");
        if (managerEntity != null)
        {
            fieldManager_ = managerEntity.GetScript<FieldManager>();
        }
        player_ = entity.GetScript<Player>();
    }

    public override void Update()
    {
        if (!isPlaying_)
        {
            return;
        }

        // タイマー更新
        timer_ += Time.deltaTime;

        // 現在のフェーズを実行
        currentPhase_?.Invoke();
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
        transform.position = originPos_ + new Vector3(0, riseHeight * (1f - t), 0);

        // 落下が終わったら元の位置に戻す
        if (timer_ >= fallDuration)
        {
            transform.position = originPos_;
            isPlaying_ = false;
            fieldManager_?.TriggerCellAt(originPos_);
        }
    }


}

public class FieldFall : MonoScript
{
    // 床がこの距離だけ落ちてからはまり判定を開始する
    private const float TRAP_TRIGGER_DEPTH = 0.3f;

    // 落下する距離
    [SerializeField] public float fallDistance    = 5.0f;
    // 落下にかかる時間
    [SerializeField] public float fallDuration    = 0.3f;
    // 戻る前の待機時間
    [SerializeField] public float waitDuration    = 2.0f;
    // 戻るのにかかる時間
    [SerializeField] public float returnDuration  = 0.3f;

    // 落下前の保存用の座標
    private float originX_;
    private float originY_;
    private float originZ_;

    // 落下開始からの経過時間
    private float timer_;
    // 落下中かどうか
    private bool  isPlaying_;
    // 
    private bool  isReturning_;  
    private System.Action currentPhase_;

    // はまれるのは1体のみ
    private Reinforcement trappedUnit_ = null;

    public bool IsPlaying => isPlaying_;

    public override void Initialize()
    {
        Vector3 p = transform.position;
        originX_ = p.x;
        originY_ = p.y;
        originZ_ = p.z;
        isPlaying_    = false;
        isReturning_  = false;
        currentPhase_ = null;
    }

    public override void Update()
    {
        if (!isPlaying_) {
            return; 
        }
        // 現在のフェーズの処理を呼び出す
        timer_ += Time.deltaTime;
        currentPhase_?.Invoke();
    }

    public void StartFalling()
    {
        if (isPlaying_) { 
            return; 
        }

        // 落下前の座標の保存
        Vector3 origin = transform.position;
        originX_ = origin.x;
        originY_ = origin.y;
        originZ_ = origin.z;

        // 落下開始
        trappedUnit_ = null;
        isReturning_ = false;
        timer_       = 0f;
        isPlaying_   = true;
        currentPhase_ = Falling;
    }

    public void TrapReinforcement(Reinforcement reinforcement)
    {
        if (isReturning_) {
            return;
        }

        // すでに挟まっているユニットがいる場合は無視
        if (reinforcement == null || reinforcement.IsTrapped || reinforcement.IsRetreating) {
            return; 
        }

        // // はまる対象の援軍が出るまで続ける
        if (trappedUnit_ != null) {
            return;
        }

        // 床がTRAP_TRIGGER_DEPTH以上落ちるまではまりを起こさない
        if (originY_ - transform.position.y < TRAP_TRIGGER_DEPTH) { 
            return; 
        }

        // はまる深さは援軍プレハブ側の sinkDepth で設定する
        float stuckY = originY_ - reinforcement.sinkDepth;

        // はまる対象の援軍をセット
        reinforcement.TrapToCell(stuckY, originX_, originZ_);
        trappedUnit_ = reinforcement;
    }

    private void Falling()
    {

        // 落下中の処理
        float t    = Mathf.Clamp01(timer_ / fallDuration);
        float newY = originY_ - fallDistance * t;
        transform.position = new Vector3(originX_, newY, originZ_);

        // 落下が完了したら次のフェーズへ
        if (timer_ >= fallDuration)
        {
            timer_ = 0f;
            currentPhase_ = Waiting;
        }
    }

    private void Waiting()
    {
        if (timer_ >= waitDuration)
        {

            // 待機が完了したら次のフェーズへ
            timer_ = 0f;
            isReturning_ = true;
            if (trappedUnit_ != null)
            {
                // はまっている援軍がいる場合は打ち上げて戻る前に解除
                trappedUnit_.LaunchUpward(); 
                trappedUnit_ = null;
            }
            currentPhase_ = Returning;
        }
    }

    private void Returning()
    {

        // 戻る処理
        float t    = Mathf.Clamp01(timer_ / returnDuration);
        float newY = (originY_ - fallDistance) + fallDistance * t;
        transform.position = new Vector3(originX_, newY, originZ_);

        // 戻りが完了したら落下前の状態にリセット
        if (timer_ >= returnDuration)
        {
            transform.position = new Vector3(originX_, originY_, originZ_);
            isPlaying_    = false;
            isReturning_  = false;
            currentPhase_ = null;
        }
    }
}

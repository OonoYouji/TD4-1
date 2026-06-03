public class FieldFall : MonoScript
{
    // 落下する距離
    [SerializeField] public float fallDistance    = 5.0f;
    // 落下にかかる時間
    [SerializeField] public float fallDuration    = 0.3f;
    // 戻る前の待機時間
    [SerializeField] public float waitDuration    = 2.0f;
    // 戻るのにかかる時間
    [SerializeField] public float returnDuration  = 0.3f;
    // 挟まり演出：元の地面より何単位下に固定するか
    [SerializeField] public float trapDepthOffset = 0.5f;

    private float originX_;
    private float originY_;
    private float originZ_;
    private float timer_;
    private bool  isPlaying_;
    private bool  isReturning_;  // Returning中は新規ではまりを拒否する
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
        if (!isPlaying_) return;
        timer_ += Time.deltaTime;
        currentPhase_?.Invoke();
    }

    public void StartFalling()
    {
        if (isPlaying_) return;

        Vector3 origin = transform.position;
        originX_ = origin.x;
        originY_ = origin.y;
        originZ_ = origin.z;

        trappedUnit_ = null;
        isReturning_ = false;
        timer_       = 0f;
        isPlaying_   = true;
        currentPhase_ = Falling;
    }

    // 援軍側から呼ばれる。Falling/Waiting中のみ1体受け付ける
    public void TrapReinforcement(Reinforcement r)
    {
        if (isReturning_) return;
        if (r == null || r.IsTrapped || r.IsRetreating) return;
        if (trappedUnit_ != null) return;

        float stuckY = originY_ - trapDepthOffset;
        r.TrapToCell(stuckY, originX_, originZ_);
        trappedUnit_ = r;
    }

    private void Falling()
    {
        float t    = Mathf.Clamp01(timer_ / fallDuration);
        float newY = originY_ - fallDistance * t;
        transform.position = new Vector3(originX_, newY, originZ_);

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
            timer_       = 0f;
            isReturning_ = true;
            if (trappedUnit_ != null)
            {
                trappedUnit_.LaunchUpward();  // 打ち上げ速度はReinforcementFallIn側で持つ
                trappedUnit_ = null;
            }
            currentPhase_ = Returning;
        }
    }

    private void Returning()
    {
        float t    = Mathf.Clamp01(timer_ / returnDuration);
        float newY = (originY_ - fallDistance) + fallDistance * t;
        transform.position = new Vector3(originX_, newY, originZ_);

        if (timer_ >= returnDuration)
        {
            transform.position = new Vector3(originX_, originY_, originZ_);
            isPlaying_    = false;
            isReturning_  = false;
            currentPhase_ = null;
        }
    }
}

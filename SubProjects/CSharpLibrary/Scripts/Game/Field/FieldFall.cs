public class FieldFall : MonoScript
{

    // 落下する距離
    [SerializeField] public float fallDistance   = 5.0f;
    // 落下にかかる時間
    [SerializeField] public float fallDuration   = 0.3f;
    // 戻る前の待機時間
    [SerializeField] public float waitDuration   = 2.0f;
    // 戻るのにかかる時間
    [SerializeField] public float returnDuration = 0.3f;

    // 内部状態
    private float originX_;
    private float originY_;
    private float originZ_;
    private float timer_;
    private bool isPlaying_;
    private System.Action currentPhase_;

    public override void Initialize()
    {
        Vector3 p = transform.position;
        originX_ = p.x;
        originY_ = p.y;
        originZ_ = p.z;
        isPlaying_ = false;
        currentPhase_ = null;
    }

    public override void Update()
    {

        // 再生していない場合は何もしない
        if (!isPlaying_) {
            return;
        }

        // フェーズの更新
        timer_ += Time.deltaTime;
        currentPhase_?.Invoke();
    }

    public void StartFalling()
    {
        if (isPlaying_) {
            return;
        }

        // 元の位置を保存
        Vector3 origin = transform.position;
        originX_ = origin.x;
        originY_ = origin.y;
        originZ_ = origin.z;

        // タイマーをリセットして再生開始
        timer_ = 0f;
        isPlaying_ = true;
        currentPhase_ = Falling;
    }

    private void Falling()
    {

        // 落下する動き（XZ は origin に固定して Y のみ変化させる）
        float t = Mathf.Clamp01(timer_ / fallDuration);
        transform.position = new Vector3(originX_, originY_ - fallDistance * t, originZ_);


        // 落下が終わったら待機フェーズに移行
        if (timer_ >= fallDuration)
        {
            timer_ = 0f;
            currentPhase_ = Waiting;
        }
    }

    private void Waiting()
    {

        // 待機が終わったら元に戻るフェーズに移行
        if (timer_ >= waitDuration)
        {
            timer_ = 0f;
            currentPhase_ = Returning;
        }
    }

    private void Returning()
    {

        // 元に戻る動き（XZ は origin に固定して Y のみ変化させる）
        float t = Mathf.Clamp01(timer_ / returnDuration);
        transform.position = new Vector3(originX_, (originY_ - fallDistance) + fallDistance * t, originZ_);

        // 元に戻ったら再生終了
        if (timer_ >= returnDuration)
        {
            transform.position = new Vector3(originX_, originY_, originZ_);
            isPlaying_ = false;
            currentPhase_ = null;
        }
    }
}

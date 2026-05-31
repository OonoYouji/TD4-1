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
    private float originY_;
    private float timer_;
    private bool isPlaying_;
    private System.Action currentPhase_;

    public override void Initialize()
    {

        originY_ = transform.position.y;
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
        originY_ = transform.position.y;

        // タイマーをリセットして再生開始
        timer_ = 0f;
        isPlaying_ = true;
        currentPhase_ = Falling;
    }

    private void Falling()
    {

        // 落下する動き
        float t = Mathf.Clamp01(timer_ / fallDuration);
        Vector3 pos = transform.position;
        pos.y = originY_ - fallDistance * t;
        transform.position = pos;


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

        // 元に戻る動き
        float t = Mathf.Clamp01(timer_ / returnDuration);
        Vector3 pos = transform.position;
        pos.y = (originY_ - fallDistance) + fallDistance * t;
        transform.position = pos;


        // 元に戻ったら再生終了
        if (timer_ >= returnDuration)
        {
            pos.y = originY_;
            transform.position = pos;
            isPlaying_ = false;
            currentPhase_ = null;
        }
    }
}

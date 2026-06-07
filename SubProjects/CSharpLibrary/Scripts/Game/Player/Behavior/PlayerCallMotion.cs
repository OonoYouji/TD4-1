using System;

public class PlayerCallMotion : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    // アニメーションを繰り返す回数
    [SerializeField] public int loopCount = 3;
    // スケールが膨らむ時間
    [SerializeField] public float expandDuration = 0.15f;
    // スケールが元に戻る時間
    [SerializeField] public float shrinkDuration = 0.1f;
    // ピーク時のスケール倍率
    [SerializeField] public float scaleMultiplier = 1.3f;

    // =========================================================
    // 内部状態
    // =========================================================

    private Vector3 baseScale_;
    private float timer_;
    private bool isPlaying_;
    private int loopsRemaining_;
    private Action currentPhase_;

    public bool IsPlaying => isPlaying_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        baseScale_ = transform.scale;
    }

    public override void Update()
    {
        if (!isPlaying_)
        {
            return;
        }

        // 再生中は再生
        timer_ += Time.deltaTime;
        currentPhase_?.Invoke();
    }

    // =========================================================
    // 制御
    // =========================================================

    public void Play()
    {
        timer_          = 0f;
        loopsRemaining_ = loopCount;
        isPlaying_      = true;
        currentPhase_   = Expanding;
    }

    // =========================================================
    // フェーズ
    // =========================================================

    private void Expanding()
    {
        // tの計算
        float t    = Mathf.Clamp01(timer_ / expandDuration);
        // 最大スケールの計算
        float peak = baseScale_.x * scaleMultiplier;
        // スケールの計算と適用
        float scale    = baseScale_.x + (peak - baseScale_.x) * Ease.Out.Back(t);
        transform.scale = new Vector3(scale, scale, scale);

        // フェーズ遷移
        if (timer_ >= expandDuration)
        {
            timer_        = 0f;
            currentPhase_ = Shrinking;
        }
    }

    private void Shrinking()
    {

        // tの計算
        float t    = Mathf.Clamp01(timer_ / shrinkDuration);
        // 最大スケールの計算
        float peak = baseScale_.x * scaleMultiplier;
        // スケールの計算と適用
        float scale    = peak + (baseScale_.x - peak) * t;
        transform.scale = new Vector3(scale, scale, scale);

        if (timer_ >= shrinkDuration)
        {
            // ループカウントを減らす
            loopsRemaining_--;

            // ループが残ってなかったら終わり
            if (loopsRemaining_ <= 0)
            {
                transform.scale = baseScale_;
                isPlaying_      = false;
                currentPhase_   = null;
            }
            else
            {
                // ループが残ってたら次のループへ
                timer_ = 0f;
                currentPhase_ = Expanding;
            }
        }
    }
}

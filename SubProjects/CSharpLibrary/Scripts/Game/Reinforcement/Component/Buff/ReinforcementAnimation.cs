using System;

public class ReinforcementAnimation : MonoScript
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

    // このループの開始スケール
    private float baseScale_;
    // 最終目標スケール
    private float maxScale_;

    private float scaleStep_;
    private float timer_;
    private bool isPlaying_;
    private int loopsRemaining_;
    private Action currentPhase_;

    public bool IsPlaying => isPlaying_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Update()
    {
        if (!isPlaying_)
        {
            return;
        }
        timer_ += Time.deltaTime;
        currentPhase_?.Invoke();
    }

    // =========================================================
    // 制御
    // =========================================================


    public void Play(float currentScale, float maxScale)
    {

        // 再生開始
        baseScale_ = currentScale;
        maxScale_ = maxScale;
        // ループカウント分均等にスケールを増やしていく為のもの
        scaleStep_ = (maxScale - currentScale) / loopCount;
        timer_ = 0f;
        loopsRemaining_ = loopCount;
        isPlaying_ = true;
        currentPhase_ = Expanding;

    }

    // =========================================================
    // フェーズ
    // =========================================================

    private void Expanding()
    {
        // tの計算
        float t = Mathf.Clamp01(timer_ / expandDuration);
        // 最大スケールへスケールを動かす
        float peak = baseScale_ * scaleMultiplier;
        float scale = baseScale_ + (peak - baseScale_) * Ease.Out.Back(t);
        transform.scale = new Vector3(scale, scale, scale);

        // スケールが沈む動きへ遷移
        if (timer_ >= expandDuration)
        {
            timer_ = 0f;
            currentPhase_ = Shrinking;
        }
    }

    private void Shrinking()
    {

        // tと最大スケールの計算
        float t = Mathf.Clamp01(timer_ / shrinkDuration);
        float peak = baseScale_ * scaleMultiplier;
        // Stepごとにスケールを増やしていく
        float nextBase = baseScale_ + scaleStep_;
        float scale = peak + (nextBase - peak) * t;
        transform.scale = new Vector3(scale, scale, scale);

        if (timer_ >= shrinkDuration)
        {
            // ループカウントを減らす
            loopsRemaining_--;
            baseScale_ = nextBase;

            if (loopsRemaining_ <= 0)
            {
                //ループが終わったら再生終了
                transform.scale = new Vector3(maxScale_, maxScale_, maxScale_);
                isPlaying_ = false;
                currentPhase_ = null;
            }
            else
            {
                timer_ = 0f;
                currentPhase_ = Expanding;
            }
        }
    }
}

using System;

/// <summary>
/// 演出プレハブが生成された瞬間に、SEやカメラシェイクなどの単発イベントを発火させるスクリプト。
/// 消滅（AI再開）の管理は TimedDestruction に任せます。
/// </summary>
public class BossPerformanceTrigger : MonoScript {
    [SerializeField] public bool shakeCamera = false;
    [SerializeField] public float shakeIntensity = 0.5f;
    [SerializeField] public float shakeDuration = 1.0f;
    [SerializeField] public string soundPath = "";
    [SerializeField] public string effectName = ""; // FrameEventで出す追加エフェクト（あれば）

    public override void Initialize() {
        // 1. カメラシェイクの発火
        if (shakeCamera) {
            // エンジン側のCameraShakeシステムをトリガー
            FrameEvent.EnqueueNamedEvent("CameraShake", entity.Id);
        }

        // 2. SEの再生
        if (!string.IsNullOrEmpty(soundPath)) {
            var audio = entity.GetComponent<AudioSource>();
            if (audio != null) {
                audio.OneShotPlay(1.0f, 1.0f, soundPath);
            }
        }

        // 3. 追加エフェクトの発火（プレハブ自体にParticleが含まれていない場合など）
        if (!string.IsNullOrEmpty(effectName)) {
            FrameEvent.EnqueueEffectEvent(effectName, entity.Id, 1.0f, 2.0f);
        }

    }
}


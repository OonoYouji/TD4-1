using System;

/// <summary>
/// カメラを揺らす（Camera Shake）演出を管理するMonoScript。
/// InvokeEventNode から "CameraShake" イベントが送られると、メインカメラを揺らします。
/// </summary>
public class CameraShake : MonoScript
{
    private float _shakeIntensity = 0f;
    private float _shakeDuration = 0f;
    private Vector3 _originalPos;
    private bool _isShaking = false;

    public override void Initialize()
    {
        // イベント待機
    }

    public override void Update()
    {
        // イベントチェック (TODO: エンジン側のイベント受取口が整備されたら有効化)
        /*
        if (FrameEvent.HasNamedEvent("CameraShake", entity.Id))
        {
            Shake(0.5f, 0.5f);
        }
        */

        if (_isShaking)
        {
            if (_shakeDuration > 0)
            {
                Vector3 randomOffset = new Vector3(
                    (float)(new Random().NextDouble() * 2 - 1) * _shakeIntensity,
                    (float)(new Random().NextDouble() * 2 - 1) * _shakeIntensity,
                    (float)(new Random().NextDouble() * 2 - 1) * _shakeIntensity
                );
                transform.position = _originalPos + randomOffset;
                _shakeDuration -= Time.deltaTime;
            }
            else
            {
                _isShaking = false;
                transform.position = _originalPos;
            }
        }
    }

    public void Shake(float intensity, float duration)
    {
        if (!_isShaking)
        {
            _originalPos = transform.position;
        }
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _isShaking = true;
    }
}

using System;

/// <summary>
/// スポーンされたエフェクトプレハブの挙動とライフサイクルを管理するスクリプト。
/// 親（発生源）への追従や、消滅時のAIへの完了通知を担当します。
/// </summary>
public class EffectLifecycleHandler : MonoScript
{
    [SerializeField] public bool followOwner = false;
    [SerializeField] public bool notifyAI = false;
    [SerializeField] public string eventName = "";
    [SerializeField] public float lifeTime = 2.0f;

    private int _ownerId = -1;
    private float _timer = 0f;
    private bool _isInitialized = false;

    public void SetOwner(int ownerId)
    {
        _ownerId = ownerId;
        _isInitialized = true;

        if (followOwner)
        {
            Entity owner = ecsGroup.GetEntity(ownerId);
            if (owner != null)
            {
                entity.parent = owner;
                // 親のローカル座標系での位置をリセット
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
            }
        }
    }

    public override void Initialize()
    {
        _timer = 0f;
    }

    public override void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= lifeTime)
        {
            OnFinish();
        }
    }

    private void OnFinish()
    {
        if (notifyAI && _ownerId != -1 && !string.IsNullOrEmpty(eventName))
        {
            // AIに対して完了を通知
            BlackboardManager.SetBool(_ownerId, eventName, true);
            Debug.Log($"[EffectLifecycle] Notified AI: EventComplete_{eventName} for Entity:{_ownerId}");
        }

        entity.Destroy();
    }
}

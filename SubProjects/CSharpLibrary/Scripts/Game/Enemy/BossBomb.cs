using System;

/// <summary>
/// ボスの爆弾攻撃用スクリプト。
/// 着弾時に急拡大して範囲ダメージを与え、消滅する。
/// </summary>
public class BossBomb : MonoScript
{
    public Vector3 targetPosition = Vector3.zero;
    public float travelTime = 1.5f; // 目標地点までにかける時間
    public float arcHeight = 5.0f;  // 弧の高さ（最大高度）
    public float explosionScale = 7.0f;
    public float explosionDuration = 0.3f;
    public int damage = 20;

    private float _timer = 0.0f;
    private bool _isExploding = false;
    private float _explosionTimer = 0.0f;
    private Vector3 _initialScale;
    private Vector3 _startPosition;

    public override void Initialize()
    {
        _timer = 0.0f;
        _isExploding = false;
        _explosionTimer = 0.0f;
        _initialScale = transform.scale;
        _startPosition = transform.position;

        // トリガー設定を適用（飛行中の物理反発を防止）
        var sphere = entity.GetComponent<SphereCollider>();
        if (sphere != null) sphere.isTrigger = true;
        var box = entity.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;

        // 初期状態では判定を無効化しておく（着弾時に有効化）
        var trigger = entity.GetScript<DamageTrigger>();
        if (trigger != null) trigger.enable = false;
    }

    public void Launch(Vector3 start, Vector3 target, float duration, float height)
    {
        _startPosition = start;
        targetPosition = target;
        travelTime = duration;
        arcHeight = height;
        transform.position = start;
        _timer = 0.0f;
        _isExploding = false;
    }

    public override void Update()
    {
        if (!_isExploding)
        {
            // デバッグ表示 (着弾予定地に紫の円を表示、太さ16.0)
            GizmoBatch.DrawWireCircle(targetPosition + Vector3.up * 0.05f, explosionScale * 0.5f, new Vector4(1, 0, 1, 1), 16, 16.0f);

            _timer += Time.deltaTime;
            float t = _timer / travelTime;

            if (t <= 1.0f)
            {
                // 放物線移動の計算
                // XZ平面は線形補間、Y軸は二次関数で弧を作る
                Vector3 currentPos = Vector3.Lerp(_startPosition, targetPosition, t);
                
                // 放物線の高さ: 4 * h * t * (1 - t)
                float heightOffset = 4.0f * arcHeight * t * (1.0f - t);
                currentPos.y += heightOffset;

                transform.position = currentPos;
            }
            else
            {
                StartExplosion();
            }
        }
        else
        {
            // デバッグ表示 (爆発範囲を紫で表示、太さ16.0)
            GizmoBatch.DrawWireCircle(transform.position + Vector3.up * 0.1f, explosionScale * 0.5f, new Vector4(1, 0, 1, 1), 24, 16.0f);

            // 爆発演出（急拡大）
            _explosionTimer += Time.deltaTime;
            float t = _explosionTimer / explosionDuration;
            
            if (t <= 1.0f)
            {
                transform.scale = Vector3.Lerp(_initialScale, _initialScale * explosionScale, t);
            }
            else
            {
                // 演出終了
                entity.Group.DestroyEntity(entity.Id);
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        // 地面（地面に類する名前）またはプレイヤーに当たったら爆発
        if (!_isExploding && (collision.name.Contains("Player") || collision.name.Contains("Ground") || collision.name.Contains("Stage")))
        {
            StartExplosion();
        }
    }

    private void StartExplosion()
    {
        if (_isExploding) return;
        _isExploding = true;

        // 当たり判定（DamageTrigger）の有効化とパラメータ設定
        var trigger = entity.GetScript<DamageTrigger>();
        if (trigger != null)
        {
            trigger.enable = true;
            trigger.damage = damage;
            trigger.interval = 0; // 単発
        }

        // --- 燃焼エリアの生成 (Spec v2) ---
        Entity fireArea = entity.Group.CreateEntity("BurningArea");
        if (fireArea != null)
        {
            fireArea.parent = null;
            fireArea.transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
            fireArea.transform.scale = new Vector3(explosionScale, 0.1f, explosionScale);
        }

        // 爆発イベント
        FrameEvent.EnqueueNamedEvent("Effect_Explosion", entity.Id);
        Debug.Log($"[BossBomb] Exploding at {Vector3.ToSimpleString(transform.position)}");
    }
}

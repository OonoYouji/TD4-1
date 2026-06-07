using System;

/// <summary>
/// 上空から落下する巨大な柱の挙動を制御するスクリプト。
/// 仕様書v2対応：
/// 1. 高速落下し、地面到達時に衝撃波ダメージを発生。
/// 2. 直撃したプレイヤーに大ダメージとスタン（3秒）を与える。
/// 3. 落下後は 10秒間 フィールドに残り、障害物（壁）となる。
/// </summary>
public class FallingPillar : MonoScript
{
    public float fallSpeed = 50.0f;
    public float impactRadius = 5.0f;
    public int directDamage = 100;
    public int shockwaveDamage = 30;
    public float stunDuration = 3.0f;
    public float stayDuration = 10.0f;

    private bool _isFalling = true;
    private float _stayTimer = 0.0f;
    private Vector3 _targetPos;
    private bool _hasImpacted = false;

    public void Launch(Vector3 startPos, Vector3 targetPos)
    {
        transform.position = startPos;
        _targetPos = targetPos;
        _isFalling = true;
        _hasImpacted = false;
        _stayTimer = 0.0f;

        // 落下中はトリガーモードにする（物理反発を防ぐ）
        var collider = entity.GetComponent<BoxCollider>();
        if (collider != null) collider.isTrigger = true;

        var trigger = entity.GetScript<DamageTrigger>();
        if (trigger != null) trigger.enable = false;
    }

    public override void Update()
    {
        if (_isFalling)
        {
            // デバッグ表示 (着弾予定地に紫の円を表示、太さ16.0)
            GizmoBatch.DrawWireCircle(_targetPos + Vector3.up * 0.05f, impactRadius, new Vector4(1, 0, 1, 1), 24, 16.0f);

            // 真下へ落下
            Vector3 pos = transform.position;
            pos.y -= fallSpeed * Time.deltaTime;

            if (pos.y <= _targetPos.y)
            {
                pos.y = _targetPos.y;
                transform.position = pos;
                OnImpact();
            }
            else
            {
                transform.position = pos;
            }
        }
        else
        {
            // 落下後の静止・消滅カウント
            _stayTimer += Time.deltaTime;
            if (_stayTimer >= stayDuration)
            {
                entity.Group.DestroyEntity(entity.Id);
            }
        }
    }
private void OnImpact()
{
    if (_hasImpacted) return;
    _hasImpacted = true;
    _isFalling = false;

    Debug.Log($"<color=red>[FallingPillar:Impact]</color> GENERATED Impact at {Vector3.ToSimpleString(transform.position)} with Radius: {impactRadius}");

    // 1. 衝撃演出
    FrameEvent.EnqueueNamedEvent("Effect_PillarImpact", entity.Id);
    FrameEvent.EnqueueNamedEvent("CameraShake_Strong", entity.Id);
    Debug.Log($"[FallingPillar] Impact at {Vector3.ToSimpleString(transform.position)}");

    // 2. 当たり判定（直撃と衝撃波）の処理
    var entities = entity.Group.GetEntities();
    foreach (var e in entities)
    {
        if (e == null || e.Id == entity.Id) continue;

        // 名前チェック
        if (!e.name.Contains("Player") && !e.name.Contains("Reinforcement")) continue;

        float dist = Vector3.Distance(transform.position, e.transform.position);
        if (dist <= impactRadius)
        {
            // 非常に近い場合は直撃
            if (dist <= 2.0f)
            {
                BossDamageUtil.ApplyDamage(e, directDamage, transform.position);
                BossDamageUtil.ApplySlow(e, 0.0f, stunDuration); // 速度0 = スタン

                if (e.name.Contains("Player"))
                {
                    Debug.Log("<color=red>[Pillar]</color> Direct hit on Player! 3s Stun.");
                }
            }
            else
            {
                BossDamageUtil.ApplyDamage(e, shockwaveDamage, transform.position);
            }
        }
    }

    // 3. 障害物（壁）としての設定
    // 着弾後は物理的な壁として機能させる
    var collider = entity.GetComponent<BoxCollider>();
    if (collider != null)
    {
        collider.isTrigger = false;
    }
}

    public override void OnCollisionEnter(Entity collision)
    {
        if (_isFalling && (collision.name.Contains("Ground") || collision.name.Contains("Stage")))
        {
            OnImpact();
        }
    }
}

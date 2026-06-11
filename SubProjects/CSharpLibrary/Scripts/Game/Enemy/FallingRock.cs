using System;

/// <summary>
/// 落下する岩の挙動を制御するスクリプト。
/// 地面やプレイヤーに接触した際に範囲ダメージとスタンを発生させる。
/// </summary>
public class FallingRock : MonoScript
{
    public float impactRadius = 3.0f;
    public int damage = 40;
    public float stunDuration = 2.0f;

    private bool _isFalling = false;
    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _hasImpacted = false;
    private float _throwElapsed = 0f;
    private float _throwDuration = 1.0f;
    private float _destroyTimer = 0f;

    public override void Initialize()
    {
        _isFalling = false;
        _hasImpacted = false;
        _destroyTimer = 0f;
    }

    public void Launch(Vector3 targetPos)
    {
        _startPos = transform.position;
        _targetPos = targetPos;
        _isFalling = true;
        _hasImpacted = false;
        _throwElapsed = 0f;
        _destroyTimer = 0f;

        // 距離に応じて到達時間を計算 (秒速40m程度)
        float horizontalDist = Vector3.Distance(new Vector3(_startPos.x, 0, _startPos.z), new Vector3(_targetPos.x, 0, _targetPos.z));
        _throwDuration = Math.Max(0.5f, horizontalDist / 40.0f);

        // 飛行中はトリガーにする（プレイヤーなどに引っかかって空中に止まるのを防ぐ）
        var box = entity.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
        var sphere = entity.GetComponent<SphereCollider>();
        if (sphere != null) sphere.isTrigger = true;
    }

    public override void Update()
    {
        if (!_isFalling) return;

        // デバッグ表示 (着弾予定地に紫の円を表示、太さ16.0)
        GizmoBatch.DrawWireCircle(_targetPos + Vector3.up * 0.05f, impactRadius, new Vector4(1, 0, 1, 1), 16, 16.0f);

        _throwElapsed += Time.deltaTime;
        float t = Math.Min(1.0f, _throwElapsed / _throwDuration);

        // 軌道の計算
        Vector3 currentPos = Vector3.Lerp(_startPos, _targetPos, t);
        
        // 放物線の高さ（ピークで8m程度）
        float heightArc = 8.0f * (1.0f - (float)Math.Pow(2.0f * t - 1.0f, 2.0f));
        currentPos.y += heightArc;

        transform.position = currentPos;

        if (t >= 1.0f)
        {
            // 着弾（地面）
            Impact(null, true); 
            _isFalling = false;
            transform.position = _targetPos;
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (collision == null || collision.Id == 0) return;

        if (_isFalling)
        {
            // プレイヤーなどに当たってもダメージだけ与え、移動は継続（空中浮遊防止）
            Impact(collision, false);
        }
    }

    private void Impact(Entity primaryTarget, bool isFinalImpact)
    {
        // すでに着弾演出済みで、かつ今回が空中衝突なら何もしない
        if (_hasImpacted && !isFinalImpact) return;

        // まだダメージを与えていないなら計算（空中または地面で一度だけ実行）
        if (!_hasImpacted)
        {
            _hasImpacted = true;

            // 直接当たった対象がいれば優先的に適用
            if (primaryTarget != null)
            {
                if (primaryTarget.name.Contains("Player") || primaryTarget.name.Contains("Reinforcement"))
                {
                    ApplyImpact(primaryTarget);
                }
            }

            // 範囲内のプレイヤーや援軍にダメージとスタンを与える
            if (entity.Group != null)
            {
                var entities = entity.Group.GetEntities();
                foreach (var e in entities)
                {
                    if (e == null || e.Id == 0) continue;

                    // 直接当たった対象は二重適用を避ける
                    if (primaryTarget != null && e.Id == primaryTarget.Id) continue;

                    if (e.name.Contains("Player") || e.name.Contains("Reinforcement"))
                    {
                        float dist = Vector3.Distance(transform.position, e.transform.position);
                        if (dist <= impactRadius)
                        {
                            ApplyImpact(e);
                        }
                    }
                }
            }

            // 演出（一度だけ）
            FrameEvent.EnqueueNamedEvent("Effect_RockImpact", entity.Id);
        }

        // 地面着弾時のみフラグを最終確定
        if (isFinalImpact)
        {
            _hasImpacted = true;

            // 地面に着いたらトリガーを解除して物理的に残るようにする
            var box = entity.GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = false;
            var sphere = entity.GetComponent<SphereCollider>();
            if (sphere != null) sphere.isTrigger = false;
        }

    }

    private void ApplyImpact(Entity e)
    {
        // 共通ユーティリティを使用してダメージとスロウを適用
        BossDamageUtil.ApplyDamage(e, damage, transform.position);
        BossDamageUtil.ApplySlow(e, 0.5f, stunDuration); // 岩の場合は50%スロウ（スタン扱い）

    }
}


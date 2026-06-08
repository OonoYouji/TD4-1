using System;

/// <summary>
/// 落下する岩の挙動を制御するスクリプト。
/// 地面やプレイヤーに接触した際に範囲ダメージとスタンを発生させる。
/// </summary>
public class FallingRock : MonoScript
{
    public float fallSpeed = 30.0f;
    public float impactRadius = 3.0f;
    public int damage = 40;
    public float stunDuration = 2.0f;

    private bool _isFalling = false;
    private Vector3 _targetPos;
    private bool _hasImpacted = false;

    public override void Initialize()
    {
        _isFalling = false;
        _hasImpacted = false;
    }

    public void Launch(Vector3 targetPos)
    {
        _targetPos = targetPos;
        _isFalling = true;
        _hasImpacted = false;

        // トリガー設定を適用（押し戻し防止）
        var box = entity.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
        var sphere = entity.GetComponent<SphereCollider>();
        if (sphere != null) sphere.isTrigger = true;

        Debug.Log($"[FallingRock] Launched towards {Vector3.ToSimpleString(targetPos)}");
    }

    public override void Update()
    {
        if (!_isFalling || _hasImpacted) return;

        // デバッグ表示 (着弾予定地に紫の円を表示、太さ16.0)
        GizmoBatch.DrawWireCircle(_targetPos + Vector3.up * 0.05f, impactRadius, new Vector4(1, 0, 1, 1), 16, 16.0f);

        // 下方向に移動
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // 地面付近（Y座標がターゲットとほぼ同じ）になったら着弾
        if (transform.position.y <= _targetPos.y + 0.5f)
        {
            Impact();
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (collision == null || collision.Id == 0) return;

        if (_isFalling && !_hasImpacted)
        {
            // 何かに当たったら即着弾（プレイヤーへの直撃も含む）
            Impact();
        }
    }
private void Impact()
{
    if (_hasImpacted) return;
    _hasImpacted = true;
    _isFalling = false;

    Debug.Log($"<color=red>[FallingRock:ImpactArea]</color> GENERATED Impact at {Vector3.ToSimpleString(transform.position)} with Radius: {impactRadius}");

    // 着弾位置を固定
    transform.position = new Vector3(transform.position.x, _targetPos.y, transform.position.z);

        // 範囲内のプレイヤーや援軍にダメージとスタンを与える
        var entities = entity.Group.GetEntities();
        foreach (var e in entities)
        {
            if (e == null || e.Id == 0) continue;

            if (e.name.Contains("Player") || e.name.Contains("Reinforcement"))
            {
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist <= impactRadius)
                {
                    ApplyImpact(e);
                }
            }
        }

        // 演出
        FrameEvent.EnqueueNamedEvent("Effect_RockImpact", entity.Id);
        Debug.Log($"<color=brown>[FallingRock]</color> IMPACT at {Vector3.ToSimpleString(transform.position)}");
    }

    private void ApplyImpact(Entity e)
    {
        // 共通ユーティリティを使用してダメージとスロウを適用
        BossDamageUtil.ApplyDamage(e, damage, transform.position);
        BossDamageUtil.ApplySlow(e, 0.5f, stunDuration); // 岩の場合は50%スロウ（スタン扱い）

        Debug.Log($"[FallingRock] Impact applied to {e.name}");
    }
}

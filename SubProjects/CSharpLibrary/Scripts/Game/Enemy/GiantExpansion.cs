using System;

/// <summary>
/// ボスの足元から広がる範囲攻撃演出と判定を制御するスクリプト。
/// 徐々に拡大し、触れたプレイヤーをノックバックさせる。
/// </summary>
public class GiantExpansion : MonoScript
{
    public float maxRadius = 15.0f;
    public float duration = 1.0f;
    public float knockbackForce = 10.0f;
    public int damage = 10;

    private float _timer = 0.0f;
    private Vector3 _initialScale;

    public override void Initialize()
    {
        _timer = 0.0f;
        _initialScale = new Vector3(0.1f, 0.05f, 0.1f);
        transform.scale = _initialScale;

        var trigger = entity.GetScript<DamageTrigger>();
        if (trigger != null)
        {
            trigger.damage = damage;
            trigger.interval = 0.5f; // 多段ヒット防止のため少し間隔をあける
            trigger.targetTag = "Player";
        }
    }

    public override void Update()
    {
        _timer += Time.deltaTime;
        float t = Math.Min(_timer / duration, 1.0f);

        // スケールの拡大 (Yは一定)
        float currentScale = Math.Max(0.1f, t * maxRadius);
        transform.scale = new Vector3(currentScale, 0.05f, currentScale);

        if (t >= 1.0f)
        {
            // 演出終了後、少し待ってから消去（余韻）
            if (_timer >= duration + 0.5f)
            {
                entity.Group.DestroyEntity(entity.Id);
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        ApplyKnockback(collision);
    }

    public override void OnCollisionStay(Entity collision)
    {
        ApplyKnockback(collision);
    }

    private void ApplyKnockback(Entity e)
    {
        if (e.name.Contains("Player"))
        {
            Vector3 diff = e.transform.position - transform.position;
            diff.y = 0; // 水平方向のみ
            if (diff.sqrMagnitude > 0.001f)
            {
                Vector3 dir = diff.Normalized();
                // 簡易的なノックバック（座標を直接動かす）
                // 本来は物理エンジンのVelocityに加算するのが望ましい
                e.transform.position += dir * knockbackForce * Time.deltaTime;
            }
        }
    }
}

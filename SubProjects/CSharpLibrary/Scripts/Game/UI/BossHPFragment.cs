using System;

/// <summary>
/// HPバーが割れた際の破片を制御するクラス。
/// </summary>
public class BossHPFragment : MonoScript
{
    private Vector3 velocity;
    private float angularVelocity;
    private float lifetime = 1.0f;
    private SpriteRenderer renderer;

    public void Setup(Vector3 startPos, Vector3 vel, float angVel, Vector4 color, Vector3 scale)
    {
        transform.position = startPos;
        velocity = vel;
        angularVelocity = angVel;
        transform.scale = scale;
        
        renderer = entity.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    public override void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0)
        {
            entity.Destroy();
            return;
        }

        // 移動と回転
        transform.position += velocity * Time.deltaTime;
        velocity.y -= 1000.0f * Time.deltaTime; // 簡易重力

        // Z軸回転（Quaternionへの変換が必要だが、簡易的にwのみ更新は不可）
        // transform.rotation = ...
        
        // 透明化
        if (renderer != null)
        {
            Vector4 c = renderer.color;
            c.w = lifetime;
            renderer.color = c;
        }
    }
}

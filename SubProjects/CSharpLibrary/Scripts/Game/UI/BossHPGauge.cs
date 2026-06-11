using System;

/// <summary>
/// 特定のエンティティ（ボスなど）のHPを画面上に表示するためのUIゲージ。
/// </summary>
public class BossHPGauge : MonoScript
{
    [SerializeField]
    public string targetEntityName = "Boss";
    [SerializeField]
    public float width = 1000.0f;
    [SerializeField]
    public float height = 50.0f;

    private SpriteRenderer renderer;
    private HP targetHP;
    private float defaultX = 0;

    public override void Initialize()
    {
        renderer = entity.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
        }

        defaultX = transform.position.x;

        FindTarget();
    }

    private void FindTarget()
    {
        Entity target = ecsGroup.FindEntity(targetEntityName);
        if (target != null)
        {
            targetHP = target.GetScript<HP>();
            if (targetHP != null)
            {
            }
            else
            {
            }
        }
    }

    public override void Update()
    {
        if (targetHP == null)
        {
            FindTarget();
            return;
        }

        float hpRatio = targetHP.CurrentHpRatio();

        // ゲージのスケールと位置を更新
        transform.scale = new Vector3(width * hpRatio, height, 1.0f);
        transform.position.x = Mathf.Lerp(defaultX, -width / 2.0f + defaultX, 1.0f - hpRatio);
    }
}


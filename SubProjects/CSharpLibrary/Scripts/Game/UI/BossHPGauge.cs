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
//             Debug.LogError("SpriteRenderer component not found on the BossHPGauge entity.");
        }

        defaultX = transform.position.x;
//         Debug.Log($"[BossHPGauge] UI Initialized. ID:{entity.Id}, DefaultX:{defaultX}");

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
//                 Debug.Log($"[BossHPGauge] Linked to target '{targetEntityName}'(ID:{target.Id}). HP:{targetHP.currentHp}/{targetHP.MAX_HP}");
            }
            else
            {
//                 Debug.LogWarning($"[BossHPGauge] Target '{targetEntityName}' found but no HP script attached.");
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

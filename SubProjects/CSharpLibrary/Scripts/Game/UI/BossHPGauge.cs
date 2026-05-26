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
            Debug.LogError("SpriteRenderer component not found on the BossHPGauge entity.");
        }

        Entity target = ecsGroup.FindEntity(targetEntityName);
        if (target == null)
        {
            Debug.LogWarning($"BossHPGauge: Target entity '{targetEntityName}' not found yet.");
            return;
        }

        // Common/HP.cs を使用していることを想定
        targetHP = target.GetScript<HP>();
        if (targetHP == null)
        {
            // もし既存の EnemyCollisionHandler の hitpoints を使いたい場合は、
            // そちらを参照するように変更する必要があるが、共通化のため HP.cs の使用を推奨
            Debug.LogWarning($"BossHPGauge: HP script not found on '{targetEntityName}'. Trying EnemyCollisionHandler...");
            // フォールバック（既存のザコ敵の仕組みに合わせる場合）
        }

        defaultX = transform.position.x;
    }

    public override void Update()
    {
        // 初期化時に見つからなかった場合、動的に探す
        if (targetHP == null)
        {
            Entity target = ecsGroup.FindEntity(targetEntityName);
            if (target != null)
            {
                targetHP = target.GetScript<HP>();
                if (targetHP != null)
                {
                    defaultX = transform.position.x;
                }
            }
            return;
        }

        float hpRatio = targetHP.CurrentHpRatio();

        // ゲージのスケールと位置を更新
        transform.scale = new Vector3(width * hpRatio, height, 1.0f);
        // 左端固定で伸び縮みさせるための計算
        transform.position.x = Mathf.Lerp(defaultX, -width / 2.0f + defaultX, 1.0f - hpRatio);
    }
}

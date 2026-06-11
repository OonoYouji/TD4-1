using System;

/// <summary>
/// 攻撃の判定（メッシュやコライダー）にアタッチし、
/// 接触した対象にダメージを与える汎用スクリプト。
/// ボスの攻撃全般（ビーム、爆発、竜巻など）で使用可能。
/// 
/// 仕様書v2対応：ヒット時のデバフ（スロウ）効果を追加。
/// </summary>
public class DamageTrigger : MonoScript
{
    [SerializeField]
    public int damage = 10;

    [SerializeField]
    public float interval = 0.5f; // 持続ダメージの間隔 (0の場合は単発)

    [SerializeField]
    public string targetTag = "Player"; // 当てる対象 (Player, Reinforcement など)

    public float slowMultiplier = 1.0f; // 1.0なら等速、0.8なら20%低下
    public float slowDuration = 0.0f;

    private float timer = 0f;
    private bool hasHit = false; // 単発ダメージ用

    public override void Initialize()
    {
        timer = 0f;
        hasHit = false;
    }

    public override void Update()
    {
        if (timer > 0) timer -= Time.deltaTime;
    }

    public override void OnCollisionStay(Entity collision)
    {
        if (interval > 0)
        {
            if (timer <= 0 && IsTarget(collision))
            {
                ApplyDamageTo(collision);
                timer = interval;
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (IsTarget(collision))
        {
            if (interval <= 0)
            {
                if (!hasHit)
                {
                    ApplyDamageTo(collision);
                }
            }
            else
            {
                if (timer <= 0)
                {
                    ApplyDamageTo(collision);
                    timer = interval;
                }
            }
        }
    }

    private bool IsTarget(Entity e)
    {
        return e.name.Contains("Player") || e.name.Contains("Reinforcement") || e.name.Contains(targetTag);
    }

    private void ApplyDamageTo(Entity e)
    {
        
        // --- 共通ユーティリティを使用してダメージ適用 ---
        BossDamageUtil.ApplyDamage(e, damage, transform.position);

        // --- デバフ効果の適用 (Spec v2) ---
        if (slowDuration > 0.001f)
        {
            BossDamageUtil.ApplySlow(e, slowMultiplier, slowDuration);
        }
    }
}


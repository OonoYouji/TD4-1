using System;

/// <summary>
/// 攻撃の判定（メッシュやコライダー）にアタッチし、
/// 接触した対象にダメージを与える汎用スクリプト。
/// ボスの攻撃全般（ビーム、爆発、竜巻など）で使用可能。
/// </summary>
public class DamageTrigger : MonoScript
{
    [SerializeField]
    public int damage = 10;

    [SerializeField]
    public float interval = 0.5f; // 持続ダメージの間隔 (0の場合は単発)

    [SerializeField]
    public string targetTag = "Player"; // 当てる対象 (Player, Reinforcement など)

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
                // Debug.Log($"[DamageTrigger] OnCollisionStay hit target: {collision.name}");
                ApplyDamageTo(collision);
                timer = interval;
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        // Debug.Log($"[DamageTrigger] OnCollisionEnter with: {collision.name}");
        
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
                // インターバルがある攻撃でも、最初の接触でダメージを与える
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
        Debug.Log($"<color=white>[DamageTrigger:Trace]</color> ApplyDamageTo enters for {e.name}");
        
        // 1. 汎用 DamageHandler 経由
        DamageHandler handler = e.GetScript<DamageHandler>();
        if (handler != null)
        {
            handler.ApplyDamage(damage, transform.position);
            return;
        }

        // 2. 援軍専用 ReinforcementDamageHandler 経由
        ReinforcementDamageHandler rHandler = e.GetScript<ReinforcementDamageHandler>();
        if (rHandler != null)
        {
            rHandler.ApplyDamage(damage, transform.position);
            return;
        }

        // 3. 援軍（Reinforcement）への直接処理（Handlerがない場合の旧方式フォールバック）
        Reinforcement reinforcement = e.GetScript<Reinforcement>();
        if (reinforcement != null)
        {
            if (reinforcement.isCollisionEnabled)
            {
                Debug.Log($"<color=orange>[DamageTrigger]</color> Hit Reinforcement (Direct): {e.name} from {entity.name}");
                reinforcement.TakeDamage();
            }
            return;
        }

        // 4. プレイヤーへの直接フォールバック（名前判定）
        if (e.name.Contains("Player"))
        {
            // HPコンポーネントを直接探す
            HP hp = e.GetScript<HP>();
            if (hp != null)
            {
                hp.TakeDamage(damage);
            }
        }
    }
}

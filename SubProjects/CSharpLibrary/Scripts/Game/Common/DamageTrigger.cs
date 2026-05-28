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
                ApplyDamageTo(collision);
                timer = interval;
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (interval <= 0 && !hasHit)
        {
            if (IsTarget(collision))
            {
                ApplyDamageTo(collision);
                // 貫通するかどうかは要件によるが、とりあえずこのオブジェクト自体は消滅しない前提
                // hasHit = true; // 1回しかダメージ判定を出さない場合はコメントアウト解除
            }
        }
    }

    private bool IsTarget(Entity e)
    {
        return e.name.Contains("Player") || e.name.Contains("Reinforcement") || e.name.Contains(targetTag);
    }

    private void ApplyDamageTo(Entity e)
    {
        // DamageHandler経由でダメージを与える
        DamageHandler handler = e.GetScript<DamageHandler>();
        if (handler != null)
        {
            handler.ApplyDamage(damage, transform.position);
            return;
        }

        // 旧PlayerCore等の直接処理（DamageHandlerがない場合のフォールバック）
        // 実際のプロジェクトの構成に応じて削除してよい
        if (e.name.Contains("Player"))
        {
            // PlayerCoreのTakeDamageをリフレクション等で呼ぶか、直接キャストする
            // ここでは簡易的にメッセージを出すのみとする
            Debug.Log($"DamageTrigger applied {damage} damage to {e.name}.");
        }
    }
}

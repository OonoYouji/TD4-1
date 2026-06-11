using System;
using System.Collections.Generic;

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

    // 個別の対象ごとのクールダウンタイマーを管理する
    private Dictionary<int, float> hitTimers = new Dictionary<int, float>();

    public override void Initialize()
    {
        hitTimers.Clear();
    }

    public override void Update()
    {
        // 辞書のタイマーを更新
        List<int> keys = new List<int>(hitTimers.Keys);
        foreach (int key in keys)
        {
            if (hitTimers[key] > 0)
            {
                hitTimers[key] -= Time.deltaTime;
            }
        }
    }

    public override void OnCollisionStay(Entity collision)
    {
        if (collision == null || collision.Id == 0) return;

        if (interval > 0 && IsTarget(collision))
        {
            if (!hitTimers.ContainsKey(collision.Id) || hitTimers[collision.Id] <= 0)
            {
                ApplyDamageTo(collision);
                hitTimers[collision.Id] = interval;
            }
        }
    }

    public override void OnCollisionEnter(Entity collision)
    {
        if (collision == null || collision.Id == 0) return;

        if (IsTarget(collision))
        {
            if (interval <= 0)
            {
                // 単発ダメージ：一度当たった相手には二度と当たらない
                if (!hitTimers.ContainsKey(collision.Id))
                {
                    ApplyDamageTo(collision);
                    hitTimers[collision.Id] = float.MaxValue;
                }
            }
            else
            {
                // 持続ダメージ：初回ヒット
                if (!hitTimers.ContainsKey(collision.Id) || hitTimers[collision.Id] <= 0)
                {
                    ApplyDamageTo(collision);
                    hitTimers[collision.Id] = interval;
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


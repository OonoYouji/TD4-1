using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ボスの攻撃によるダメージ処理を統一するためのユーティリティクラス。
/// プレイヤーと援軍の両方を対象としたダメージ適用や範囲攻撃をサポートします。
/// </summary>
public static class BossDamageUtil
{
    /// <summary>
    /// 単一のエンティティに対して、種類（Player/Reinforcement）に応じた適切な方法でダメージを与えます。
    /// </summary>
    /// <param name="target">対象エンティティ</param>
    /// <param name="damage">ダメージ量</param>
    /// <param name="attackerPos">攻撃者の位置</param>
    public static void ApplyDamage(Entity target, int damage, Vector3 attackerPos)
    {
        if (target == null || target.Id == 0) return;


        // 1. 汎用 DamageHandler (HPを持つエンティティ、ボス、障害物など)
        DamageHandler handler = target.GetScript<DamageHandler>();
        if (handler != null)
        {
            handler.ApplyDamage(damage, attackerPos);
            return;
        }

        // 2. 援軍専用 ReinforcementDamageHandler (HPを持つ援軍)
        ReinforcementDamageHandler rHandler = target.GetScript<ReinforcementDamageHandler>();
        if (rHandler != null)
        {
            rHandler.ApplyDamage(damage, attackerPos);
            return;
        }

        // 3. 援軍 Reinforcement (HPスクリプトを介さない簡易的な援軍)
        Reinforcement reinforcement = target.GetScript<Reinforcement>();
        if (reinforcement != null)
        {
            // 衝突判定が有効な場合のみダメージ（穴にはまってる間などは無効）
            if (reinforcement.isCollisionEnabled)
            {
                reinforcement.TakeDamage();
            }
            return;
        }

        // 4. プレイヤー (Fallback: スクリプト経由でなく直接HPを操作)
        if (target.name.Contains("Player"))
        {
            HP hp = target.GetScript<HP>();
            if (hp != null) {
                hp.TakeDamage(damage);
            }
        }
    }

    // デバッグ用: 当たり判定の走査ログを出力するかどうか
    private static bool _traceHits = true;

    /// <summary>
    /// 指定された範囲内の全ての対象（Player/Reinforcement）にダメージを与えます。
    /// </summary>
    /// <param name="group">ECSグループ</param>
    /// <param name="center">攻撃の中心座標</param>
    /// <param name="radius">攻撃半径</param>
    /// <param name="damage">ダメージ量</param>
    public static void ApplyAreaDamage(ECSGroup group, Vector3 center, float radius, int damage)
    {
        
//         if (_traceHits) 

        // 開発用デバッグ表示 (赤い円、太さ12.0)
        GizmoBatch.DrawWireCircle(center + Vector3.up * 0.1f, radius, new Vector4(1, 0, 0, 1), 32, 12.0f);

        // 走査中にコレクションが変更される（Entity.Destroy() など）のを防ぐため、
        // まず対象をリストに抽出してからダメージを適用する
        var allEntities = group.GetEntities();
        List<Entity> targets = new List<Entity>();

//         if (_traceHits) 

        foreach (var e in allEntities)
        {
            if (e == null || e.Id == 0) continue;

            string name = e.name;
            string lowerName = name.ToLower();
            bool isTarget = lowerName.Contains("player") || lowerName.Contains("reinforcement");
            
            float dist = Vector3.Distance(center, e.transform.position);

            if (_traceHits && isTarget) 
            {
            }

            if (!isTarget) continue;

            if (dist <= radius)
            {
//                 if (_traceHits) 
                targets.Add(e);
            }
        }

        foreach (var target in targets)
        {
            ApplyDamage(target, damage, center);
        }

//         if (targets.Count > 0) 
//         if (_traceHits) 
    }

    /// <summary>
    /// 対象がプレイヤーの場合、移動速度を低下（スロウ）させます。
    /// </summary>
    /// <param name="target">対象エンティティ</param>
    /// <param name="multiplier">速度倍率</param>
    /// <param name="duration">持続時間</param>
    public static void ApplySlow(Entity target, float multiplier, float duration)
    {
        if (target == null || target.Id == 0) return;

        if (target.name.Contains("Player"))
        {
            Player player = target.GetScript<Player>();
            if (player != null)
            {
                player.ApplySlow(multiplier, duration);
            }
        }
    }
}


using System;
using System.Collections.Generic;

/// <summary>
/// 個別の吸引フィールド（竜巻など）を制御するクラス。
/// 周囲のエンティティを自身の中心へ引き寄せる。
/// </summary>
public class VortexField : MonoScript
{
    public float suctionRadius = 15.0f;
    public float suctionForce = 20.0f;
    public float duration = 5.0f;
    public float centerDamageRadius = 3.0f;
    public int centerDamage = 10;
    public float damageInterval = 0.5f;

    private float lifeTimer = 0.0f;
    private float damageTimer = 0.0f;

    public override void Initialize()
    {
        lifeTimer = duration;
        damageTimer = 0.0f;

        // トリガー設定を適用（吸い込み中に反発しないようにする）
        var sphere = entity.GetComponent<SphereCollider>();
        if (sphere != null) sphere.isTrigger = true;
        var box = entity.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }

    public override void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            ecsGroup.DestroyEntity(entity.Id);
            return;
        }

        bool applyDamage = false;
        damageTimer -= Time.deltaTime;
        if (damageTimer <= 0)
        {
            applyDamage = true;
            damageTimer = damageInterval;
        }

        // 吸引ロジック
        Vector3 center = transform.position;
        foreach (var e in ecsGroup.GetEntities())
        {
            if (e == null || e.Id == entity.Id) continue;
            // プレイヤーや援軍、岩などを対象とする
            
            float dist = Vector3.Distance(center, e.transform.position);
            if (dist <= suctionRadius && dist > 0.1f)
            {
                // 距離の2乗に反比例する吸引力
                float power = suctionForce / (dist * dist + 1.0f);
                
                // 岩や柱の場合は吸引力を強める
                if (e.name.Contains("Rock") || e.name.Contains("Pillar")) power *= 1.5f;

                Vector3 pullDir = (center - e.transform.position).Normalized();
                e.transform.position += pullDir * power * Time.deltaTime;

                // 中心部ダメージ
                if (applyDamage && dist <= centerDamageRadius)
                {
                    e.GetScript<HP>()?.TakeDamage(centerDamage);
                }
            }
        }

        // デバッグ表示 (紫、太さ16.0)
        GizmoBatch.DrawWireCircle(center, suctionRadius, new Vector4(1, 0, 1, 0.5f), 32, 16.0f);
    }
}

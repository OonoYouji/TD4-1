using System;
using System.Collections.Generic;

/// <summary>
/// 個別の吸引フィールド（竜巻など）を制御するクラス。
/// 周囲のエンティティを自身の中心へ引き寄せる。
/// </summary>
public class VortexField : MonoScript
{
    public float suctionRadius = 15.0f;
    public float suctionForce = 100.0f; // 大幅に強化
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

            // --- 修正：吸引対象をホワイトリスト形式で厳格に制限する ---
            string ename = e.name;
            bool isValidTarget = ename.Contains("Player") || ename.Contains("Reinforcement") || 
                                 ename.Contains("Rock") || ename.Contains("Pillar") ||
                                 ename.Contains("TargetRock");
            
            if (!isValidTarget) continue;

            // 吸引対象かどうかを距離で判定
            float dist = Vector3.Distance(center, e.transform.position);
            if (dist <= suctionRadius && dist > 0.1f)
            {
                // 質量（Mass）を取得して吸引力に反映させる (a = F / m)
                float mass = 1.0f;
                var sphere = e.GetComponent<SphereCollider>();
                if (sphere != null) mass = sphere.mass;
                else {
                    var box = e.GetComponent<BoxCollider>();
                    if (box != null) mass = box.mass;
                }
                mass = Math.Max(0.1f, mass); // 0除算防止

                // 距離に応じて減衰する吸引力（線形減衰に近づけて範囲内での効きを良くする）
                float power = (suctionForce * (1.0f - (dist / suctionRadius))) / mass;

                Vector3 pullDir = (center - e.transform.position).Normalized();
                Vector3 moveAmount = pullDir * power * Time.deltaTime;
                
                e.transform.position += moveAmount;

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

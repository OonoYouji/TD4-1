using System;
using System.Collections.Generic;

/// <summary>
/// ボスの「寄せ攻撃」を制御するクラス。
/// 複数の吸引フィールドを円状に配置する。
/// </summary>
public class BossVortexAttack : MonoScript
{
    [SerializeField] public float pullingForce = 1000.0f;
    [SerializeField] public float pullingRadius = 300.0f;
    [SerializeField] public int vortexCount = 6;
    [SerializeField] public float spawnRadius = 800.0f;
    [SerializeField] public float duration = 5.0f;
    [SerializeField] public string vortexPrefabName = "VortexField";

    private bool isActive_ = false;
    public bool IsActive => isActive_;

    public override void Update()
    {
        // デバッグ用にVキーで開始
        if (Input.TriggerKey(KeyCode.V))
        {
            StartAttack();
        }
    }

    public void StartAttack()
    {
        if (isActive_) return;
        
//         Debug.Log($"[BossVortexAttack] Spawning {vortexCount} vortex fields.");
        
        float angleStep = 360.0f / vortexCount;
        for (int i = 0; i < vortexCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPos = transform.position + offset;
            spawnPos.y = 0;

            Entity vortex = ecsGroup.CreateEntity(vortexPrefabName);
            if (vortex != null)
            {
                var script = vortex.GetScript<VortexField>();
                if (script != null)
                {
                    script.suctionForce = pullingForce;
                    script.suctionRadius = pullingRadius;
                    script.duration = duration;
                }
                vortex.transform.position = spawnPos;
            }
        }

        // 終了タイマーなどは各VortexFieldが自身で持っているため、
        // マネージャー側は開始するだけでよい（演出同期が必要なら追加する）
    }
}

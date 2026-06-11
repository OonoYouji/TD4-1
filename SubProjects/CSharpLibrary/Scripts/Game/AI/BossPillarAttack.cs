using System;
using System.Collections.Generic;

/// <summary>
/// ボスの「柱落とし攻撃」を制御するクラス。
/// プレイヤーの方向に向け、円周上に沿って端から順番に柱を落下させる。
/// </summary>
public class BossPillarAttack : MonoScript
{
    [SerializeField] public float dropIntervalTime = 0.3f;
    [SerializeField] public float angleStep = 15.0f; // 柱同士の角度間隔
    [SerializeField] public int pillarCount = 5;
    [SerializeField] public float spawnRadius = 700.0f;
    [SerializeField] public string pillarPrefabName = "BossPillar";

    private bool isActive_ = false;
    public bool IsActive => isActive_;
    private int spawnedCount = 0;
    private float timer = 0.0f;
    private float startAngle = 0.0f;
    private Animator animator;
    private string currentAnim = "";

    public override void Initialize()
    {
        animator = entity.GetComponent<Animator>();
    }

    private void PlayAnimation(string clipName)
    {
        if (animator == null || currentAnim == clipName) return;
        animator.CrossFade(clipName, 0.15f);
        currentAnim = clipName;
    }

    public override void Update()
    {
        // デバッグ用にPキーで開始
        if (Input.TriggerKey(KeyCode.P))
        {
            StartAttack();
        }

        if (isActive_)
        {
            // 攻撃中もプレイヤーの方を向く（または開始時の向きを維持）
            RotateToPlayer();

            float totalDuration = pillarCount * dropIntervalTime;
            if (currentAnim != "pillar") {
                animator.CrossFadeWithDuration("pillar", totalDuration);
                currentAnim = "pillar";
            }
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                SpawnPillar();
                timer = dropIntervalTime;
            }
        }
    }

    public void StartAttack()
    {
        if (isActive_) return;

        // プレイヤーの方向を取得
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null) return;

        // 向きを強制
        RotateToPlayer();

        Vector3 toPlayer = player.transform.position - transform.position;
        float centerAngle = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;

        // 端から順番に落とすため、開始角度を計算
        startAngle = centerAngle - (angleStep * (pillarCount - 1) / 2.0f);
        
        isActive_ = true;
        spawnedCount = 0;
        timer = 0.0f;
        PlayAnimation("pillar_start");
    }

    private void RotateToPlayer()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null) return;

        Vector3 diff = player.transform.position - transform.position;
        diff.y = 0;
        if (diff.sqrMagnitude > 0.001f)
        {
            Vector3 dir = diff.Normalized();
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotate = targetRot;

            var aiIntent = entity.GetComponent<AgentIntentComponent>();
            if (aiIntent != null)
            {
                aiIntent.desiredRotation = targetRot;
                aiIntent.useDesiredRotation = true;
            }
        }
    }

    private void SpawnPillar()
    {
        float currentAngle = (startAngle + (spawnedCount * angleStep)) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(currentAngle), 0, Mathf.Sin(currentAngle)) * spawnRadius;
        Vector3 targetPos = transform.position + offset;
        targetPos.y = 0;

        Entity pillar = ecsGroup.CreateEntity(pillarPrefabName);
        if (pillar != null)
        {
            var script = pillar.GetScript<FallingPillar>();
            if (script != null)
            {
                // 上空から落とす
                script.Launch(targetPos + Vector3.up * 1000.0f, targetPos);
            }
            else
            {
                pillar.transform.position = targetPos;
            }
        }

        spawnedCount++;
        if (spawnedCount >= pillarCount)
        {
            isActive_ = false;
            PlayAnimation("pillar_end");
        }
    }
}


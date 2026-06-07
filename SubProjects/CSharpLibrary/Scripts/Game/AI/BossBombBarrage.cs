using System;
using System.Collections.Generic;

/// <summary>
/// ボスの「爆弾バラ撒き攻撃」を制御するクラス。
/// 回転しながら2つの距離へ交互に爆弾を投擲する。
/// </summary>
public class BossBombBarrage : MonoScript
{
    [SerializeField] public float explosionRange = 7.0f;
    [SerializeField] public float fallTime = 1.5f;
    [SerializeField] public float throwInterval = 0.5f;
    [SerializeField] public float rotationSpeed = 90.0f; // 度/秒
    [SerializeField] public float rotationCountToFinish = 1.0f;
    [SerializeField] public float launchDistance1 = 500.0f;
    [SerializeField] public float launchDistance2 = 800.0f;
    [SerializeField] public string bombPrefabName = "BossBomb";

    private bool isActive = false;
    private float throwTimer = 0.0f;
    private float totalRotatedAngle = 0.0f;
    private bool useDistance1 = true;
    private Animator animator;
    private string currentAnim = "";

    public override void Initialize()
    {
        animator = entity.GetComponent<Animator>();
    }

    private void PlayAnimation(string clipName)
    {
        if (animator == null || currentAnim == clipName) return;
        Debug.Log($"[BossAnimation] Changing to: {clipName} (from: {currentAnim})");
        animator.CrossFade(clipName, 0.2f);
        currentAnim = clipName;
    }

    public override void Update()
    {
        if (!isActive)
        {
            if (Input.TriggerKey(KeyCode.D))
            {
                StartAttack();
            }
            return;
        }

        PlayAnimation("bomb");

        // 回転処理
        float deltaAngle = rotationSpeed * Time.deltaTime;
        totalRotatedAngle += Mathf.Abs(deltaAngle);

        // 投擲処理
        throwTimer -= Time.deltaTime;
        if (throwTimer <= 0)
        {
            ThrowBomb();
            throwTimer = throwInterval;
            useDistance1 = !useDistance1;
        }

        // 終了判定
        if (totalRotatedAngle >= rotationCountToFinish * 360.0f)
        {
            isActive = false;
            PlayAnimation("bomb_end");
            Debug.Log("[BossBombBarrage] Attack Finished.");
        }

        // 予測位置の表示
        DrawPrediction();
    }

    public void StartAttack()
    {
        if (isActive) return;
        isActive = true;
        throwTimer = 0.0f;
        totalRotatedAngle = 0.0f;
        useDistance1 = true;
        PlayAnimation("bomb_start");
        Debug.Log("[BossBombBarrage] Starting Rotating Bomb Barrage.");
    }

    private void ThrowBomb()
    {
        float dist = useDistance1 ? launchDistance1 : launchDistance2;
        
        // 現在の「前方」に向けて投げる
        // transform.forward が正しく更新されている想定
        Vector3 targetPos = transform.position + transform.forward * dist;
        targetPos.y = 0; // 地面に着地

        Entity bomb = ecsGroup.CreateEntity(bombPrefabName);
        if (bomb != null)
        {
            var script = bomb.GetScript<BossBomb>();
            if (script != null)
            {
                script.Launch(transform.position, targetPos, fallTime, 300.0f);
            }
        }
    }

    private void DrawPrediction()
    {
        // 現在の向きから予測される着弾点を表示
        float dist = useDistance1 ? launchDistance1 : launchDistance2;
        Vector3 nextTarget = transform.position + transform.forward * dist;
        nextTarget.y = 0;
        GizmoBatch.DrawWireCircle(nextTarget, explosionRange * 10.0f, new Vector4(1, 0.5f, 0, 1));
    }
}

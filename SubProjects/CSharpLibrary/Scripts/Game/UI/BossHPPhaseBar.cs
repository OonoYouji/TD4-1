using System;

/// <summary>
/// 個別のフェーズHPバーを制御するクラス。
/// 自分の担当範囲のHPを表示し、HPがなくなると「割れる」演出を行う。
/// </summary>
public class BossHPPhaseBar : MonoScript
{
    [SerializeField] public float minRatio = 0.7f;
    [SerializeField] public float maxRatio = 1.0f;
    [SerializeField] public float fullWidth = 800.0f;
    [SerializeField] public float barHeight = 30.0f;

    private SpriteRenderer renderer;
    private bool isBroken = false;
    private float breakTimer = 0.0f;
    private Vector3 originalPos;
    private BossHPPhaseManager manager;

    public override void Initialize()
    {
        renderer = entity.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Vector4.green;
        }
        originalPos = transform.position;
        // 初期状態のスケールをそのセグメントの「満タン」の幅とする
        fullWidth = transform.scale.x;

        FindManager();
    }

    public void SetManager(BossHPPhaseManager mgr)
    {
        manager = mgr;
    }

    private void FindManager()
    {
        if (manager != null) return;
        Entity p = entity.parent;
        if (p != null)
        {
            manager = p.GetScript<BossHPPhaseManager>();
        }
    }

    public void UpdateRatio(float totalRatio)
    {
        if (isBroken) return;

        float localRatio = 0.0f;

        if (totalRatio >= maxRatio)
        {
            localRatio = 1.0f;
        }
        else if (totalRatio <= minRatio)
        {
            localRatio = 0.0f;
            Break(); // パリンと割れる
            return;
        }
        else
        {
            // 自分の担当範囲内（線形計算）
            localRatio = (totalRatio - minRatio) / (maxRatio - minRatio);
        }

        ApplyBarVisual(localRatio, totalRatio);
    }

    private void ApplyBarVisual(float localRatio, float totalRatio)
    {
        // 1. スケールの更新 (左端固定・右端が減る)
        float currentScaleX = fullWidth * localRatio;
        transform.scale = new Vector3(currentScaleX, barHeight, 1.0f);
        
        // 2. 位置の調整 (左端を固定)
        float offset = (fullWidth - currentScaleX) / 2.0f;
        transform.position = new Vector3(originalPos.x - offset, originalPos.y, originalPos.z);

        // 3. 色の更新 (緑 -> 黄 -> 赤)
        if (totalRatio > 0.7f) renderer.color = new Vector4(0.2f, 0.8f, 0.2f, 1.0f); // 緑
        else if (totalRatio > 0.4f) renderer.color = new Vector4(0.8f, 0.8f, 0.2f, 1.0f); // 黄
        else renderer.color = new Vector4(0.8f, 0.2f, 0.2f, 1.0f); // 赤
    }

    private void Break()
    {
        if (isBroken) return;
        isBroken = true;
        
        Debug.Log($"<color=white>[BossUI]</color> Bar Shattered! ({entity.name})");
        
        // 1. パーティクル発生 (Effectシステムを利用)
        // 既存の破壊エフェクトを流用、またはログで代替
        FrameEvent.EnqueueNamedEvent("Effect_PillarImpact", entity.Id); 
        
        // 2. 非表示にする
        renderer.enable = 0;
        
        // 3. スケールを0にする
        transform.scale = Vector3.zero;
    }

    private void UpdateBreakEffect()
    {
        if (breakTimer > 0)
        {
            breakTimer -= Time.deltaTime;
            
            // 揺れる演出
            float shake = 10.0f * breakTimer;
            transform.position = originalPos + new Vector3(RandomUtil.NextFloat11() * shake, RandomUtil.NextFloat11() * shake, 0);
            
            // 徐々に透明にする
            renderer.color = new Vector4(1, 1, 1, breakTimer);

            if (breakTimer <= 0)
            {
                renderer.enable = 0; // 完全に消す
            }
        }
    }
}

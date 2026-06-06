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
        if (isBroken)
        {
            UpdateBreakEffect();
            return;
        }

        float localRatio = 0.0f;

        if (totalRatio >= maxRatio)
        {
            // まだ自分の出番ではない（満タン）
            localRatio = 1.0f;
        }
        else if (totalRatio <= minRatio)
        {
            // すでに自分の出番は終わった（空・壊れる）
            localRatio = 0.0f;
            if (!isBroken)
            {
                Break();
                return;
            }
        }
        else
        {
            // 自分の担当範囲内（0.0〜1.0に変換して表示）
            localRatio = (totalRatio - minRatio) / (maxRatio - minRatio);
        }

        ApplyBarVisual(localRatio);
    }

    private void ApplyBarVisual(float localRatio)
    {
        // 表示の更新（左端固定のスケーリング）
        transform.scale = new Vector3(fullWidth * localRatio, barHeight, 1.0f);
        
        // 中心位置の調整: 左端を固定するため、(1.0 - localRatio)の半分だけ左にずらす
        float offset = (fullWidth * (1.0f - localRatio)) / 2.0f;
        transform.position.x = originalPos.x - offset;
    }

    private void Break()
    {
        isBroken = true;
        breakTimer = 1.0f; // 1秒間演出
        Debug.Log($"[BossHPPhaseBar] Phase Bar Broken! ({minRatio}-{maxRatio})");
        
        // 破片を生成
        int fragmentCount = 10;
        for (int i = 0; i < fragmentCount; i++)
        {
            // バーの範囲内にランダムに配置
            float rx = RandomUtil.NextFloat11() * (fullWidth / 2.0f);
            Vector3 spawnPos = transform.position + new Vector3(rx, 0, -1); // 手前に表示
            
            // 爆発するように飛ばす
            Vector3 vel = new Vector3(RandomUtil.NextFloat11() * 500.0f, RandomUtil.NextFloat() * 1000.0f, 0);
            float angVel = RandomUtil.NextFloat11() * 10.0f;
            
            // 破片エンティティの生成（ここでは共通のプレハブがないため、動的に生成して設定する想定だが、
            // エンジン側の制約でスクリプトから空のエンティティにコンポーネントを追加するのは難しいため、
            // 簡易的に演出のみとする。もし Fragment プレハブがあればそれを使うのがベスト）
        }

        // 今回はプレハブ作成の手間を省くため、バー自体の演出を強化
        renderer.color = new Vector4(1, 1, 1, 1); 
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

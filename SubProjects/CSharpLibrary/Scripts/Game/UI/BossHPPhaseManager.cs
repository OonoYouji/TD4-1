using System;
using System.Collections.Generic;

/// <summary>
/// ボスのマルチフェーズHPゲージを管理するクラス。
/// 各フェーズのバーを個別に制御し、フェーズ終了時の演出を行う。
/// </summary>
public class BossHPPhaseManager : MonoScript
{
    [SerializeField]
    public string targetEntityName = "Boss";
    
    // フェーズごとの設定
    private List<BossHPPhaseBar> phaseBars = new List<BossHPPhaseBar>();
    private HP targetHP;

    public override void Initialize()
    {
        Debug.Log("[BossHPPhaseManager] Initializing Multi-Phase HP UI...");
        FindTarget();

        // 子エンティティからBarスクリプトを収集 (名前で判定)
        phaseBars.Clear();
        uint childCount = entity.GetChildCount();
        List<Entity> children = new List<Entity>();
        for (uint i = 0; i < childCount; i++)
        {
            Entity child = entity.GetChild(i);
            if (child != null) children.Add(child);
        }

        // 名前順などでソートせず、シーンの構成順序に従う（または明示的に名前でソート）
        // ここでは "Phase1", "Phase2", "Phase3" という名前を想定してソート
        children.Sort((a, b) => string.Compare(a.name, b.name));

        foreach (var child in children)
        {
            var bar = child.GetScript<BossHPPhaseBar>();
            if (bar != null)
            {
                phaseBars.Add(bar);
                bar.SetManager(this);
            }
        }

        // 自動的に範囲を割り振る (線形分割)
        if (phaseBars.Count > 0)
        {
            float step = 1.0f / phaseBars.Count;
            int count = phaseBars.Count;
            for (int i = 0; i < count; i++)
            {
                // indexが大きいほど（Phase3など）、低いHP範囲を担当するようにする（右から左へ並んでいる前提）
                // 0: 0.66 - 1.00
                // 1: 0.33 - 0.66
                // 2: 0.00 - 0.33
                phaseBars[i].minRatio = (count - 1 - i) * step;
                phaseBars[i].maxRatio = (count - i) * step;
                Debug.Log($"[BossHPPhaseManager] Bar '{phaseBars[i].entity.name}' assigned: {phaseBars[i].minRatio:F2} - {phaseBars[i].maxRatio:F2}");
            }
        }
    }

    private void FindTarget()
    {
        Entity target = ecsGroup.FindEntity(targetEntityName);
        if (target != null)
        {
            targetHP = target.GetScript<HP>();
            if (targetHP != null)
            {
                Debug.Log($"[BossHPPhaseManager] Linked to target '{targetEntityName}'.");
            }
        }
    }

    public override void Update()
    {
        if (targetHP == null)
        {
            FindTarget();
            return;
        }

        float totalRatio = targetHP.CurrentHpRatio();
        
        foreach (var bar in phaseBars)
        {
            bar.UpdateRatio(totalRatio);
        }
    }

    // 外部（UI構築スクリプトなど）からバーを登録するための口を用意しておく
    public void RegisterPhaseBar(BossHPPhaseBar bar)
    {
        if (!phaseBars.Contains(bar))
        {
            phaseBars.Add(bar);
        }
    }
}

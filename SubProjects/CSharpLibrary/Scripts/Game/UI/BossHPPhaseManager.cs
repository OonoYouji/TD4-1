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
        FindTarget();

        // 子エンティティからBarスクリプトを収集 (名前で判定)
        phaseBars.Clear();
        uint childCount = entity.GetChildCount();

        List<Entity> children = new List<Entity>();
        for (uint i = 0; i < childCount; i++)
        {
            Entity child = entity.GetChild(i);
            if (child != null) 
            {
                children.Add(child);
            }
            else
            {
            }
        }

        // 名前順などでソートせず、シーンの構成順序に従う（または明示的に名前でソート）
        children.Sort((a, b) => string.Compare(a.name, b.name));

        foreach (var child in children)
        {
            var bar = child.GetScript<BossHPPhaseBar>();
            if (bar != null)
            {
                phaseBars.Add(bar);
                bar.SetManager(this);
            }
            else
            {
            }
        }

        // 自動的に範囲を割り振る (線形分割)
        if (phaseBars.Count > 0)
        {
            float step = 1.0f / phaseBars.Count;
            int count = phaseBars.Count;
            for (int i = 0; i < count; i++)
            {
                phaseBars[i].minRatio = (count - 1 - i) * step;
                phaseBars[i].maxRatio = (count - i) * step;
            }
        }
        else
        {
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


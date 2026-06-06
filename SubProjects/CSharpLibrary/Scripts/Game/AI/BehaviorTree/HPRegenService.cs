using System;

/// <summary>
/// エンティティのHPを一定量ずつ継続的に回復させるサービス。
/// ボス仕様書にある「HPは常に回復し続ける」を実装するために使用。
/// 
/// 拡張：上位フェーズへ逆戻りしないよう、回復の上限（割合）を指定可能にしました。
/// </summary>
public class HPRegenService : BehaviorService
{
    /// <summary>
    /// 現在のHPが格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string currentHPKey = "CurrentHP";

    /// <summary>
    /// 最大HP。回復の絶対上限。
    /// </summary>
    public float maxHP = 1200.0f;

    /// <summary>
    /// 1秒あたりの回復量。
    /// </summary>
    public float regenPerSecond = 5.0f;

    /// <summary>
    /// 回復可能な上限割合（0.0 ~ 1.0）。
    /// 例えば 0.7 を指定すると、HPが 70% を超えて回復することはない。
    /// </summary>
    public float maxRatio = 1.0f;

    private float _accumulatedHeal = 0.0f;

    /// <summary>
    /// 定期実行される回復処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        // 1. 物理的なHPコンポーネントを取得
        HP hpComp = owner.GetScript<HP>();
        if (hpComp == null) return;

        // 2. 回復限界値の計算 (最大HPか、指定された比率の小さい方)
        float limitHP = Math.Min(maxHP, maxHP * maxRatio);

        // 3. すでに限界を超えているなら何もしない
        if (hpComp.currentHp >= (int)limitHP)
        {
            _accumulatedHeal = 0; // 蓄積もリセット
            return;
        }

        // 4. 回復量を計算（端数を蓄積する）
        _accumulatedHeal += regenPerSecond * Interval;
        
        int amountInt = (int)_accumulatedHeal;

        if (amountInt >= 1)
        {
            int nextHp = hpComp.currentHp + amountInt;
            
            // 限界値を超えないようにクランプ
            if (nextHp > (int)limitHP)
            {
                nextHp = (int)limitHP;
            }

            hpComp.currentHp = nextHp;
                
            // 5. Blackboard の値を即座に同期
            uint hpKeyHash = BehaviorTreeLoader.HashString(currentHPKey);
            blackboard.SetFloat(hpKeyHash, (float)hpComp.currentHp);
            
            // 蓄積を消費
            _accumulatedHeal -= amountInt;
        }
    }
}

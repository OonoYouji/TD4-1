using System;

/// <summary>
/// エンティティのHPを一定量ずつ継続的に回復させるサービス。
/// ボス仕様書にある「HPは常に回復し続ける」を実装するために使用。
/// 回復量などのパラメータは Blackboard から読み取ることでフェーズごとの可変に対応。
/// </summary>
public class HPRegenService : BehaviorService
{
    /// <summary>
    /// 現在のHPが格納されているBlackboardキー。
    /// </summary>
    [BlackboardKey]
    public string currentHPKey = "CurrentHP";

    /// <summary>
    /// 最大HP。回復の上限。
    /// </summary>
    public float maxHP = 1000.0f;

    /// <summary>
    /// 1秒あたりの回復量。
    /// </summary>
    public float regenPerSecond = 5.0f;

    private float _accumulatedHeal = 0.0f;

    /// <summary>
    /// 定期実行される回復処理。
    /// </summary>
    public override void OnTick(Blackboard blackboard, Entity owner)
    {
        // 1. 物理的なHPコンポーネントを取得
        HP hpComp = owner.GetScript<HP>();
        if (hpComp == null) return;

        // 2. 回復量を計算（端数を蓄積する）
        _accumulatedHeal += regenPerSecond * Interval;
        
        int amountInt = (int)_accumulatedHeal;

        if (amountInt >= 1)
        {
            if (hpComp.currentHp < hpComp.MAX_HP)
            {
                hpComp.Heal(amountInt);
                
                // 3. Blackboard の値を即座に同期
                uint hpKeyHash = BehaviorTreeLoader.HashString(currentHPKey);
                blackboard.SetFloat(hpKeyHash, (float)hpComp.currentHp);
                
                // Debug.Log($"[HPRegen] {owner.name} healed for {amountInt}. Current: {hpComp.currentHp}/{hpComp.MAX_HP}");
            }
            
            // 消費した分を差し引く（端数は残す）
            _accumulatedHeal -= amountInt;
        }
    }
}

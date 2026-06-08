using System;
using System.Collections.Generic;

/// <summary>
/// ボスのフェーズ管理とフェーズごとのパラメータ設定を行うクラス。
/// </summary>
public class BossPhaseManager : MonoScript
{
    [Serializable]
    public class PhaseSettings
    {
        public int phaseIndex;
        public float hpThresholdRatio; // この割合以下になったらこのフェーズ
        public float regenPerSecond;
        public float movementSpeed;
        // 他のアクション用パラメータもここに追加可能
    }

    [SerializeField]
    public List<PhaseSettings> phases = new List<PhaseSettings>();

    private int currentPhaseIndex = -1;
    private HP hpComponent;
    private BossMovement movementComponent;

    public override void Initialize()
    {
        hpComponent = entity.GetScript<HP>();
        movementComponent = entity.GetScript<BossMovement>();
        
        if (hpComponent == null)
        {
            Debug.LogError("[BossPhaseManager] HP component not found!");
            return;
        }

        // デフォルト設定（仕様書に基づく例）
        if (phases.Count == 0)
        {
            phases.Add(new PhaseSettings { phaseIndex = 1, hpThresholdRatio = 1.0f, regenPerSecond = 5.0f, movementSpeed = 300.0f });
            phases.Add(new PhaseSettings { phaseIndex = 2, hpThresholdRatio = 0.7f, regenPerSecond = 5.0f, movementSpeed = 400.0f });
            phases.Add(new PhaseSettings { phaseIndex = 3, hpThresholdRatio = 0.4f, regenPerSecond = 10.0f, movementSpeed = 500.0f });
        }

        UpdatePhase();
    }

    public override void Update()
    {
        if (hpComponent == null) return;

        // HPが変化した時だけチェック
        if (hpComponent.HasHPChanged())
        {
            UpdatePhase();
        }
    }

    private void UpdatePhase()
    {
        float ratio = hpComponent.CurrentHpRatio();
        int newPhaseIndex = 1;

        // 閾値をチェックして現在のフェーズを決定
        // リストは threshold の降順で入っている前提
        foreach (var phase in phases)
        {
            if (ratio <= phase.hpThresholdRatio)
            {
                newPhaseIndex = phase.phaseIndex;
            }
        }

        if (newPhaseIndex != currentPhaseIndex)
        {
            currentPhaseIndex = newPhaseIndex;
            OnPhaseChanged(currentPhaseIndex);
        }
    }

    private void OnPhaseChanged(int newPhase)
    {
        Debug.Log($"[BossPhaseManager] Phase Changed to: {newPhase} (HP Ratio: {hpComponent.CurrentHpRatio():F2})");
        
        // フェーズごとのパラメータ適用
        var settings = phases.Find(p => p.phaseIndex == newPhase);
        if (settings != null)
        {
            hpComponent.regenPerSecond = settings.regenPerSecond;
            
            if (movementComponent != null)
            {
                movementComponent.SetSpeed(settings.movementSpeed);
            }
        }

        // TODO: BTの変数（Blackboardなど）を更新する処理が必要
    }

    public int GetCurrentPhase()
    {
        return currentPhaseIndex;
    }
}

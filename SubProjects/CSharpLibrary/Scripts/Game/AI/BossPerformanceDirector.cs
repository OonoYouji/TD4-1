using System;
using System.Collections.Generic;

/// <summary>
/// ボスの演出（開幕、フェーズ遷移）を管理するスクリプト。
/// HP割合を監視し、特定の閾値を越えた際に演出用プレハブを生成し、AIを一時停止します。
/// </summary>
public class BossPerformanceDirector : MonoScript
{
    [Serializable]
    public class PhasePerformance
    {
        public float hpThreshold;
        public string prefabPath;
        internal bool triggered = false; // 実行済みフラグ（保存しない）
    }

    [SerializeField] public List<PhasePerformance> performances = new List<PhasePerformance>();
    [SerializeField] public string deathPerformancePrefabPath = "";
    
    private HP _hp;
    private AgentIntentComponent _intent;
    private bool _isPerforming = false;
    private bool _isDead = false;
    private int _activePerformanceEntityId = -1;

    public override void Initialize()
    {
        _hp = entity.GetScript<HP>();
        _intent = entity.GetComponent<AgentIntentComponent>();
        
        // フラグを強制リセット
        foreach (var p in performances) p.triggered = false;

        // HPスクリプト側で自動消滅を無効化する
        if (_hp != null) _hp.disableAutoDestruction = true;
    }

    public override void Update()
    {
        if (_hp == null || _intent == null) return;

        // 演出中かどうかのチェック
        if (_isPerforming)
        {
            CheckPerformanceFinished();
            return;
        }

        if (_isDead) return;

        float currentRatio = _hp.CurrentHpRatio();

        // 撃破判定
        if (currentRatio <= 0 && !string.IsNullOrEmpty(deathPerformancePrefabPath))
        {
            TriggerDeathPerformance();
            return;
        }

        // 開幕・フェーズ遷移判定
        foreach (var p in performances)
        {
            if (!p.triggered && currentRatio <= p.hpThreshold)
            {
                TriggerPerformance(p);
                break; 
            }
        }
    }

    private void TriggerPerformance(PhasePerformance p)
    {
        if (string.IsNullOrEmpty(p.prefabPath))
        {
            p.triggered = true;
            return;
        }

        p.triggered = true;
        _isPerforming = true;
        _intent.isPaused = true;

        Entity perfEntity = ecsGroup.CreateEntity(p.prefabPath);
        if (perfEntity != null)
        {
            _activePerformanceEntityId = perfEntity.Id;
            
            // 位置と向きをボスに完全一致させる
            perfEntity.transform.position = transform.position;
            perfEntity.transform.rotation = transform.rotation;
        }
        else
        {
            EndPerformance();
        }
    }

    private void TriggerDeathPerformance()
    {
        _isDead = true;
        _isPerforming = true;
        _intent.isPaused = true;

        Entity perfEntity = ecsGroup.CreateEntity(deathPerformancePrefabPath);
        if (perfEntity != null)
        {
            _activePerformanceEntityId = perfEntity.Id;
            perfEntity.transform.position = transform.position;
            perfEntity.transform.rotation = transform.rotation;
        }
        else
        {
            EndPerformance();
        }
    }

    private void CheckPerformanceFinished()
    {
        bool finished = false;
        if (_activePerformanceEntityId != -1)
        {
            Entity perf = ecsGroup.GetEntity(_activePerformanceEntityId);
            if (perf == null) finished = true;
        }
        else
        {
            finished = true;
        }

        if (finished)
        {
            EndPerformance();
        }
    }

    private void EndPerformance()
    {
        _isPerforming = false;
        _activePerformanceEntityId = -1;

        if (_isDead)
        {
            entity.Destroy();
        }
        else
        {
            _intent.isPaused = false;
        }
    }
}

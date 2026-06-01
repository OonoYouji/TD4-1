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
        public bool triggered = false;
    }

    [SerializeField] public List<PhasePerformance> performances = new List<PhasePerformance>();
    
    private HP _hp;
    private AgentIntentComponent _intent;
    private bool _isPerforming = false;
    private int _activePerformanceEntityId = -1;

    public override void Initialize()
    {
        _hp = entity.GetScript<HP>();
        _intent = entity.GetComponent<AgentIntentComponent>();
        
        // 開幕演出があるかチェック（HP 1.0以上で設定されているものを探す）
        Update(); 
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

        float currentRatio = _hp.CurrentHpRatio();

        foreach (var p in performances)
        {
            if (!p.triggered && currentRatio <= p.hpThreshold)
            {
                TriggerPerformance(p);
                break; // 同時に複数は発生させない
            }
        }
    }

    private void TriggerPerformance(PhasePerformance p)
    {
        Debug.Log($"<color=gold>[BossPerformanceDirector]</color> Triggering performance for threshold {p.hpThreshold} (Prefab: {p.prefabPath})");
        
        p.triggered = true;
        _isPerforming = true;
        _intent.isPaused = true;

        // 演出用プレハブの生成
        Entity perfEntity = ecsGroup.CreateEntity(p.prefabPath);
        if (perfEntity != null)
        {
            _activePerformanceEntityId = perfEntity.Id;
            
            // 演出側にオーナー（ボス）の情報を伝える仕組み
            // プレハブ内のスクリプトが SetBossId などのメソッドを持っていることを期待
        }
        else
        {
            Debug.LogError($"[BossPerformanceDirector] Failed to spawn performance prefab: {p.prefabPath}");
            EndPerformance();
        }
    }

    private void CheckPerformanceFinished()
    {
        // 演出用エンティティが消滅した、または完了通知キーが立ったら終了とみなす
        bool finished = false;

        if (_activePerformanceEntityId != -1)
        {
            Entity perf = ecsGroup.GetEntity(_activePerformanceEntityId);
            if (perf == null)
            {
                finished = true;
            }
        }
        else
        {
            finished = true;
        }

        // Blackboard経由の明示的な通知もチェック（オプション）
        // string key = "PerformanceFinished_" + entity.Id;
        // if (BlackboardManager.Get(entity.Id)?.GetBool(key) == true) finished = true;

        if (finished)
        {
            EndPerformance();
        }
    }

    private void EndPerformance()
    {
        Debug.Log("<color=gold>[BossPerformanceDirector]</color> Performance finished. Resuming AI.");
        _isPerforming = false;
        _activePerformanceEntityId = -1;
        _intent.isPaused = false;
    }
}

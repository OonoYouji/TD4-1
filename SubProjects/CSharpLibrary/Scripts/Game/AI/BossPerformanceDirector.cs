using System;
using System.Collections.Generic;

/// <summary>
/// ボスの演出（開幕、フェーズ遷移）を管理するスクリプト。
/// HP割合を監視し、特定の閾値を越えた際に演出用プレハブを生成し、AIを一時停止します。
/// 演出終了後に、そのフェーズの行動を開始するようにHPやAIの状態を制御します。
/// </summary>
public class BossPerformanceDirector : MonoScript
{
    [Serializable]
    public class PhasePerformance
    {
        public string name = "New Phase";
        public float hpThreshold = 0.7f; // 0.7ならHP70%到達時に発動
        public string prefabPath = "";
        public float waitDuration = 2.0f; // 演出待機時間（秒）
        public bool isIntro = false; // 開幕演出（HP100%超）として扱うか
        internal bool triggered = false;
    }

    [SerializeField] public List<PhasePerformance> performances = new List<PhasePerformance>();
    [SerializeField] public string deathPerformancePrefabPath = "";
    [SerializeField] public float deathWaitDuration = 3.0f;
    
    private HP _hp;
    private AgentIntentComponent _intent;
    private bool _isPerforming = false;
    private bool _isDead = false;
    private int _activePerformanceEntityId = -1;
    private float _performanceTimer = 0.0f;
    private PhasePerformance _currentActivePerformance = null;

    public override void Initialize()
    {
        _hp = entity.GetScript<HP>();
        _intent = entity.GetComponent<AgentIntentComponent>();
        
        // フラグを強制リセット
        foreach (var p in performances) p.triggered = false;

        // HPスクリプト側で自動消滅を無効化する（演出完了後に破棄するため）
        if (_hp != null) _hp.disableAutoDestruction = true;
        
        _isPerforming = false;
        _isDead = false;
    }

    public override void Update()
    {
        if (_hp == null || _intent == null) return;

        // 演出中かどうかのチェック
        if (_isPerforming)
        {
            _performanceTimer -= Time.deltaTime;
            
            // 時間経過、かつ（もしあれば）プレハブの消滅を確認
            bool timerFinished = _performanceTimer <= 0;
            bool prefabFinished = true;

            if (_activePerformanceEntityId != -1)
            {
                Entity perf = ecsGroup.GetEntity(_activePerformanceEntityId);
                if (perf == null) prefabFinished = true;
                else prefabFinished = false;
            }

            if (timerFinished && prefabFinished)
            {
                EndPerformance();
            }
            return;
        }

        if (_isDead) return;

        float currentRatio = _hp.CurrentHpRatio();
        
        // 特殊ケース：現在のHPが100%を超えている（110%など）場合は、Intro演出を探す
        bool isExceedingMax = _hp.currentHp > _hp.MAX_HP;

        // 撃破判定
        if (currentRatio <= 0)
        {
            TriggerDeathPerformance();
            return;
        }

        // 開幕・フェーズ遷移判定
        foreach (var p in performances)
        {
            if (p.triggered) continue;

            bool shouldTrigger = false;
            if (p.isIntro && isExceedingMax) shouldTrigger = true;
            else if (!p.isIntro && currentRatio <= p.hpThreshold) shouldTrigger = true;

            if (shouldTrigger)
            {
                TriggerPerformance(p);
                break; 
            }
        }
    }

    private void TriggerPerformance(PhasePerformance p)
    {
        p.triggered = true;
        _currentActivePerformance = p;
        _isPerforming = true;
        _performanceTimer = p.waitDuration;
        
        // AIを一時停止 & 無敵化
        _intent.isPaused = true;
        _intent.desiredMoveDirection = Vector3.zero;
        if (_hp != null) _hp.isInvincible = true;

        // アニメーション再生
        var animator = entity.GetComponent<Animator>();
        if (animator != null) {
            animator.CrossFade("idle", 0.2f);
        }

//         Debug.Log($"<color=yellow>[Performance]</color> START: {p.name}. Waiting {p.waitDuration}s...");

        if (!string.IsNullOrEmpty(p.prefabPath))
        {
            Entity perfEntity = ecsGroup.CreateEntity(p.prefabPath);
            if (perfEntity != null)
            {
                _activePerformanceEntityId = perfEntity.Id;
                perfEntity.transform.position = transform.position;
                perfEntity.transform.rotation = transform.rotation;
            }
        }
    }

    private void TriggerDeathPerformance()
    {
        _isDead = true;
        _isPerforming = true;
        _performanceTimer = deathWaitDuration;
        _intent.isPaused = true;
        if (_hp != null) _hp.isInvincible = true;

        // アニメーション再生
        var animator = entity.GetComponent<Animator>();
        if (animator != null) {
            animator.CrossFade("idle", 0.2f);
        }

//         Debug.Log($"<color=red>[Performance]</color> START: Death. Waiting {deathWaitDuration}s...");

        if (!string.IsNullOrEmpty(deathPerformancePrefabPath))
        {
            Entity perfEntity = ecsGroup.CreateEntity(deathPerformancePrefabPath);
            if (perfEntity != null)
            {
                _activePerformanceEntityId = perfEntity.Id;
                perfEntity.transform.position = transform.position;
                perfEntity.transform.rotation = transform.rotation;
            }
        }
    }

    private void EndPerformance()
    {
        _isPerforming = false;
        _activePerformanceEntityId = -1;

        if (_isDead)
        {
//             Debug.Log("[Performance] END: Death. Destroying boss.");
            entity.Destroy();
        }
        else
        {
//             Debug.Log($"[Performance] END: {(_currentActivePerformance != null ? _currentActivePerformance.name : "Transition")}. Resuming AI.");
            
            // 開幕演出(Intro)が終わったなら、HPを100%に下げて戦闘開始
            if (_currentActivePerformance != null && _currentActivePerformance.isIntro)
            {
                if (_hp != null) _hp.currentHp = _hp.MAX_HP;
//                 Debug.Log("[Performance] Intro finished. Combat starts now (HP normalized to 100%).");
            }

            _intent.isPaused = false;
            if (_hp != null) _hp.isInvincible = false;
            _currentActivePerformance = null;
        }
    }
}

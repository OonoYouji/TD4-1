using System;

/// <summary>
/// ゲーム全体の進行状態（フェーズ）を管理するスクリプト。
/// シーン全体の流れ、ボスの段階、クリア・ゲームオーバー判定を一括管理します。
/// </summary>
public class GameStateManager : MonoScript
{
    public enum GamePhase
    {
        Start,          // 開始演出・導入
        BossPhase1,     // ボス第1段階
        BossPhase2,     // ボス第2段階
        BossPhase3,     // ボス第3段階
        End             // ゲーム終了（クリア or ゲームオーバー）
    }

    public enum GameResult
    {
        None,
        Clear,
        GameOver
    }

    [SerializeField] public GamePhase currentPhase = GamePhase.Start;
    [SerializeField] public GameResult result = GameResult.None;
    [SerializeField] public string clearSceneName = "GameClear";
    [SerializeField] public string overSceneName = "GameOver";
    [SerializeField] public float sceneTransitionDelay = 3.0f; // 撃破演出後の待ち時間

    private Entity _player;
    private HP _playerHp;
    private Entity _boss;
    private HP _bossHp;
    private AgentIntentComponent _bossIntent;
    private float _endTimer = 0f;

    public override void Initialize()
    {
        _player = ecsGroup.FindEntity("Player");
        if (_player != null) _playerHp = _player.GetScript<HP>();

        _boss = ecsGroup.FindEntity("Boss");
        if (_boss != null)
        {
            _bossHp = _boss.GetScript<HP>();
            _bossIntent = _boss.GetComponent<AgentIntentComponent>();
        }

        currentPhase = GamePhase.Start;
        result = GameResult.None;
        _endTimer = 0f;
    }

    public override void Update()
    {
        // デバッグキー（強制終了）
        if (Input.TriggerKey(KeyCode.K)) { EndGame(GameResult.Clear); return; }
        if (Input.TriggerKey(KeyCode.L)) { EndGame(GameResult.GameOver); return; }

        if (currentPhase == GamePhase.End)
        {
            UpdateEndSequence();
            return;
        }

        UpdateHealthChecks();
        UpdatePhaseTransitions();
    }

    private void UpdateHealthChecks()
    {
        // ゲームオーバー判定
        if (_playerHp != null && _playerHp.currentHp <= 0)
        {
            EndGame(GameResult.GameOver);
            return;
        }

        // ゲームクリア判定
        if (_bossHp != null && _bossHp.currentHp <= 0)
        {
            EndGame(GameResult.Clear);
            return;
        }
    }

    private void UpdatePhaseTransitions()
    {
        if (_bossHp == null) return;

        float hpRatio = _bossHp.CurrentHpRatio();

        switch (currentPhase)
        {
            case GamePhase.Start:
                // ボスのAIが再開された（＝開始演出が終わった）らPhase1へ
                if (_bossIntent != null && !_bossIntent.isPaused)
                {
                    currentPhase = GamePhase.BossPhase1;
//                     Debug.Log("<color=gold>[GameStateManager]</color> Intro Finished. Battle Start!");
                }
                break;

            case GamePhase.BossPhase1:
                if (hpRatio < 0.7f)
                {
                    currentPhase = GamePhase.BossPhase2;
//                     Debug.Log("<color=cyan>[GameStateManager]</color> Transition to BossPhase2 (HP < 70%)");
                }
                break;

            case GamePhase.BossPhase2:
                if (hpRatio < 0.3f)
                {
                    currentPhase = GamePhase.BossPhase3;
//                     Debug.Log("<color=cyan>[GameStateManager]</color> Transition to BossPhase3 (HP < 30%)");
                }
                break;
        }
    }

    private void EndGame(GameResult gameResult)
    {
        if (currentPhase == GamePhase.End) return;

        currentPhase = GamePhase.End;
        result = gameResult;
        _endTimer = 0f;

        if (result == GameResult.Clear)
        {
//             Debug.Log("<color=green>[GameStateManager]</color> BOSS DEFEATED! Playing death sequence...");
        }
        else
        {
//             Debug.Log("<color=red>[GameStateManager]</color> PLAYER DIED...");
        }
    }

    private void UpdateEndSequence()
    {
        _endTimer += Time.deltaTime;

        // 撃破演出や暗転などの時間を待ってからシーン遷移
        if (_endTimer >= sceneTransitionDelay)
        {
            if (result == GameResult.Clear) Transition(clearSceneName);
            else Transition(overSceneName);
        }
    }

    private void Transition(string sceneName)
    {
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.TransitionTo(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}

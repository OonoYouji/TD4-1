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

    private Entity _player;
    private HP _playerHp;
    private Entity _boss;
    private HP _bossHp;

    public override void Initialize()
    {
        _player = ecsGroup.FindEntity("Player");
        if (_player != null) _playerHp = _player.GetScript<HP>();

        _boss = ecsGroup.FindEntity("Boss");
        if (_boss != null) _bossHp = _boss.GetScript<HP>();

        currentPhase = GamePhase.Start;
        result = GameResult.None;
    }

    public override void Update()
    {
        if (currentPhase == GamePhase.End) return;

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

        // ゲームクリア判定（ボス撃破）
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

        // 状態遷移の監視
        switch (currentPhase)
        {
            case GamePhase.Start:
                // 開幕演出等が終わったらPhase1へ（現在は即時遷移、必要なら演出終了待ちを入れる）
                currentPhase = GamePhase.BossPhase1;
                break;

            case GamePhase.BossPhase1:
                if (hpRatio < 0.7f)
                {
                    currentPhase = GamePhase.BossPhase2;
                    Debug.Log("<color=cyan>[GameStateManager]</color> Transition to BossPhase2");
                }
                break;

            case GamePhase.BossPhase2:
                if (hpRatio < 0.3f)
                {
                    currentPhase = GamePhase.BossPhase3;
                    Debug.Log("<color=cyan>[GameStateManager]</color> Transition to BossPhase3");
                }
                break;
        }
    }

    private void EndGame(GameResult gameResult)
    {
        currentPhase = GamePhase.End;
        result = gameResult;

        if (result == GameResult.Clear)
        {
            Debug.Log("<color=green>[GameStateManager]</color> MISSION COMPLETE! Transitioning to Clear Scene.");
            Transition(clearSceneName);
        }
        else if (result == GameResult.GameOver)
        {
            Debug.Log("<color=red>[GameStateManager]</color> MISSION FAILED... Transitioning to Game Over Scene.");
            Transition(overSceneName);
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

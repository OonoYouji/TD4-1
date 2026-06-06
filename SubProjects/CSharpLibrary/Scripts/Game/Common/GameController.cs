using System;
using System.Collections.Generic;

public class GameController : MonoScript {
    [SerializeField] public string clearSceneName = "GameClear";
    [SerializeField] public string overSceneName = "GameOver";

    private Entity player;
    private HP playerHp;
    private bool isGameFinished = false;

    // エディタ監視用 (Scene Statsに表示するために static にしておく)
    public static string currentStatus = "Initializing";
    public static string currentPhase = "N/A";

    public override void Initialize() {
        currentStatus = "Playing";
        isGameFinished = false;
        player = ecsGroup.FindEntity("Player");
        if (player != null) {
            playerHp = player.GetScript<HP>();
            if (playerHp == null) {
                Debug.LogWarning("GameController: HP script not found on Player entity!");
            }
        } else {
            Debug.LogWarning("GameController: Player entity not found!");
        }
    }

    public override void Update() {
        if (isGameFinished) {
            // シーン遷移後も少しの間 stats が見える可能性があるためステータスを保持
            return;
        }

        // ボスのフェーズを監視して同期
        UpdateBossPhaseInfo();

        // デバッグ用キー入力
        if (Input.TriggerKey(KeyCode.K)) {
            Debug.Log("[GameController] Debug Key K pressed - Triggering Game Clear");
            TriggerGameClear();
            return;
        }
        if (Input.TriggerKey(KeyCode.L)) {
            Debug.Log("[GameController] Debug Key L pressed - Triggering Game Over");
            TriggerGameOver();
            return;
        }

        // ゲームオーバー判定: プレイヤーのHPが0
        if (playerHp != null && playerHp.currentHp <= 0) {
            Debug.Log($"[GameController] Player HP is {playerHp.currentHp}. Triggering Game Over.");
            TriggerGameOver();
            return;
        }

        // ゲームクリア判定: ボスのHPが0
        if (IsBossDefeated()) {
            Debug.Log("[GameController] IsBossDefeated returned TRUE. Triggering Game Clear.");
            TriggerGameClear();
        }
    }

    private void UpdateBossPhaseInfo() {
        // "Boss" という名前のエンティティを探す
        Entity boss = ecsGroup.FindEntity("Boss");
        if (boss == null) {
            currentPhase = "No Boss";
            return;
        }

        HP hp = boss.GetScript<HP>();
        if (hp != null) {
            float ratio = hp.CurrentHpRatio();
            // 仕様書v2の閾値に基づいてフェーズを判定
            if (ratio > 1.0f) currentPhase = "Intro";
            else if (ratio >= 0.7f) currentPhase = "Phase 1";
            else if (ratio >= 0.4f) currentPhase = "Phase 2";
            else if (ratio > 0.0f) currentPhase = "Phase 3";
            else currentPhase = "Defeated";
        } else {
            currentPhase = "No HP Component";
        }
    }

    private bool IsBossDefeated() {
        Entity boss = ecsGroup.FindEntity("Boss");
        if (boss == null) return false;

        HP bossHp = boss.GetScript<HP>();
        if (bossHp != null) {
            return bossHp.currentHp <= 0;
        }
        return false;
    }

    private void TriggerGameClear() {
        isGameFinished = true;
        currentStatus = "Game Clear";
        currentPhase = "Defeated";
        Debug.Log("GameController: ALL ENEMIES DEFEATED! Game Clear!");
        Transition(clearSceneName);
    }

    private void TriggerGameOver() {
        isGameFinished = true;
        currentStatus = "Game Over";
        Debug.Log("GameController: PLAYER DIED! Game Over!");
        Transition(overSceneName);
    }

    private void Transition(string sceneName) {
        if (SceneTransition.Instance != null) {
            SceneTransition.Instance.TransitionTo(sceneName);
        } else {
            SceneManager.LoadScene(sceneName);
        }
    }
}

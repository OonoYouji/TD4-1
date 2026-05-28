using System;
using System.Collections.Generic;

public class GameController : MonoScript {
    [SerializeField] public string clearSceneName = "GameClear";
    [SerializeField] public string overSceneName = "GameOver";

    private Entity player;
    private HP playerHp;
    private bool isGameFinished = false;

    public override void Initialize() {
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
        if (isGameFinished) return;

        // デバッグ用キー入力
        if (Input.TriggerKey(KeyCode.K)) {
            Debug.Log("GameController: Debug Key K pressed - Triggering Game Clear");
            TriggerGameClear();
            return;
        }
        if (Input.TriggerKey(KeyCode.L)) {
            Debug.Log("GameController: Debug Key L pressed - Triggering Game Over");
            TriggerGameOver();
            return;
        }

        // ゲームオーバー判定: プレイヤーのHPが0
        if (playerHp != null && playerHp.currentHp <= 0) {
            TriggerGameOver();
            return;
        }

        // ゲームクリア判定: TestEnemyを持つエンティティが全滅
        if (IsAllEnemiesDefeated()) {
            TriggerGameClear();
        }
    }

    private bool IsAllEnemiesDefeated() {
        // 全エンティティの中から TestEnemy スクリプトを持っているものを探す
        foreach (var entity in ecsGroup.GetEntities()) {
            if (entity.GetScript<TestEnemy>() != null) {
                return false; // まだ敵がいる
            }
        }
        return true; // 敵がいない
    }

    private void TriggerGameClear() {
        isGameFinished = true;
        Debug.Log("GameController: ALL ENEMIES DEFEATED! Game Clear!");
        Transition(clearSceneName);
    }

    private void TriggerGameOver() {
        isGameFinished = true;
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

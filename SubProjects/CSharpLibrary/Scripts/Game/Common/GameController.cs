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

    private bool IsBossDefeated() {
        // "Boss" という名前のエンティティを探す
        Entity boss = ecsGroup.FindEntity("Boss");
        if (boss == null) {
            // Debug.Log("[GameController:IsBossDefeated] Boss entity NOT FOUND.");
            return false;
        }

        HP bossHp = boss.GetScript<HP>();
        if (bossHp != null) {
            if (bossHp.currentHp <= 0) {
                Debug.Log($"[GameController:IsBossDefeated] Boss found (ID:{boss.Id}), but HP is {bossHp.currentHp}!");
                return true;
            }
            // 正常にHPが残っている場合はここ
            return false;
        }

        // Debug.LogWarning($"[GameController:IsBossDefeated] Boss found (ID:{boss.Id}), but NO HP script attached!");
        return false;
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

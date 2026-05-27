using System;
using System.Collections.Generic;

public class TitleSceneController : MonoScript {
    [SerializeField] public string nextSceneName = "GameScene";

    public override void Initialize() {
    }

    public override void Update() {
        // スペースキーまたはゲームパッドのAボタンで遷移
        if (Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A)) {
            if (SceneTransition.Instance != null) {
                Debug.Log("TitleSceneController: Transitioning to " + nextSceneName + " via transition.");
                SceneTransition.Instance.TransitionTo(nextSceneName);
            } else {
                Debug.Log("TitleSceneController: Transitioning to " + nextSceneName + " immediately.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}

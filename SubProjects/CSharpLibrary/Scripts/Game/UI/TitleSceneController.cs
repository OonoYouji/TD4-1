using System;
using System.Collections.Generic;

public class TitleSceneController : MonoScript {
    [SerializeField] public string nextSceneName = "GameScene";

    public override void Initialize() {
    }

    public override void Update() {
        // スペースキーまたはゲームパッドのAボタンで遷移
        if (Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A)) {
            Debug.Log("TitleSceneController: Transitioning to " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}

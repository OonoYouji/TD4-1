using System;
using System.Collections.Generic;

public class EndSceneController : MonoScript {
    [SerializeField] public string nextSceneName = "TitleScene";

    public override void Initialize() {
    }

    public override void Update() {
        // スペースキーまたはゲームパッドのAボタンで遷移
        if (Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A)) {
            Debug.Log("EndSceneController: Transitioning to " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}

using System;
using System.Collections.Generic;

public class TitleSceneController : MonoScript {
    [SerializeField] public string nextSceneName = "GameScene";

    public override void Initialize() {
    }

    public override void Update() {
        bool spaceTriggered = Input.TriggerKey(KeyCode.Space);
        bool gamepadTriggered = Input.TriggerGamepad(Gamepad.A);

        if (spaceTriggered || gamepadTriggered) {
            
            if (SceneTransition.Instance != null) {
                SceneTransition.Instance.TransitionTo(nextSceneName);
            } else {
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}


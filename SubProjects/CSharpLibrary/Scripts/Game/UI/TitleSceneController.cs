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
//             Debug.Log(string.Format("TitleSceneController: Input detected! Space: {0}, GamepadA: {1}", spaceTriggered, gamepadTriggered));
            
            if (SceneTransition.Instance != null) {
//                 Debug.Log("TitleSceneController: Calling SceneTransition.Instance.TransitionTo with " + nextSceneName);
                SceneTransition.Instance.TransitionTo(nextSceneName);
            } else {
//                 Debug.LogError("TitleSceneController: SceneTransition.Instance is NULL!");
//                 Debug.Log("TitleSceneController: Falling back to immediate LoadScene.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}

using System.Collections.Generic;

public class EndSceneController : MonoScript
{
    [SerializeField]
    public string nextSceneName = "TitleScene";

    public void SetSceneName(string nextSceneName_)
    {
        nextSceneName = nextSceneName_;
    }
       

    public override void Update()
    {

        // スペースキーまたはゲームパッドのAボタンで遷移
        if (Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A))
        {
            if (SceneTransition.Instance != null)
            {
                Debug.Log("EndSceneController: Transitioning to " + nextSceneName + " via transition.");
                SceneTransition.Instance.TransitionTo(nextSceneName);
            }
            else
            {
                Debug.Log("EndSceneController: Transitioning to " + nextSceneName + " immediately.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}

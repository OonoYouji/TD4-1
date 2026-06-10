public class EndSceneController : MonoScript
{
    [SerializeField] public string nextSceneName = "TitleScene";

    public override void Initialize()
    {
    }

    public override void Update()
    {
        // スペースキーまたはゲームパッドのAボタンで遷移
        if (Input.TriggerKey(KeyCode.Space) || Input.TriggerGamepad(Gamepad.A))
        {
            if (nextSceneName == "Null")
            {
                // TODO: ここにゲーム終了を書く
                Debug.LogInfo("EndSceneController: No next scene specified. Exiting game.");
            }
            else if (SceneTransition.Instance != null)
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

    public void SetNextSceneName(string nextSceneName_)
    {
        nextSceneName = nextSceneName_;
    }
}

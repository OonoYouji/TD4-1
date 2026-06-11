public class EndSceneController : MonoScript
{
    [SerializeField] public string nextSceneName = "TitleScene";
    [SerializeField] public string playSoundPath = "";

    AudioFadeoutAll audioFadeoutAll;

    public override void Initialize()
    {
        audioFadeoutAll = ecsGroup.FindEntity("AudioManager")?.GetScript<AudioFadeoutAll>();
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
                AudioSource audioSource = entity.GetComponent<AudioSource>();
                audioSource?.OneShotPlay(0.8f, 1.0f, playSoundPath);
                audioFadeoutAll?.StartFadeOut();
            }
            else
            {
                Debug.Log("EndSceneController: Transitioning to " + nextSceneName + " immediately.");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }

    public void SetNextSceneName(string nextSceneName_, string playSoundPath_)
    {
        nextSceneName = nextSceneName_;
        playSoundPath = playSoundPath_ ?? "";
    }
}

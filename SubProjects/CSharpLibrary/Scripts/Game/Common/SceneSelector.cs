using System.Collections.Generic;

class SceneSelector : MonoScript
{
    struct SceneInfo
    {
        public string sceneName;
        public string playSoundPath;
    }

    private readonly List<SceneInfo> nextSceneInfos = new List<SceneInfo>();
    private int nextSceneIndex = 0;

    string selectMoveSEPath = "./Assets/Sounds/MainGameSounds/se/outGame/cursor_move.mp3";
    string selectMoveFailedSEPath = "./Assets/Sounds/MainGameSounds/se/outGame/cursor_cantmove.mp3";

    private float timer = 0f;
    private readonly float inputDelay = 0.5f; // 入力を受け付けるまでの遅延時間
    private bool isPrevInputUp = false;

    AudioSource audioSource;

    EndSceneController controller = null;

    public override void Initialize()
    {
        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            nextSceneInfos.Add(new SceneInfo
            {
                sceneName = child.name,
                playSoundPath = child.GetScript<DummyStringData>()?.value
            });
        }

        controller = entity.GetScript<EndSceneController>();
        if (controller == null)
        {
            Debug.LogWarning("SceneSelector: EndSceneController script not found on the entity!");
        }

        audioSource = entity.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("SceneSelector: AudioSource script not found on the entity!");
        }
    }

    public override void Update()
    {

        if (nextSceneInfos.Count == 0)
        {
            return; // 遷移先のシーンがない場合は何もしない
        }

        if (
            Input.ReleaseKey(KeyCode.UpArrow) &&
            Input.ReleaseKey(KeyCode.W) &&
            Input.ReleaseGamepad(Gamepad.DPadUp) &&

            Input.ReleaseKey(KeyCode.DownArrow) &&
            Input.ReleaseKey(KeyCode.S) &&
            Input.ReleaseGamepad(Gamepad.DPadDown) &&

            Mathf.Abs(Input.GamepadThumb(GamepadAxis.LeftThumb).y) < 0.5f
            )
        {
            timer = 0.0f;
        }
        timer -= Time.deltaTime;
        Debug.LogInfo($"Timer: {timer}, isPrevInputUp: {isPrevInputUp}");

        // カーソル移動
        if (
            (!isPrevInputUp || timer <= 0) &&
            (Input.PressKey(KeyCode.UpArrow) ||
            Input.PressKey(KeyCode.W) ||
            Input.PressGamepad(Gamepad.DPadUp) ||
            Input.GamepadThumb(GamepadAxis.LeftThumb).y > 0.5f)
            )
        {
            isPrevInputUp = true;
            timer = inputDelay; // 入力遅延をリセット
            if (nextSceneIndex != 0)
            {
                nextSceneIndex = nextSceneIndex - 1;
                audioSource.OneShotPlay(0.6f, 1.0f, selectMoveSEPath);
            }
            else
            {
                audioSource.OneShotPlay(0.6f, 1.0f, selectMoveFailedSEPath);
            }
        }

        if (
            (isPrevInputUp || timer <= 0) &&
            (Input.PressKey(KeyCode.DownArrow) ||
             Input.PressGamepad(Gamepad.DPadDown) ||
             Input.PressKey(KeyCode.S) ||
             Input.GamepadThumb(GamepadAxis.LeftThumb).y < -0.5f)
            )
        {
            isPrevInputUp = false;
            timer = inputDelay; // 入力遅延をリセット
            if (nextSceneIndex != nextSceneInfos.Count - 1)
            {
                nextSceneIndex = nextSceneIndex + 1;
                audioSource.OneShotPlay(0.6f, 1.0f, selectMoveSEPath);
            }
            else
            {
                audioSource.OneShotPlay(0.6f, 1.0f, selectMoveFailedSEPath);
            }
        }

        controller.SetNextSceneName(nextSceneInfos[nextSceneIndex].sceneName, nextSceneInfos[nextSceneIndex].playSoundPath);
    }

    public int GetNextSceneIndex()
    {
        return nextSceneIndex;
    }
}

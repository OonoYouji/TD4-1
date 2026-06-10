using System.Collections.Generic;

class SceneSelector : MonoScript
{
    private readonly List<string> nextSceneName = new List<string>();
    private int nextSceneIndex = 0;

    private float timer = 0f;
    private readonly float inputDelay = 0.5f; // 入力を受け付けるまでの遅延時間
    private bool isPrevInputUp = false;

    EndSceneController controller = null;

    public override void Initialize()
    {
        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            nextSceneName.Add(child.name);
        }

        controller = entity.GetScript<EndSceneController>();
        if (controller == null)
        {
            Debug.LogWarning("SceneSelector: EndSceneController script not found on the entity!");
        }
    }

    public override void Update()
    {

        if (nextSceneName.Count == 0)
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

        // カーソル移動
        if (
            (!isPrevInputUp || timer <= 0) &&
            (Input.TriggerKey(KeyCode.UpArrow) ||
            Input.TriggerKey(KeyCode.UpArrow) ||
            Input.TriggerKey(KeyCode.W) ||
            Input.TriggerGamepad(Gamepad.DPadUp) ||
            Input.GamepadThumb(GamepadAxis.LeftThumb).y > 0.5f)
            )
        {
            isPrevInputUp = true;
            timer = inputDelay; // 入力遅延をリセット
            if (nextSceneIndex != 0)
            {
                nextSceneIndex = nextSceneIndex - 1;
            }
            else
            {

            }
        }

        if (
            (isPrevInputUp || timer <= 0) &&
            (Input.TriggerKey(KeyCode.DownArrow) ||
             Input.TriggerGamepad(Gamepad.DPadDown) ||
             Input.TriggerKey(KeyCode.S) ||
             Input.GamepadThumb(GamepadAxis.LeftThumb).y < -0.5f)
            )
        {
            isPrevInputUp = false;
            timer = inputDelay; // 入力遅延をリセット
            if (nextSceneIndex != nextSceneName.Count - 1)
            {
                nextSceneIndex = nextSceneIndex + 1;
            }
        }

        controller.SetNextSceneName(nextSceneName[nextSceneIndex]);
    }

    public int GetNextSceneIndex()
    {
        return nextSceneIndex;
    }
}

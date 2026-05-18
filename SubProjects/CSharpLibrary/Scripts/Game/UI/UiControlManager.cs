using System.Collections.Generic;

class UiControlManager : MonoScript
{
    Player playerScript;
    bool isGamepadMode = true;
    readonly List<UiController> uiControllers = new List<UiController>();

    [SerializeField]
    float uncontrolTimer = 0.0f;
    [SerializeField]
    float UNCONTROLED_THRESHOLD = 5.0f;

    public override void Initialize()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null)
        {
            Debug.LogError("Player entity not found in the ECS group.");
        }
        else
        {
            playerScript = player.GetScript<Player>();
            if (playerScript == null)
            {
                Debug.LogError("Player script not found on the Player entity.");
            }
        }

        // UiControllerを子の中から取得
        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            UiController uiController = child.GetScript<UiController>();
            if (uiController != null)
            {
                uiControllers.Add(uiController);
            }
        }

        uncontrolTimer = UNCONTROLED_THRESHOLD;
    }

    public override void Update()
    {
        // UIの描画をするかどうか
        // プレイヤーが一定時間操作していない場合はUIを表示する
        uncontrolTimer += Time.deltaTime;
        bool isPlayerUncontrolled = uncontrolTimer >= UNCONTROLED_THRESHOLD;
        Debug.LogInfo($"Uncontrol Timer: {uncontrolTimer}, Is Player Uncontrolled: {isPlayerUncontrolled}");
        foreach (UiController uiController in uiControllers)
        {
            uiController.entity.enable = isPlayerUncontrolled;
        }

        bool isCurrentGamepadMode = playerScript.isGamepadMode;
        if (isCurrentGamepadMode != isGamepadMode)
        {
            isGamepadMode = isCurrentGamepadMode;
            Debug.LogInfo($"Gamepad mode changed to: {isGamepadMode}");
            foreach (UiController uiController in uiControllers)
            {
                uiController.SetSpriteByMode(isGamepadMode);
            }
        }
    }

    public void ResetUncontrolTimer()
    {
        uncontrolTimer = 0.0f;
    }
}

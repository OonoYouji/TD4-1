using System.Collections.Generic;

class UiControlManager : MonoScript
{
    Player playerScript;
    bool isGamepadMode = true;
    readonly List<UiController> uiControllers = new List<UiController>();

    [SerializeField]
    readonly float UNCONTROLED_THRESHOLD = 5.0f;

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
    }

    public override void Update()
    {
        if (playerScript == null)
        {
            return; // Player scriptが見つからない場合は処理をスキップ
        }

        // UIの描画をするかどうか
        // プレイヤーが一定時間操作していない場合はUIを表示する
        bool isPlayerUncontrolled = playerScript.fireCooldownTimer >= UNCONTROLED_THRESHOLD;
        foreach (UiController uiController in uiControllers)
        {
            uiController.entity.enable = isPlayerUncontrolled;
        }

        // 入力モードによってUIを切り替え
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
}

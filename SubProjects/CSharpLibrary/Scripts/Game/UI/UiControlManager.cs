using System.Collections.Generic;

class UiControlManager : MonoScript
{
    Player playerScript;
    bool isGamepadMode = true;
    readonly List<UiController> uiControllers = new List<UiController>();

    public override void Initialize()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null)
        {
        }
        else
        {
            playerScript = player.GetScript<Player>();
            if (playerScript == null)
            {
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
            return;
        }

        // 入力モードによってUIを切り替え
        bool isCurrentGamepadMode = playerScript.isGamepadMode;
        if (isCurrentGamepadMode != isGamepadMode)
        {
            isGamepadMode = isCurrentGamepadMode;
            foreach (UiController uiController in uiControllers)
            {
                uiController.SetSpriteByMode(isGamepadMode);
            }
        }
    }
}


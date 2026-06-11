class UiController : MonoScript
{
    // gamepad
    [SerializeField]
    public string gamepadSpriteEntityName = "Gamepad";
    private Entity gamepadEntity;

    // keyboard and mouse
    [SerializeField]
    public string kbmSpriteEntityName = "KBM";
    private Entity kbmEntity;

    public override void Initialize()
    {
        gamepadEntity = TryFindChild(gamepadSpriteEntityName);
        if (gamepadEntity == null)
        {
        }

        kbmEntity = TryFindChild(kbmSpriteEntityName);
        if (kbmEntity != null)
        {
            kbmEntity.enable = false;
        }
        else
        {
        }
    }

    public void SetSpriteByMode(bool isGamepad)
    {
        if (gamepadEntity != null)
        {
            gamepadEntity.enable = isGamepad;
        }
        if (kbmEntity != null)
        {
            kbmEntity.enable = !isGamepad;
        }
    }

    public Entity TryFindChild(string name)
    {
        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            if (child != null)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
        }
        return null;
    }
}


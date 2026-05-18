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
        if (gamepadEntity != null)
        {
            SpriteRenderer renderer= gamepadEntity.GetComponent<SpriteRenderer>();
            if(renderer != null) {
                Vector2 textureSize = renderer.textureSize;
                gamepadEntity.transform.scale = new Vector3(textureSize.x, textureSize.y, 1);
            }
        }
        else
        {
            Debug.LogError("Gamepad sprite entity not found: " + gamepadSpriteEntityName);
        }

        kbmEntity = TryFindChild(kbmSpriteEntityName);
        if (kbmEntity != null)
        {
            SpriteRenderer renderer = kbmEntity.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Vector2 textureSize = renderer.textureSize;
                kbmEntity.transform.scale = new Vector3(textureSize.x, textureSize.y, 1);
            }
            kbmEntity.enable = false;
        }
        else
        {
            Debug.LogError("KBM sprite entity not found: " + kbmSpriteEntityName);
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

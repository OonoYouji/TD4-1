

class PlayerInputHandler : MonoScript
{

    [SerializeField]
    readonly float launchInterval = 0.5f;
    float launchTimer = 0.0f;

    ///
    /// Uターンを制御するため
    ///
    [SerializeField] readonly UTurnType uTurnType = UTurnType.Left;
    [SerializeField] public Mouse launchKey = Mouse.Left;

    PlayerBulletLauncher bulletLauncher;

    public override void Initialize()
    {
        Debug.Log("PlayerInputHandler Initializing");
        bulletLauncher = entity.GetScript<PlayerBulletLauncher>();
        if (bulletLauncher == null)
        {
            Debug.LogWarning("PlayerBulletLauncher script not found on the entity.");
        }

        Debug.Log("PlayerInputHandler initialized.");
    }

    public override void Update()
    {
        if (Input.PressMouse(launchKey))
        {
            launchTimer += Time.deltaTime;
        }

        if (launchTimer >= launchInterval)
        {
            if (bulletLauncher != null)
            {
                bulletLauncher.FireBullet(uTurnType);
            }
            launchTimer = 0.0f;
        }
    }
}
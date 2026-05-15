class UiGauge : MonoScript
{
    [SerializeField]
    public float width = 1500;
    [SerializeField]
    public float height = 70;

    private SpriteRenderer renderer;
    private PlayerCore core;

    public override void Initialize()
    {
        renderer = entity.GetComponent<SpriteRenderer>();
        if(renderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on the UiGauge entity.");
        }

        Entity playerCore = ecsGroup.FindEntity("PlayerCore");
        if(playerCore == null)
        {
            Debug.LogError("PlayerCore entity not found in the ECS group.");
            return;
        }
        core = playerCore.GetScript<PlayerCore>();
        if(core == null)
        {
            Debug.LogError("PlayerCore component not found on the PlayerCore entity.");
            return;
        }
    }

    public override void Update()
    {
        if(core == null)
        {
            return;
        }

        float hpRatio = core.CurrentHpRatio();

        transform.scale = new Vector3(width * hpRatio, height, 1);
        transform.position.x = -width * (1 - hpRatio) / 2;
    }
}

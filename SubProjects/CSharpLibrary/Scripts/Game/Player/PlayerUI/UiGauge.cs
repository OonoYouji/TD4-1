class UiGauge : MonoScript
{
    [SerializeField]
    public float width = 1500;
    [SerializeField]
    public float height = 70;

    public float defaultX = 0;

    private SpriteRenderer renderer;
    private HP playerHp;

    public override void Initialize()
    {
        renderer = entity.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on the UiGauge entity.");
        }

        Entity playerCore = ecsGroup.FindEntity("Player");
        if (playerCore == null)
        {
            Debug.LogError("Player entity not found in the ECS group.");
            return;
        }
        playerHp = playerCore.GetScript<HP>();
        if (playerHp == null)
        {
            Debug.LogError("HP component not found on the Player entity.");
            return;
        }
        defaultX = transform.position.x;
        Debug.LogInfo($"defaultX: {defaultX}");
    }

    public override void Update()
    {
        if (playerHp == null)
        {
            return;
        }

        float hpRatio = playerHp.CurrentHpRatio();

        transform.scale = new Vector3(width * hpRatio, height, 1);
        transform.position.x = Mathf.Lerp(defaultX, -width / 2 + defaultX, 1 - hpRatio);
    }
}

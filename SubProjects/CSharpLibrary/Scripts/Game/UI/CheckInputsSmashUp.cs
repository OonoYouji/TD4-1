class CheckInputsSmashUp : MonoScript
{
    [SerializeField]
    readonly float UNCONTROLED_THRESHOLD = 5.0f;

    GameController controller;
    PlayerSmashUp smashUp;

    public override void Initialize()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null)
        {
        }
        else
        {
            smashUp = player.GetScript<PlayerSmashUp>();
            if (smashUp == null)
            {
            }
        }

        Entity controllerEntity = ecsGroup.FindEntity("GameController");
        if (controllerEntity == null)
        {
        }
        else
        {
            controller = controllerEntity.GetScript<GameController>();
            if (controller == null)
            {
            }
        }
    }

    public override void Update()
    {
        // UIの描画をするかどうか
        // プレイヤーが一定時間操作していない場合はUIを表示する
        bool isPlayerUncontrolled =
            smashUp.smashTimer_ >= UNCONTROLED_THRESHOLD && controller.IsP2or3();

        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = isPlayerUncontrolled ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
            }
        }
    }
}

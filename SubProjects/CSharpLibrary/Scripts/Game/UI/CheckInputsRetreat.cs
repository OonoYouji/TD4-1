class CheckInputsRetreat : MonoScript
{
    [SerializeField]
    readonly float UNCONTROLED_THRESHOLD = 5.0f;

    CallingReinforcement callingReinforcement;

    public override void Initialize()
    {
        Entity player = ecsGroup.FindEntity("Player");
        if (player == null)
        {
        }
        else
        {
            callingReinforcement = player.GetScript<CallingReinforcement>();
            if (callingReinforcement == null)
            {
            }
        }
    }

    public override void Update()
    {
        // UIの描画をするかどうか
        // プレイヤーが一定時間操作していない場合はUIを表示する
        bool isPlayerUncontrolled = callingReinforcement.retreatTimer >= UNCONTROLED_THRESHOLD;
        if (!isPlayerUncontrolled)
        {
            return;
        }

        Debug.LogInfo($"Player retreat timer: {callingReinforcement.retreatTimer}, isPlayerUncontrolled: {isPlayerUncontrolled}");

        for (uint i = 0; i < entity.GetChildCount(); i++)
        {
            Entity child = entity.GetChild(i);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.enable = isPlayerUncontrolled ? 1 : 0;
            }
        }
    }
}

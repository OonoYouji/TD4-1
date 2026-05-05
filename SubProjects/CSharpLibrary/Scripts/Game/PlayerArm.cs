public class PlayerArm : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 左腕かどうか
    [SerializeField] public bool isLeft = true;


    // =========================================================
    // 内部状態
    // =========================================================

    private Player player;


    // =========================================================
    // プロパティ
    // =========================================================

    public bool IsLeft => isLeft;


    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        // プレイヤーエンティティにペアレント
        Entity playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity == null)
        {
            Debug.LogError("PlayerArm: Playerエンティティが見つかりません");
            return;
        }
        entity.parent = playerEntity;

        // Playerスクリプトを取得
        player = playerEntity.GetScript<Player>();
        if (player == null)
        {
            Debug.LogError("PlayerArm: PlayerスクリプトがPlayerエンティティに見つかりません");
        }
    }

    public override void Update()
    {
        if (player == null) { return; }

        float sign = 1.0f;
        if (isLeft)
        {
            sign = -1.0f;
        }

        // X軸スケールを腕の長さに合わせる
        Vector3 scale = transform.scale;
        scale.x = player.armLength * sign;
        transform.scale = scale;
    }

}

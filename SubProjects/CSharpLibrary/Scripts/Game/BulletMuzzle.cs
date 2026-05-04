public class BulletMuzzle : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 左腕の発射口かどうか
    [SerializeField] public bool isLeft = true;

    [SerializeField] public Vector3 offsetPos = new Vector3();


    // =========================================================
    // 内部状態
    // =========================================================

    private Player player;

    // PlayerLeftHand.objのX軸方向の自然長
    private const float modelNaturalLength = 2.249086f;


    // =========================================================
    // プロパティ
    // =========================================================

    public bool IsLeft => isLeft;

    // 発射口のワールド座標
    public Vector3 FirePosition => transform.worldPosition;


    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        // プレイヤーエンティティにペアレント
        Entity playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity == null)
        {
            Debug.LogError("BulletMuzzle: Playerエンティティが見つかりません");
            return;
        }
        entity.parent = playerEntity;

        // Playerスクリプトをキャッシュ
        player = playerEntity.GetScript<Player>();
        if (player == null)
        {
            Debug.LogError("BulletMuzzle: PlayerスクリプトがPlayerエンティティに見つかりません");
        }
    }

    public override void Update()
    {
        if (player == null) { return; }

        // プレイヤーのローカルX軸上の腕の先端に配置
        float sign = 1.0f;
        if (isLeft)
        {
            sign = -1.0f;
        }

        Vector3 pos = transform.position;
        pos.x = sign * player.armLength * modelNaturalLength;
        transform.position = pos + offsetPos;
    }

}

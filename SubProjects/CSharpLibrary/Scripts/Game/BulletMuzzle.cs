public class BulletMuzzle : MonoScript
{

    // =========================================================
    // パラメーター
    // =========================================================

    // 左腕の発射口かどうか
    [SerializeField] public bool isLeft = true;

    // オフセット位置
    [SerializeField] public Vector3 offsetPos = new Vector3();


    // =========================================================
    // 内部状態
    // =========================================================

    private Player player;
    private PlayerBulletLauncher launcher;

    // PlayerLeftHand.objのX軸方向の自然長
    private const float modelNaturalLength = 2.249086f;


    // =========================================================
    // プロパティ
    // =========================================================

    public bool IsLeft => isLeft;

    // 発射口のワールド座標
    public Vector3 FirePosition
    {
        get
        {
            if (player == null) { return transform.position; }
            Matrix4x4 rotMat = Matrix4x4.Rotate(player.transform.rotate);
            return player.transform.position + Matrix4x4.Transform(transform.position, rotMat);
        }
    }


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

        // PlayerBulletLauncher を取得
        launcher = entity.GetScript<PlayerBulletLauncher>();
        if (launcher == null)
        {
            launcher = entity.AddScript<PlayerBulletLauncher>();
        }
        launcher.enable = false;
    }

    public override void Update()
    {
        if (player == null)
        {
            return;
        }

        // プレイヤーのローカルX軸上の腕の先端に配置
        float sign = 1.0f;
        if (isLeft)
        {
            sign = -1.0f;
        }
        // プレイヤーの腕の長さに応じて発射口の位置を調整
        Vector3 pos = transform.position;
        pos.x = sign * player.armLength * modelNaturalLength;
        transform.position = pos + offsetPos;

        // 発射方向をプレイヤーの前方に更新
        Matrix4x4 rotMat = Matrix4x4.Rotate(player.transform.rotate);
        launcher.launchDirection = Matrix4x4.Transform(Vector3.forward, rotMat);
    }

    // =========================================================
    // 発射
    // =========================================================

    public void Fire()
    {
        var bullet = ecsGroup.CreateEntity("PlayerBullet");
        bullet.transform.position = FirePosition;

        PlayerBullet bs = bullet.GetScript<PlayerBullet>();
        if (bs != null)
        {
            bs.velocity = launcher.launchDirection.Normalized() * launcher.bulletSpeed;
            bs.uTurnType = UTurnType.Right;
            if (isLeft) { 
                bs.uTurnType = UTurnType.Left; 
            }
        }
    }

}

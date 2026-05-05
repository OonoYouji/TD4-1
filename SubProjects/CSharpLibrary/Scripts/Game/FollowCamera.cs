using System;

public class FollowCamera : MonoScript
{

    // =========================================================
    // パラメーター 
    // =========================================================

    // カメラのオフセット位置
    [SerializeField] public Vector3 offsetPos = new Vector3();

    // =========================================================
    // 内部状態
    // =========================================================

    private Entity playerEntity;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        playerEntity = ecsGroup.FindEntity("Player");

    }

    public override void Update()
    {
        if (playerEntity == null) { return; }

        // プレイヤーの位置取得
        Vector3 playerPos = playerEntity.transform.position;

        // プレイヤーの真上 + 後方オフセット
        Vector3 pos = transform.position;
        pos.x = playerPos.x + offsetPos.x;
        pos.y = playerPos.y + offsetPos.y;
        pos.z = playerPos.z + offsetPos.z;

        // transformの適応
        transform.position = pos;


    }
}

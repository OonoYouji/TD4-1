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

    private Entity        playerEntity;
    private PlayerSmashUp smashUp_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        playerEntity = ecsGroup.FindEntity("Player");
        if (playerEntity != null)
        {
            smashUp_ = playerEntity.GetScript<PlayerSmashUp>();
        }
    }

    public override void Update()
    {
        if (playerEntity == null) { return; }

        // プレイヤーの位置取得
        Vector3 playerPos = playerEntity.transform.position;

        Vector3 pos = transform.position;
        pos.x = playerPos.x + offsetPos.x;
        pos.z = playerPos.z + offsetPos.z;

        // SmashUp中はY追従を止める
        if (smashUp_ == null || !smashUp_.IsPlaying)
        {
            pos.y = playerPos.y + offsetPos.y;
        }

        transform.position = pos;
    }
}

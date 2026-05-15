using System;

public class EnemyBulletLauncher : MonoScript {

    // 発射口の位置オフセット
    [SerializeField]
	Vector3 offset = new Vector3(0, 0, 1);

    // 弾の速度
    [SerializeField]
	public float bulletSpeed = 10.0f;

	[SerializeField] int uTurnTypeInt = 0;
	UTurnType uTurnType => (UTurnType)uTurnTypeInt;

    // 発射方向
    public Vector3 launchDirection = new Vector3(0, 0, 1);

	public override void Initialize() {
	}

	public override void Update() {
		// 発射方向を自身の前方に更新
		Matrix4x4 rotMat = Matrix4x4.Rotate(transform.rotate);
		launchDirection = Matrix4x4.Transform(Vector3.forward, rotMat);
	}

	public void Fire() {
        FireBullet(uTurnType);
	}

	public void FireBullet(UTurnType type) {
        // 弾のエンティティを生成
        var bullet = ecsGroup.CreateEntity("EnemyBullet");
        // EnemyBulletスクリプトを取得して初期化
        EnemyBullet bs = bullet.GetScript<EnemyBullet>();
        if (bs != null) {
            bs.startPosition = new Vector3(transform.matrix.m30, transform.matrix.m31, transform.matrix.m32) + offset;
            bs.velocity = launchDirection.Normalized() * bulletSpeed;
            bs.uTurnType = type;
        }
	}
}

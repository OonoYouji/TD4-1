public class EnemyBulletLauncher : MonoScript {

	[SerializeField]
	Vector3 offset = new Vector3(0, 0, 1);

	[SerializeField]
	public float bulletSpeed = 10.0f;

	public Vector3 launchDirection = new Vector3(0, 0, 1);

	Entity target;

	public override void Initialize() {
		Debug.LogInfo("EnemyBulletLauncher Initializing on entity: " + entity.Id);
		target = ecsGroup.FindEntity("PlayerCore");
	}

	public override void Update() {
		if (target != null) {
			launchDirection = (target.transform.position - transform.position).Normalized();
		} else {
			Matrix4x4 rotMat = Matrix4x4.Rotate(transform.rotate);
			launchDirection = Matrix4x4.Transform(Vector3.forward, rotMat);
			target = ecsGroup.FindEntity("PlayerCore");
		}
	}

	public void Fire() {
		var bullet = ecsGroup.CreateEntity("EnemyBullet");
		EnemyBullet bs = bullet.GetScript<EnemyBullet>();
		if (bs != null) {
			bs.startPosition = new Vector3(transform.matrix.m30, transform.matrix.m31, transform.matrix.m32) + offset;
			bs.velocity = launchDirection.Normalized() * bulletSpeed;
		}
	}
}

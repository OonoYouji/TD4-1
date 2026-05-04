using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum UTurnType {
	Left, Right
}

public class PlayerBulletLauncher : MonoScript {


	///
	/// 基礎
	///

	[SerializeField]
	Vector3 offset = new Vector3(0, 0, 1);

	[SerializeField]
	public Vector3 launchDirection = new Vector3(0, 0, 1);

	[SerializeField]
	float launchInterval = 0.5f;
	float launchTimer = 0.0f;

	[SerializeField]
	float bulletSpeed = 10.0f;


	///
	/// Uターンを制御するため
	///
	[SerializeField] UTurnType uTurnType = UTurnType.Left;
	[SerializeField] public Mouse launchKey = Mouse.Left;


	public override void Initialize() {}

	public override void Update() {
		if (Input.PressMouse(launchKey)) {
			launchTimer += Time.deltaTime;
		}

		if (launchTimer >= launchInterval) {
			FireBullet(uTurnType);
			launchTimer = 0.0f;
		}
	}




	void FireBullet(UTurnType type) {
		var bullet = ecsGroup.CreateEntity("PlayerBullet");
		bullet.transform.position = transform.position + offset;

		PlayerBullet bs = bullet.GetScript<PlayerBullet>();
		bs.velocity = launchDirection.Normalized() * bulletSpeed;
		bs.uTurnType = type;
	}



}

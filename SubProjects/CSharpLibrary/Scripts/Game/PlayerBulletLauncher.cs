using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerBulletLauncher : MonoScript {

	[SerializeField]
	Vector3 offset = new Vector3(0, 0, 1);

	[SerializeField]
	public Vector3 launchDirection = new Vector3(0, 0, 1);

	[SerializeField]
	float launchInterval = 0.5f;
	float launchTimer = 0.0f;

	[SerializeField]
	float bulletSpeed = 10.0f;

	public override void Initialize() {

	}

	public override void Update() {
		if (Input.PressKey(KeyCode.Space)) {
			launchTimer += Time.deltaTime;
		}


		if (launchTimer >= launchInterval) {
			FireBullet();
			launchTimer = 0.0f;
		}
	}




	void FireBullet() {
		var bullet = ecsGroup.CreateEntity("PlayerBullet");
		bullet.transform.position = transform.position + offset;

		PlayerBullet bs = bullet.GetScript<PlayerBullet>();
		bs.velocity = launchDirection.Normalized() * bulletSpeed;
	}



}

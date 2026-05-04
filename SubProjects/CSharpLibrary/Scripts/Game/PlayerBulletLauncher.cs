using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerBulletLauncher : MonoScript {

	public override void Initialize() {
		Debug.Log("PlayerBulletLauncher Start");
	}

	public override void Update() {
		Debug.Log("PlayerBulletLauncher Update");
		if (Input.TriggerKey(KeyCode.Space)) {
			var bullet = ecsGroup.CreateEntity("PlayerBullet");
			bullet.transform.position = transform.position;
		}
	}

}

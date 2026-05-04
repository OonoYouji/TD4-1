using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PlayerBullet : MonoScript {

	[SerializeField]
	float speed = 0.1f;

	[SerializeField]
	float lifeTime = 5.0f;

	public override void Initialize() {
		Debug.Log("PlayerBullet Start");
	}

	public override void Update() {
		Debug.Log("PlayerBullet Update");
		transform.position += new Vector3(0, 0, speed);

		lifeTime -= Time.deltaTime;

		if (lifeTime <= 0) {
			entity.Destroy();
		}
	}

}

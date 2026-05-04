using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Player : MonoScript {

	float rotateY = 0f;

	public override void Initialize() {
		Debug.Log("Player Start");
	}

	public override void Update() {
		Debug.Log("Player Update");

		rotateY += 1.0f;

		transform.rotate = Quaternion.FromEuler(new Vector3(0, rotateY, 0));

	}

}

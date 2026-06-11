using System;

public class TargetRock : MonoScript {

	float gravityY = -9.8f;

	public override void Update() {
		Vector3 pos = transform.position;
		pos.y = gravityY * Time.deltaTime;
		transform.position = pos;
	}
}

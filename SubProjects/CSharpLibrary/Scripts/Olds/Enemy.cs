using System;

public class Enemy : MonoScript {

	float hp;
	bool isAlive;

	public override void Initialize() {
		hp = 100f;
		isAlive = true;
	}

	public override void Update() {

		if (hp <= 0f) {
			isAlive = false;
			return;
		}
	}


	public bool IsAlive { 
		get {
			return isAlive;
		}
	}

}


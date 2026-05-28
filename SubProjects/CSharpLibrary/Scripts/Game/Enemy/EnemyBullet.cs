public class EnemyBullet : MonoScript {

	public Vector3 startPosition = Vector3.zero;
	public Vector3 velocity = new Vector3(0, 0, 1);

	bool positionApplied = false;

	public override void Initialize() {
		positionApplied = false;
	}

	public override void Update() {
		if (!positionApplied) {
			transform.position = startPosition;
			positionApplied = true;
		}

		transform.position += velocity * Time.deltaTime;
	}
}

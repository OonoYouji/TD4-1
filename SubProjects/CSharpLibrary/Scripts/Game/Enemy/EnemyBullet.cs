public class EnemyBullet : MonoScript {

	public Vector3 startPosition = Vector3.zero;
	public Vector3 velocity = new Vector3(0, 0, 1);
	float lifeTime = 20.0f;

	bool positionApplied = false;

	public override void Initialize() {
		positionApplied = false;
		lifeTime = 20.0f;
	}

	public override void Update() {
		if (!positionApplied) {
			transform.position = startPosition;
			positionApplied = true;
		}

		transform.position += velocity * Time.deltaTime;

		lifeTime -= Time.deltaTime;
		if (lifeTime <= 0) {
			entity.Destroy();
		}
	}
}

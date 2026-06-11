
public class Breathe : MonoScript {

	[SerializeField] public Vector3 defaultScale = Vector3.one; // デフォルトのスケール
	Vector3 scale = Vector3.one;

	public override void Awake() {

	}

	public override void Initialize() {
		//defaultScale = transform.scale; // 初期スケールを保存
	}

	public override void Update() {

		Transform t = transform;
		scale = defaultScale * Mathf.Clamp(Mathf.Sin(Time.time) * 0.5f + 0.5f, 0.2f, 1f);
		t.scale = scale;
	}

}


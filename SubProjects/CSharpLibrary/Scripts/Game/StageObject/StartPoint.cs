using System;

public class StartPoint : MonoScript {

	bool isStarted = false;

	public override void Update() {
		if (!isStarted) {
			// シーン内にあるPlayerインスタンスを検索
			Entity playerEntity = ecsGroup.FindEntity("Player");
			if (playerEntity != null) {
				// 自身のpositionに配置
				playerEntity.transform.position = transform.position;
				Debug.Log("StartPoint: Player positioned at " + transform.position.ToString());
			} else {
				Debug.LogWarning("StartPoint: Player entity not found in scene.");
			}
			isStarted = true;
		}
	}
}

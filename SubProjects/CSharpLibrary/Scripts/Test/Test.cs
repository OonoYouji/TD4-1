using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Test : MonoScript {

	[SerializeField] Vector4 color = new Vector4(1, 0, 0, 1);
	[SerializeField] Vector3 copyPosition = Vector3.zero;

	public override void Initialize() {
		//MeshRenderer renderer = entity.AddComponent<MeshRenderer>();
		//renderer.color = new Vector4(1, 0, 1, 0);
	}

	public override void Update() {

		MeshRenderer renderer = entity.GetComponent<MeshRenderer>();
		if (renderer != null) {
			//if(renderer.postEffectFlags == 0) {
			//	color = new Vector4(1, 0, 0, 1);
			//} else {
			//	color = new Vector4(0, 1, 0, 1);
			//}

			//renderer.color = color;
			//color = renderer.color;
		}

		if (transform) {
			copyPosition = transform.position;
		}

	}

}

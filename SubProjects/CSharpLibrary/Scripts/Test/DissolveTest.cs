using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DissolveTest : MonoScript {

	[SerializeField] float threshold = 0.0f;

	public override void Initialize() {

	}

	public override void Update() {

		DissolveMeshRenderer renderer = entity.GetComponent<DissolveMeshRenderer>();
		if (renderer != null) {
			renderer.threshold = threshold;
		}

	}
}

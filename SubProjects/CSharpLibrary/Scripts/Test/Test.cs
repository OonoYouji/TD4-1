using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Test : MonoScript {

	[SerializeField] Vector3 copyPosition = Vector3.zero;

	public override void Initialize() {
		copyPosition = transform.position;
	}

}

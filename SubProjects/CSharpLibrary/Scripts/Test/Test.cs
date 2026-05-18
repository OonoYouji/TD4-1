using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Test : MonoScript {

	[SerializeField] Vector4 mat1;
	[SerializeField] Vector4 mat2;
	[SerializeField] Vector4 mat3;
	[SerializeField] Vector4 mat4;


	public override void Initialize() {

		Transform transform = entity.GetComponent<Transform>();
		Matrix4x4 matrix = transform.matrix;
		mat1 = matrix.GetRow(0);
		mat2 = matrix.GetRow(1);
		mat3 = matrix.GetRow(2);
		mat4 = matrix.GetRow(3);

	}

	public override void Update() {

	}

}

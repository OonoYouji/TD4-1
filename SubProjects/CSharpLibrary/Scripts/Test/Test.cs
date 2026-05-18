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

	[SerializeField] float fovY;
	[SerializeField] float nearClip;
	[SerializeField] float farClip;


	public override void Initialize() {

		CameraComponent camera = entity.GetComponent<CameraComponent>();
		Matrix4x4 matVP = camera.matVP;
		mat1 = matVP.GetRow(0);
		mat2 = matVP.GetRow(1);
		mat3 = matVP.GetRow(2);
		mat4 = matVP.GetRow(3);

		fovY = camera.fovY;
		nearClip = camera.nearClip;
		farClip = camera.farClip;
	}

	public override void Update() {
		//CameraComponent camera = entity.GetComponent<CameraComponent>();
		//Matrix4x4 matVP = camera.matVP;
		//mat1 = matVP.GetRow(0);
		//mat2 = matVP.GetRow(1);
		//mat3 = matVP.GetRow(2);
		//mat4 = matVP.GetRow(3);


		//fovY = camera.fovY;
		//nearClip = camera.nearClip;
		//farClip = camera.farClip;
	}

}

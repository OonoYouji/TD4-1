using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Test : MonoScript {

	[SerializeField] Vector4 color;
	[SerializeField] Vector2 textureSize;
	[SerializeField] Vector2 uvTransform_position;
	[SerializeField] float uvTransform_rotate;
	[SerializeField] Vector2 uvTransform_scale;

	public override void Initialize() {
		SpriteRenderer renderer = entity.GetComponent<SpriteRenderer>();
		color = renderer.color;
		textureSize = renderer.textureSize;
		uvTransform_position = renderer.uvTransform.offset;
		uvTransform_rotate = renderer.uvTransform.rotate;
		uvTransform_scale = renderer.uvTransform.scale;
	}

}

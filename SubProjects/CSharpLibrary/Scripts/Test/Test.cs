using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Test : MonoScript {

	[SerializeField] Vector4 color;
	[SerializeField] Vector2 textureSize;

	public override void Initialize() {
		SpriteRenderer renderer = entity.GetComponent<SpriteRenderer>();
		color = renderer.color;
		textureSize = renderer.textureSize;
	}

}

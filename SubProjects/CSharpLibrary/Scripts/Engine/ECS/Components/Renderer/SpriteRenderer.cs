using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class SpriteRenderer : Component {

	public struct BatchData {
		public uint compId;
		public Vector4 color;
		public Vector2 textureSize;
	}

	private Vector4 color_ = Vector4.one;
	public Vector4 color {
		get {
			return color_;
		}
		set {
			color_ = value;
		}
	}

	private Vector2 textureSize_ = Vector2.zero;
	public Vector2 textureSize {
		get {
			return textureSize_;
		}
		set {
			textureSize_ = value;
		}
	}
}

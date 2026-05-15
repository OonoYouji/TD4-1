using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public class DissolveMeshRenderer : Component {
	public struct BatchData {
		public uint compId;
		public float threshold;
		public UVTransform uvTransform;
	}

	private float threshold_ = 0.0f;
	public float threshold {
		get {
			return threshold_;
		}
		set {
			threshold_ = value;
		}
	}

	private UVTransform uvTransform_ = UVTransform.identity;
	public UVTransform uvTransform {
		get {
			return uvTransform_;
		}
		set {
			uvTransform_ = value;
		}
	}
}

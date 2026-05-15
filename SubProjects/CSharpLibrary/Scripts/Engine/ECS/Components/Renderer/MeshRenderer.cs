using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

class MeshRenderer : Component {
	public struct BatchData {
		public uint compId;
		public Vector4 color;
		public uint postEffectFlags;
		public UVTransform uvTransform;
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

	private uint postEffectFlags_ = 0;
	public uint postEffectFlags {
		get {
			return postEffectFlags_;
		}
		set {
			postEffectFlags_ = value;
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


	public string meshPath {
		get {
			return InternalGetMeshName(nativeHandle);
		}
		set {
			InternalSetMeshName(nativeHandle, value);
		}
	}


	/// -------------------------------------------
	/// internal methods
	/// -------------------------------------------

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern string InternalGetMeshName(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetMeshName(ulong _nativeHandle, string _meshName);

}


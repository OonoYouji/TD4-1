using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

public enum RenderQueue : uint {
	Background = 0,
	Telegraph  = 1,
	Default    = 2,
}

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
			return InternalGetColor(nativeHandle);
		}
		set {
			InternalSetColor(nativeHandle, value);
			color_ = value;
		}
	}

	private uint postEffectFlags_ = 0;
	public uint postEffectFlags {
		get {
			return InternalGetPostEffectFlags(nativeHandle);
		}
		set {
			InternalSetPostEffectFlags(nativeHandle, value);
			postEffectFlags_ = value;
		}
	}

	public RenderQueue renderQueue {
		get {
			return (RenderQueue)InternalGetRenderQueue(nativeHandle);
		}
		set {
			InternalSetRenderQueue(nativeHandle, (uint)value);
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

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern Vector4 InternalGetColor(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetColor(ulong _nativeHandle, Vector4 _color);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern uint InternalGetPostEffectFlags(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetPostEffectFlags(ulong _nativeHandle, uint _flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern uint InternalGetRenderQueue(ulong _nativeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetRenderQueue(ulong _nativeHandle, uint _queue);
}


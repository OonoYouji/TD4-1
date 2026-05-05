using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

class DissolveMeshRenderer : Component {
	public struct BatchData {
		public uint compId;
		public float threshold;
	}

	BatchData batchData;
	public BatchData GetBatchData() {
		return batchData;
	}


	//public string meshPath {
	//	get {
	//		return InternalGetMeshName(nativeHandle);
	//	}
	//	set {
	//		InternalSetMeshName(nativeHandle, value);
	//	}
	//}

	public float threshold {
		get {
			return batchData.threshold;
		}
		set {
			batchData.threshold = value;
		}
	}


	/// -------------------------------------------
	/// internal methods
	/// -------------------------------------------

	//[MethodImpl(MethodImplOptions.InternalCall)]
	//static extern string InternalGetMeshName(ulong _nativeHandle);

	//[MethodImpl(MethodImplOptions.InternalCall)]
	//static extern void InternalSetMeshName(ulong _nativeHandle, string _meshName);

}

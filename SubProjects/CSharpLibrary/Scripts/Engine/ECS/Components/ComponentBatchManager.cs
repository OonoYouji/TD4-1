using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// 変更点: Allocatorも配列全体(IComponentArray)を受け取るように変更
delegate Array ComponentBatchConverter(IComponentArray array);
delegate Array ComponentBatchAllocator(IComponentArray array);

static class ComponentBatchManager {
	private static Dictionary<Type, ComponentBatchConverter> converters = new Dictionary<Type, ComponentBatchConverter>();
	private static Dictionary<Type, ComponentBatchAllocator> allocators = new Dictionary<Type, ComponentBatchAllocator>();

	public static void Initialize() {

		// --- Transform の登録 ---

		// 送信用コンバータ
		RegisterConverter<Transform, Transform.BatchData>((ComponentArray<Transform> array) => {
			int count = array.Count;
			Transform.BatchData[] batch = new Transform.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				batch[i].compId = comp.compId;
				batch[i].position = comp.position;
				batch[i].rotate = comp.rotate;
				batch[i].scale = comp.scale;
			}
			return batch;
		});

		// 受信用アロケータ (変更点: IDを事前に埋める)
		RegisterAllocator<Transform, Transform.BatchData>((ComponentArray<Transform> array) => {
			int count = array.Count;
			Transform.BatchData[] batch = new Transform.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				batch[i].compId = comp.compId;
			}
			return batch;
		});


		// --- MeshRenderer の登録 ---

		// 送信用コンバータ
		RegisterConverter<MeshRenderer, MeshRenderer.BatchData>((ComponentArray<MeshRenderer> array) => {
			int count = array.Count;
			MeshRenderer.BatchData[] batch = new MeshRenderer.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				var batchData = comp.GetBatchData();

				// C++に何を送信するかログに出す
				Debug.LogError($"--- SEND BATCH for MeshRenderer[{comp.compId}]: color={batchData.color}");

				batch[i].compId = comp.compId;
				batch[i].color = batchData.color;
				batch[i].postEffectFlags = batchData.postEffectFlags;
			}
			return batch;
		});

		// 受信用アロケータ (変更点: Handleを事前に埋める)
		RegisterAllocator<MeshRenderer, MeshRenderer.BatchData>((ComponentArray<MeshRenderer> array) => {
			int count = array.Count;
			MeshRenderer.BatchData[] batch = new MeshRenderer.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				batch[i].compId = comp.compId;
			}
			return batch;
		});


		// --- DissolveMeshRenderer の登録 ---

		// 送信用コンバータ
		RegisterConverter<DissolveMeshRenderer, DissolveMeshRenderer.BatchData>((ComponentArray<DissolveMeshRenderer> array) => {
			int count = array.Count;
			DissolveMeshRenderer.BatchData[] batch = new DissolveMeshRenderer.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				var batchData = comp.GetBatchData();

				batch[i].compId = comp.compId;
				batch[i].threshold = batchData.threshold;
			}
			return batch;
		});

		// 受信用アロケータ (変更点: Handleを事前に埋める)
		RegisterAllocator<DissolveMeshRenderer, DissolveMeshRenderer.BatchData>((ComponentArray<DissolveMeshRenderer> array) => {
			int count = array.Count;
			DissolveMeshRenderer.BatchData[] batch = new DissolveMeshRenderer.BatchData[count];
			for (int i = 0; i < count; i++) {
				var comp = array.Get(i);
				batch[i].compId = comp.compId;
			}
			return batch;
		});


	}


	// Converter を登録
	public static void RegisterConverter<TComponent, TData>(Func<ComponentArray<TComponent>, TData[]> converter)
		where TComponent : Component {
		converters[typeof(TComponent)] = (IComponentArray array) => {
			return converter((ComponentArray<TComponent>)array);
		};
	}

	// Allocator を登録 (変更点: Func<int, ...> から Func<ComponentArray<...>, ...> へ変更)
	public static void RegisterAllocator<TComponent, TData>(Func<ComponentArray<TComponent>, TData[]> allocator)
		where TComponent : Component {
		allocators[typeof(TComponent)] = (IComponentArray array) => {
			return allocator((ComponentArray<TComponent>)array);
		};
	}


	// 一括送信
	public static void SendAllBatches(ComponentCollection _collection, string _ecsGroupName) {
		Debug.LogInfo("ComponentBatchManager.SendAllBatches: Start sending all batches.");
		// Debug.LogInfo($"ComponentBatchManager.SendAllBatches: Total converters registered: {converters.Count}.");

		foreach (var kv in converters) {
			if (!_collection.TryGetArray(kv.Key, out IComponentArray array)) {
				Debug.LogWarning($"ComponentBatchManager.SendAllBatches: ComponentArray for {kv.Key} not found.");
				continue;
			}

			// Debug.LogInfo($"ComponentBatchManager.SendAllBatches: Sending batch for {kv.Key}.");
			Array batch = kv.Value(array);
			InternalSetBatch(kv.Key, batch, batch.Length, _ecsGroupName);
		}
	}

	// 一括受信
	public static void ReceiveAllBatches(ComponentCollection _collection, string _ecsGroupName) {
		Debug.LogInfo($"ComponentBatchManager.ReceiveAllBatches: START for group: {_ecsGroupName}");
		foreach (var kv in allocators) {
			if (!_collection.TryGetArray(kv.Key, out IComponentArray array)) {
				// Debug.LogWarning($"ComponentBatchManager.ReceiveAllBatches: ComponentArray for {kv.Key} not found.");
				continue;
			}

			int count = array.Count;
			if (count == 0) {
				// Debug.LogWarning($"ComponentBatchManager.ReceiveAllBatches: No components to receive for {kv.Key}.");
				continue;
			}

			// 変更点: 配列そのもの(array)を渡して、ID設定済みのBatch配列を受け取る
			Array batch = kv.Value(array);

			Debug.LogInfo($"ComponentBatchManager.ReceiveAllBatches: Receiving batch for {kv.Key} with count {count}.");

			// batch内には既に compId/nativeHandle が入っているので、C++側で正しく処理可能
			InternalGetBatch(kv.Key, batch, count, _ecsGroupName);

			ApplyBatch(kv.Key, batch, array);
		}
		Debug.LogInfo($"ComponentBatchManager.ReceiveAllBatches: END for group: {_ecsGroupName}");
	}

	//
	// データの適用
	//
	static public void ApplyBatch(Type _componentType, Array _batch, IComponentArray _array) {
		if (_componentType == typeof(Transform)) {
			var array = (ComponentArray<Transform>)_array;
			var batch = (Transform.BatchData[])_batch;

			for (int i = 0; i < batch.Length; i++) {
				var comp = array.Get(i);

				// Debug.LogInfo($"--- RECEIVE BATCH for Transform[{comp.compId}]: pos={batch[i].position}");

				// 念のためIDの一致を確認することも可能だが、
				// Allocatorで順番通りに作成しているため、ここではそのまま適用する
				comp.position = batch[i].position;
				comp.rotate = batch[i].rotate;
				comp.scale = batch[i].scale;
				comp.matrix = batch[i].matrix;
			}
		}

		if (_componentType == typeof(MeshRenderer)) {
			var array = (ComponentArray<MeshRenderer>)_array;
			var batch = (MeshRenderer.BatchData[])_batch;

			for (int i = 0; i < batch.Length; i++) {
				var comp = array.Get(i);
				// C++から何を受け取ったかログに出す
				Debug.LogError($"--- RECEIVE BATCH for MeshRenderer[{comp.compId}]: color={batch[i].color}");
				// Handleは変更せず、描画パラメータのみ更新
				comp.color = batch[i].color;
				comp.postEffectFlags = batch[i].postEffectFlags;
			}
		}


		if (_componentType == typeof(DissolveMeshRenderer)) {
			var array = (ComponentArray<DissolveMeshRenderer>)_array;
			var batch = (DissolveMeshRenderer.BatchData[])_batch;
			for (int i = 0; i < batch.Length; i++) {
				var comp = array.Get(i);
				Debug.LogError($"--- RECEIVE BATCH for DissolveMeshRenderer[{comp.compId}]: threshold={batch[i].threshold}");
				// Handleは変更せず、描画パラメータのみ更新
				comp.threshold = batch[i].threshold;
			}
		}
	}


	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetBatch(Type _componentType, Array _batch, int _count, string _ecsGroupName);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalGetBatch(Type _componentType, Array batch_, int _count, string _ecsGroupName);
}
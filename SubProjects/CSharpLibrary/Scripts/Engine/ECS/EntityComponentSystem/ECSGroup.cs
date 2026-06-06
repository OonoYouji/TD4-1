using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

public class ECSGroup {
	///////////////////////////////////////////////////////////////////////////////////////////
	// objects
	///////////////////////////////////////////////////////////////////////////////////////////

	public string groupName;
	private bool enable_; //!< このGroupの有効/無効フラグ
	private Dictionary<int, Entity> entityMap_ = new Dictionary<int, Entity>();
	private List<Entity> entities_ = new List<Entity>();
	public ComponentCollection componentCollection = new ComponentCollection();

	/// 生成処理、初期化処理の呼び出し用リスト
	private List<Entity> awakeList_ = new List<Entity>();
	private List<Entity> initList_ = new List<Entity>();


	///////////////////////////////////////////////////////////////////////////////////////////
	// methods
	///////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// コンストラクタ
	/// </summary>
	/// <param name="_groupName"></param>
	public ECSGroup(string _groupName) {
		ComponentBatchManager.Initialize();
		groupName = _groupName;
		enable_ = true;

		// 主要なComponentArrayをあらかじめ確保しておく
		componentCollection.GetArray<Transform>();
		componentCollection.GetArray<MeshRenderer>();
		componentCollection.GetArray<DissolveMeshRenderer>();
		componentCollection.GetArray<AgentIntentComponent>();
		componentCollection.GetArray<CameraComponent>();
		componentCollection.GetArray<Animator>();
	}

	// ==============================================
	// エンティティの生成
	// ==============================================

	/// <summary>
	/// c/c++側から呼び出すエンティティの追加関数
	/// </summary>
	public void AddEntity(int _id) {
		//Debug.LogInfo("ECSGroup.AddEntity - Adding entity with ID: " + _id + ", Group Name: " + groupName);
		if(entityMap_.ContainsKey(_id)) {
			//Debug.LogError("ECSGroup.AddEntity - Entity already exists with ID: " + _id + ", Group Name: " + groupName);
			return;
		}


		Entity entity = new Entity(_id, this);
		entityMap_.Add(_id, entity);
		entities_.Add(entity);

		// C++側のデータを同期するために主要なコンポーネントを取得しておく
		entity.FetchInitialData();

		/// 生成、初期化の呼び出し用リストに追加
		awakeList_.Add(entity);
		initList_.Add(entity);
	}

	/// <summary>
	/// C/C++側から呼び出すコンポーネントの追加関数
	/// </summary>
	public void AddScript(int _entityId, MonoScript _behavior, bool _enable) {
		Entity entity;
		if (entityMap_.TryGetValue(_entityId, out entity)) {
			//Debug.LogInfo("ECSGroup.AddScript - Adding script to Entity ID: " + _entityId + ", Script Name: " + _behavior.GetType().Name);
			_behavior.CreateBehavior(_entityId, _behavior.GetType().Name, this);
			_behavior.enable = _enable;
			entity.AddScript(_behavior);

            // もしエンティティが既に初期化済みリストに含まれていない（＝既に初期化が走った後である）なら
            // この場で初期化関数を呼ぶ必要がある
            if (!awakeList_.Contains(entity) && !initList_.Contains(entity)) {
                _behavior.Awake();
                _behavior.Initialize();
                Debug.Log($"ECSGroup: Dynamically initialized script {_behavior.GetType().Name} for Entity {_entityId}");
            }
		} else {
			//Debug.LogError("Entity.AddScript - Entity not found with ID: " + _entityId);
		}
	}

	/// <summary>
	/// c#側から呼び出すエンティティの生成関数
	/// </summary>
	public Entity CreateEntity(string _prefabName) {
		int id = 0;
		InternalCreateEntity(out id, _prefabName, groupName);
		Entity entity = new Entity(id, this);
		entityMap_.Add(id, entity);
		entities_.Add(entity);

		// 誰が生成しているかログを出す
		Debug.LogInfo($"[ENTITY_SPAWN] Prefab: {_prefabName} spawned by C# script. ID: {id}");


		awakeList_.Add(entity); //!< 生成されたエンティティを生成リストに追加
		initList_.Add(entity); //!< 初期化リストにも追加
		//Debug.Log("ECSGroup.CreateEntity - AwakeListCount: " + awakeList_.Count + ", InitListCount: " + initList_.Count);

		return entity;
	}

	// ==============================================
	// 更新
	// ==============================================

	/// <summary>
	/// リストないのエンティティの更新処理を呼ぶ
	/// </summary>
	public void UpdateEntities() {
		//!< 無効なら処理しない
		if (!enable_) {
			return;
		}

		try {
			var sw = Stopwatch.StartNew();

			// 受信を最初に行う
			componentCollection.GetArray<MeshRenderer>();
			ComponentBatchManager.ReceiveAllBatches(componentCollection, groupName);

			CallAwake();
			CallInitialize();

			// ヒエラルキー順に更新を行う（スタックオーバーフロー防止のためループで実装）
			UpdateEntitiesIterative();

			ComponentBatchManager.SendAllBatches(componentCollection, groupName);

			// フレームの終わりに描画データをC++へ送る
			GizmoBatch.SubmitBatch();

			sw.Stop();
		} catch (Exception e) {
			Debug.LogError($"[ECSGroup] Exception in UpdateEntities ({groupName}): {e.Message}\n{e.StackTrace}");
			throw;
		}
	}

	/// <summary>
	/// スタックを使用して非再帰的にエンティティを更新する
	/// </summary>
	private void UpdateEntitiesIterative() {
		// ルートエンティティをスタックに積む
		// スタックを使うと「深さ優先（子を先に）」になる
		Stack<Entity> stack = new Stack<Entity>();
		
		int rootCount = InternalGetRootEntityCount(groupName);
		// スタックなので逆順に積むことで取得順（0, 1, 2...）に処理されるようにする
		for (int i = rootCount - 1; i >= 0; i--) {
			int rootId = InternalGetRootEntityId(groupName, i);
			Entity entity = GetEntity(rootId);
			if (entity != null) {
				stack.Push(entity);
			}
		}

		while (stack.Count > 0) {
			Entity current = stack.Pop();
			if (current == null || current.Id == 0 || !current.enable) continue;

			// --- 現在のエンティティのスクリプト更新 ---
			// 更新中にリストが変更される可能性があるためコピーを使用
			var scripts = current.GetScripts(); 
			foreach (MonoScript script in scripts) {
				if (script != null && script.enable) {
					try {
						script.Update();
					} catch (Exception e) {
						Debug.LogError($"[ECSGroup] Exception in script '{script.GetType().Name}' on entity '{current.name}' (ID:{current.Id}): {e.Message}\n{e.StackTrace}");
						throw;
					}
				}
			}

			// --- 子エンティティをスタックに追加 ---
			uint childCount = current.GetChildCount();
			// 逆順に積むことで、Popした時にインデックス順に処理される
			for (int i = (int)childCount - 1; i >= 0; i--) {
				Entity child = current.GetChild((uint)i);
				if (child != null) {
					stack.Push(child);
				}
			}
		}
	}

	// 以前の再帰メソッドは削除（またはプライベート化して中身を空に）
	private void UpdateRecursive(Entity _entity) {
		// 実装を UpdateEntitiesIterative へ移行したため廃止
	}


	/// <summary>
	/// 生成されたエンティティの生成関数の呼び出しを行う
	/// </summary>
	private void CallAwake() {
		if (awakeList_.Count == 0) {
			return;
		}

/*
#if DEBUG
		Debug.InternalLog("");
		Debug.InternalLog("//////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.InternalLog("ECSGroup.CallAwake - Awakening entities in group: " + groupName + ", Count: " + awakeList_.Count);
#endif
*/

		List<Entity> entitiesToAwake = new List<Entity>(awakeList_);
		awakeList_.Clear(); // 生成リストをクリア
		foreach (Entity entity in entitiesToAwake) {
			foreach (MonoScript script in entity.GetScripts()) {
				script.Awake();
			}
		}

/*
#if DEBUG
		Debug.InternalLog("//////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.InternalLog("");
#endif
*/
	}


	/// <summary>
	/// 生成されたエンティティの初期化関数の呼び出しを行う
	/// </summary>
	private void CallInitialize() {
		if (initList_.Count == 0) {
			return;
		}

/*
#if DEBUG
		Debug.InternalLog("");
		Debug.InternalLog("//////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.InternalLog("ECSGroup.CallInitialize - Initializing entities in group: " + groupName + ", Count: "
				  + initList_.Count);
#endif
*/

		List<Entity> entitiesToInitialize = new List<Entity>(initList_);
		initList_.Clear();
		foreach (Entity entity in entitiesToInitialize) {
			foreach (MonoScript script in entity.GetScripts()) {
				script.Initialize();
			}
		}

/*
#if DEBUG
		Debug.InternalLog("//////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.InternalLog("");
#endif
*/
	}

	/// <summary>
	/// Entityの取得
	/// </summary>
	public Entity GetEntity(int _id) {
		if (entityMap_.TryGetValue(_id, out Entity entity)) {
#if DEBUG
			//Debug.Log("ECSGroup.GetEntity - Entity found with ID: " + entity.Id + ", Entity Name: " + entity.name);
#endif
			return entity;
		}

#if DEBUG
		//Debug.LogError("ECSGroup.GetEntity - Entity not found with ID: " + _id + ", Group Name: " + groupName);
#endif
		return null;
	}

	/// <summary>
	/// エンティティの削除
	/// </summary>
	public void DestroyEntity(int _id) {
		if (entityMap_.TryGetValue(_id, out Entity entity)) {
			entityMap_.Remove(_id);
			entities_.Remove(entity);
			InternalDestroyEntity(groupName, _id);
#if DEBUG
			Debug.Log("Entity destroyed with ID: " + _id);
		} else {
			Debug.LogError("Entity not found with ID: " + _id);
#endif
		}
	}

	/// <summary>
	/// すべてのエンティティを削除
	/// </summary>
	public void DeleteEntityAll() {
#if DEBUG
		Debug.Log("ECSGroup.DeleteEntityAll - Deleting all entities in group: " + groupName + ", EntityCount: "
				  + entities_.Count);
#endif

		var entitiesToDestroy = new List<Entity>(entities_);
		foreach (var entity in entitiesToDestroy) {
			entity.Destroy();
		}
	}

	/// <summary>
	/// シーン遷移時のクリア処理（C++側のエンティティは削除しない）
	/// </summary>
	public void ClearForSceneTransition() {
#if DEBUG
		Debug.Log("ECSGroup.ClearForSceneTransition - Clearing C# state for group: " + groupName + ", EntityCount: "
				  + entities_.Count);
#endif
		// スクリプトの破棄イベントを呼ぶ
		foreach (var entity in entities_) {
			foreach (var script in entity.GetScripts()) {
				script.OnDestroy();
			}
		}

		// C#側の状態のみをクリアする
		entities_.Clear();
		entityMap_.Clear();
		awakeList_.Clear();
		initList_.Clear();
		componentCollection.Clear();
	}

	/// <summary>
	/// すべてのエンティティを取得
	/// </summary>
	public IEnumerable<Entity> GetEntities() {
		return entities_;
	}

	/// <summary>
	/// エンティティの探索
	/// </summary>
	public Entity FindEntity(string _name) {
		foreach (var entity in entities_) {
			if (entity.name == _name) {
				return entity;
			}
		}

#if DEBUG
		Debug.LogError("Entity not found with name: " + _name);
#endif
		return null;
	}


	bool CheckEnable(Entity _entity) {
		if (!_entity) {
			return false;
		}

		if (!_entity.enable) {
			return false;
		}

		Entity parent = _entity.parent;
		if (parent) {
			return CheckEnable(parent);
		}

		return true;
	}


	///////////////////////////////////////////////////////////////////////////////////////////
	// internal methods
	///////////////////////////////////////////////////////////////////////////////////////////

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalCreateEntity(out int _id, string _prefabName, string _groupName);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern int InternalGetRootEntityCount(string _groupName);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern int InternalGetRootEntityId(string _groupName, int _index);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalDestroyEntity(string _ecsGroupName, int _id);

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalSetEnable();

	[MethodImpl(MethodImplOptions.InternalCall)]
	static extern void InternalFindEntity(string _ecsGroupName, string _entityName);


	///////////////////////////////////////////////////////////////////////////////////////////
	// operators
	///////////////////////////////////////////////////////////////////////////////////////////

	public static implicit operator bool(ECSGroup _group) {
		return _group != null;
	}
}
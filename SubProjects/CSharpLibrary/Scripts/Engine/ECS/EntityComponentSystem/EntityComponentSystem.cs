

using System.Collections.Generic;

static public class EntityComponentSystem {
	
	///////////////////////////////////////////////////////////////////////////////////////////
	// objects
	///////////////////////////////////////////////////////////////////////////////////////////
	
	static private Dictionary<string, ECSGroup> groups = new Dictionary<string, ECSGroup>();

	/// <summary>
	/// すべてのECSGroupを取得
	/// </summary>
	static public IEnumerable<ECSGroup> GetAllGroups() {
		return groups.Values;
	}
	
	///////////////////////////////////////////////////////////////////////////////////////////
	// methods
	///////////////////////////////////////////////////////////////////////////////////////////

	/// <summary>
	/// 新規グループの追加
	/// </summary>
	static public ECSGroup AddECSGroup(string _name) {
		string trimmedName = _name.Trim();
		if (groups.TryGetValue(trimmedName, out ECSGroup existingGroup)) {
			// 既に存在する場合は、そのグループを返す（クリアは明示的に行う必要がある）
			return existingGroup;
		}

		ECSGroup group = new ECSGroup(trimmedName);
		groups.Add(trimmedName, group);
		return group;
	}

	/// <summary>
	/// グループのクリア
	/// </summary>
	static public void ClearECSGroup(string _name) {
		string trimmedName = _name.Trim();
		if (groups.TryGetValue(trimmedName, out ECSGroup group)) {
			group.ClearForSceneTransition();
		}
	}

	/// <summary>
	/// ECSGroupの取得
	/// </summary>
	static public ECSGroup GetECSGroup(string _name) {
		string trimmedName = _name.Trim();
#if DEBUG
#endif

		if (groups.TryGetValue(trimmedName, out ECSGroup group)) {
			return group;
		} else {
			// キー名に空白が含まれている可能性があるため、全件チェック
			foreach (var kvp in groups) {
				if (kvp.Key.Trim() == trimmedName) {
					return kvp.Value;
				}
			}

#if DEBUG
			foreach (var ecsGroup in groups) {
			}
#endif
			return null;
		}
	}

	
	/// <summary>
	/// すべてのGroupのエンティティを削除する
	/// </summary>
	static public void DeleteEntityAll() {
#if DEBUG
#endif

		foreach (var group in groups.Values) {
			group.DeleteEntityAll();
		}
	}
	

	static public Entity GetEntity(string _groupName, int _id) {
	#if DEBUG
		// foreach (var g in groups) {
		// }
	#endif


		if (groups.TryGetValue(_groupName, out ECSGroup group)) {
	#if DEBUG
	#endif
			return group.GetEntity(_id);
		} else {
	#if DEBUG
	#endif
			return null;
		}
	}

	static public MonoScript GetMonoBehavior(string _groupName, int _entityId, string _scriptName) {
	#if DEBUG
	#endif

		if (groups.TryGetValue(_groupName, out ECSGroup group)) {			Entity entity = group.GetEntity(_entityId);
			if (entity != null) {
				return entity.GetScript(_scriptName);
			} else {
#if DEBUG
#endif
				return null;
			}
		} else {
#if DEBUG
#endif
			return null;
		}
	}

}


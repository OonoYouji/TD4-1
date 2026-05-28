

using System.Collections.Generic;

static public class EntityComponentSystem {
	
	///////////////////////////////////////////////////////////////////////////////////////////
	// objects
	///////////////////////////////////////////////////////////////////////////////////////////
	
	static private Dictionary<string, ECSGroup> groups = new Dictionary<string, ECSGroup>();
	
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
		Debug.LogInfo("EntityComponentSystem.AddECSGroup - added: '" + group.groupName + "'  GroupCount " + groups.Count);
		return group;
	}

	/// <summary>
	/// グループのクリア
	/// </summary>
	static public void ClearECSGroup(string _name) {
		string trimmedName = _name.Trim();
		if (groups.TryGetValue(trimmedName, out ECSGroup group)) {
			group.ClearForSceneTransition();
			Debug.LogInfo("EntityComponentSystem.ClearECSGroup - cleared: '" + group.groupName + "'");
		}
	}

	/// <summary>
	/// ECSGroupの取得
	/// </summary>
	static public ECSGroup GetECSGroup(string _name) {
		string trimmedName = _name.Trim();
#if DEBUG
		Debug.LogInfo("EntityComponentSystem.GetECSGroup - Getting ECSGroup: '" + trimmedName + "'  GroupCount " + groups.Count);
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
			Debug.LogError("EntityComponentSystem.GetECSGroup - ECSGroup not found: '" + trimmedName + "'  GroupCount " + groups.Count);
			foreach (var ecsGroup in groups) {
				Debug.LogError("Available ECSGroups: '" + ecsGroup.Key + "'");
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
		Debug.Log("//////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.Log("EntityComponentSystem.DeleteEntityAll - Deleting all entities from all ECSGroups. GroupCount: " + groups.Count);
		Debug.Log("//////////////////////////////////////////////////////////////////////////////////////////////////////////////////");
		Debug.LogInfo("All entities deleted from all ECSGroups.");
#endif

		foreach (var group in groups.Values) {
			group.DeleteEntityAll();
		}
	}
	

	static public Entity GetEntity(string _groupName, int _id) {
#if DEBUG
		Debug.LogInfo("EntityComponentSystem.GetEntity - Getting Entity from group: " + _groupName + ", ID: " + _id);
		Debug.LogInfo("EntityComponentSystem.GetEntity - GroupCount: " + groups.Count);
		foreach (var g in groups) {
			Debug.LogInfo("EntityComponentSystem.GetEntity - Available ECSGroup: " + g.Key);
		}
#endif


		if (groups.TryGetValue(_groupName, out ECSGroup group)) {
#if DEBUG
			Debug.LogInfo("EntityComponentSystem.GetEntity - ECSGroup found: " + group.groupName);
#endif
			return group.GetEntity(_id);
		} else {
#if DEBUG
			Debug.LogError("EntityComponentSystem.GetEntity - ECSGroup not found: " + _groupName);
#endif
			return null;
		}
	}

	static public MonoScript GetMonoBehavior(string _groupName, int _entityId, string _scriptName) {
#if DEBUG
		Debug.LogInfo("EntityComponentSystem.GetMonoBehavior - Getting MonoBehavior from group: " + _groupName + ", Entity ID: " + _entityId + ", Script Name: " + _scriptName);
		Debug.LogInfo("EntityComponentSystem.GetEntity - GroupCount: " + groups.Count);
#endif
		
		if (groups.TryGetValue(_groupName, out ECSGroup group)) {
			Entity entity = group.GetEntity(_entityId);
			if (entity != null) {
				return entity.GetScript(_scriptName);
			} else {
#if DEBUG
				Debug.LogError("EntityComponentSystem.GetMonoBehavior - Entity not found with ID: " + _entityId);
#endif
				return null;
			}
		} else {
#if DEBUG
			Debug.LogError("EntityComponentSystem.GetMonoBehavior - ECSGroup not found: " + _groupName);
#endif
			return null;
		}
	}

}
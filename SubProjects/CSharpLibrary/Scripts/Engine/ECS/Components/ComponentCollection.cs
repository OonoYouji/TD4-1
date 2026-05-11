using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IComponentArray {
	int Count {
		get;
	}
}

public class ComponentArray<T> : IComponentArray where T : Component {
	public List<T> components = new List<T>();

	public int Count => components.Count;

	public T Get(int _index) {
		return components[_index];
	}
}

public class ComponentCollection {
	Dictionary<Type, IComponentArray> arrays_ = new Dictionary<Type, IComponentArray>();


	public void AddComponent<T>(T _component) where T : Component {
		if (!arrays_.ContainsKey(typeof(T))) {
			arrays_[typeof(T)] = new ComponentArray<T>();
		}

		ComponentArray<T> array = (ComponentArray<T>)arrays_[typeof(T)];
		array.components.Add(_component);
	}

	public void RemoveComponent<T>(T _component) where T : Component {
		if (!arrays_.ContainsKey(typeof(T))) {
			return;
		}

		ComponentArray<T> array = (ComponentArray<T>)arrays_[typeof(T)];
		array.components.Remove(_component);
	}

	public bool TryGetArray(Type _type, out IComponentArray _array) {
		return arrays_.TryGetValue(_type, out _array);
	}

	public ComponentArray<T> GetArray<T>() where T : Component {
		Debug.Log($"GetArray<{typeof(T).Name}>: Checking if key exists."); // 追加ログ
		if (!arrays_.ContainsKey(typeof(T))) {
			Debug.LogError($"GetArray<{typeof(T).Name}>: Key not found. Creating new ComponentArray."); // 追加ログ
			arrays_[typeof(T)] = new ComponentArray<T>();
			Debug.Log($"GetArray<{typeof(T).Name}>: Key added. Count: {arrays_.Count}"); // 追加ログ
		} else {
			Debug.Log($"GetArray<{typeof(T).Name}>: Key found. Returning existing array."); // 追加ログ
		}		return (ComponentArray<T>)arrays_[typeof(T)];
	}

}
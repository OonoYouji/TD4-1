using System.Collections.Generic;

/// <summary>
/// AIのデータ共有用ストレージ。
/// 文字列キーの代わりにハッシュを使用することで、GCの発生を抑え、高速なアクセスを実現する。
/// 値型（int, float, bool, Vector3）については個別の辞書で管理し、ボクシングを防ぐ。
/// </summary>
public class Blackboard
{
    private readonly Dictionary<uint, int> _intData = new Dictionary<uint, int>();
    private readonly Dictionary<uint, float> _floatData = new Dictionary<uint, float>();
    private readonly Dictionary<uint, bool> _boolData = new Dictionary<uint, bool>();
    private readonly Dictionary<uint, Vector3> _vector3Data = new Dictionary<uint, Vector3>();
    private readonly Dictionary<uint, object> _objectData = new Dictionary<uint, object>();

    public void SetInt(uint key, int value) => _intData[key] = value;
    public int GetInt(uint key, int defaultValue = 0) => _intData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetFloat(uint key, float value) => _floatData[key] = value;
    public float GetFloat(uint key, float defaultValue = 0f) => _floatData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetBool(uint key, bool value) => _boolData[key] = value;
    public bool GetBool(uint key, bool defaultValue = false) => _boolData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetVector3(uint key, Vector3 value) => _vector3Data[key] = value;
    public Vector3 GetVector3(uint key) => _vector3Data.TryGetValue(key, out var val) ? val : Vector3.zero;

    public void SetObject(uint key, object value) => _objectData[key] = value;
    public T GetObject<T>(uint key) where T : class
    {
        if (_objectData.TryGetValue(key, out var val) && val is T typedVal) return typedVal;
        return null;
    }

    public bool HasKey(uint key)
    {
        return _intData.ContainsKey(key) || _floatData.ContainsKey(key) || 
               _boolData.ContainsKey(key) || _vector3Data.ContainsKey(key) || 
               _objectData.ContainsKey(key);
    }

    public void Remove(uint key)
    {
        _intData.Remove(key);
        _floatData.Remove(key);
        _boolData.Remove(key);
        _vector3Data.Remove(key);
        _objectData.Remove(key);
    }

    public void Clear()
    {
        _intData.Clear();
        _floatData.Clear();
        _boolData.Clear();
        _vector3Data.Clear();
        _objectData.Clear();
    }
}

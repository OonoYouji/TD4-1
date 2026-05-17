using System;
using System.Collections.Generic;

/// <summary>
/// AIのデータ共有用ストレージ。
/// 値が変更された際に通知を行う Observer パターンをサポートする。
/// </summary>
public class Blackboard
{
    public event Action<uint> OnValueChanged;

    private readonly Dictionary<uint, int> _intData = new Dictionary<uint, int>();
    private readonly Dictionary<uint, float> _floatData = new Dictionary<uint, float>();
    private readonly Dictionary<uint, bool> _boolData = new Dictionary<uint, bool>();
    private readonly Dictionary<uint, Vector3> _vector3Data = new Dictionary<uint, Vector3>();
    private readonly Dictionary<uint, string> _stringData = new Dictionary<uint, string>();
    private readonly Dictionary<uint, object> _objectData = new Dictionary<uint, object>();

    public void SetInt(uint key, int value) 
    { 
        _intData[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public int GetInt(uint key, int defaultValue = 0) => _intData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetFloat(uint key, float value) 
    { 
        _floatData[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public float GetFloat(uint key, float defaultValue = 0f) => _floatData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetBool(uint key, bool value) 
    { 
        _boolData[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public bool GetBool(uint key, bool defaultValue = false) => _boolData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetVector3(uint key, Vector3 value) 
    { 
        _vector3Data[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public Vector3 GetVector3(uint key) => _vector3Data.TryGetValue(key, out var val) ? val : Vector3.zero;

    public void SetString(uint key, string value) 
    { 
        _stringData[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public string GetString(uint key, string defaultValue = "") => _stringData.TryGetValue(key, out var val) ? val : defaultValue;

    public void SetObject(uint key, object value) 
    { 
        _objectData[key] = value; 
        OnValueChanged?.Invoke(key);
    }
    public T GetObject<T>(uint key) where T : class
    {
        if (_objectData.TryGetValue(key, out var val) && val is T typedVal) return typedVal;
        return null;
    }

    /// <summary>
    /// 指定したキーの値をobject型として取得する（型が不明な場合やデバッグ用）
    /// </summary>
    public object GetValueAsObject(uint key)
    {
        if (_intData.TryGetValue(key, out var i)) return i;
        if (_floatData.TryGetValue(key, out var f)) return f;
        if (_boolData.TryGetValue(key, out var b)) return b;
        if (_vector3Data.TryGetValue(key, out var v)) return v;
        if (_stringData.TryGetValue(key, out var s)) return s;
        if (_objectData.TryGetValue(key, out var o)) return o;
        return null;
    }

    public bool HasKey(uint key)
    {
        return _intData.ContainsKey(key) || _floatData.ContainsKey(key) || 
               _boolData.ContainsKey(key) || _vector3Data.ContainsKey(key) || 
               _stringData.ContainsKey(key) || _objectData.ContainsKey(key);
    }

    public void Remove(uint key)
    {
        _intData.Remove(key);
        _floatData.Remove(key);
        _boolData.Remove(key);
        _vector3Data.Remove(key);
        _stringData.Remove(key);
        _objectData.Remove(key);
    }

    public void Clear()
    {
        _intData.Clear();
        _floatData.Clear();
        _boolData.Clear();
        _vector3Data.Clear();
        _stringData.Clear();
        _objectData.Clear();
    }
}

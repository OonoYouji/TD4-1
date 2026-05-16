using System.Collections.Generic;

/// <summary>
/// AIのデータ共有用ストレージ。
/// 文字列キーの代わりにハッシュを使用することで、GCの発生を抑え、高速なアクセスを実現する。
/// </summary>
public class Blackboard
{
    private readonly Dictionary<uint, object> _data = new Dictionary<uint, object>();

    public void SetValue<T>(uint key, T value)
    {
        _data[key] = value;
    }

    public T GetValue<T>(uint key)
    {
        if (_data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default(T);
    }
    
    public bool TryGetValue<T>(uint key, out T value)
    {
        if (_data.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default(T);
        return false;
    }

    public bool HasKey(uint key)
    {
        return _data.ContainsKey(key);
    }

    public bool Remove(uint key)
    {
        return _data.Remove(key);
    }

    public void Clear()
    {
        _data.Clear();
    }
}

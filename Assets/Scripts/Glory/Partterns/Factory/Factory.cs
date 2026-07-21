using System;
using System.Collections.Generic;
using UnityEngine;

public interface IFactory<T, TEnum>
    where T : MonoBehaviour
    where TEnum : struct, Enum
{
    T Create(TEnum _type);
}

public interface IMemoryPoolFactory<T, TEnum> : IFactory<T, TEnum>
    where T : FactoryObject
    where TEnum : struct, Enum
{
    bool Recycle(TEnum _type, T _obj);
    void Prewarm();
    void Clear();
}

public class MemoryPoolFactory<T, TEnum> : IMemoryPoolFactory<T, TEnum>
    where T : FactoryObject
    where TEnum : struct, Enum
{
    private Dictionary<TEnum, MemoryPooling<T>> m_MemoryPoolDictionary = new Dictionary<TEnum, MemoryPooling<T>>();

    /// <param name="_pathMap">enum 값별 Resources 경로 매핑</param>
    /// <param name="_maxCount">풀당 사전 생성(Prewarm) 개수</param>
    /// <param name="_parent">생성된 오브젝트의 부모 Transform</param>
    public MemoryPoolFactory(Dictionary<TEnum, string> _pathMap, int _maxCount, Transform _parent)
    {
        foreach (var entry in _pathMap)
        {
            m_MemoryPoolDictionary.Add(entry.Key, new MemoryPooling<T>(_maxCount, entry.Value, _parent));
        }
    }

    public T Create(TEnum _type)
    {
        if (m_MemoryPoolDictionary.TryGetValue(_type, out MemoryPooling<T> pool) == false)
        {
            Logger.Error($"[MemoryPoolFactory] 등록되지 않은 타입: {_type}");
            return null;
        }

        T obj = pool.Pop();
        if (obj == null)
            return null;

        obj.Open();
        return obj;
    }

    public bool Recycle(TEnum _type, T _obj)
    {
        if (_obj == null)
        {
            Logger.Error($"[MemoryPoolFactory] Recycle 실패 — null 오브젝트: {_type}");
            return false;
        }

        if (m_MemoryPoolDictionary.TryGetValue(_type, out MemoryPooling<T> pool) == false)
        {
            Logger.Error($"[MemoryPoolFactory] 등록되지 않은 타입: {_type}");
            return false;
        }

        // 풀 반납이 실제로 성공한 경우에만 Close — 이중 반납/미소속 오브젝트에 부작용 방지
        if (pool.Push(_obj) == false)
            return false;

        _obj.Close();
        return true;
    }

    public void Prewarm()
    {
        foreach (MemoryPooling<T> pool in m_MemoryPoolDictionary.Values)
        {
            pool.Prewarm();
        }
    }

    public void Clear()
    {
        foreach (MemoryPooling<T> pool in m_MemoryPoolDictionary.Values)
        {
            pool.Clear();
        }
    }

    public virtual void UpdateLogic()
    {
        foreach (MemoryPooling<T> pool in m_MemoryPoolDictionary.Values)
        {
            pool.UpdateLogic();
        }
    }
}

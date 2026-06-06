using System.Collections.Generic;
using UnityEngine;

public class MemoryPooling<T> where T : Component
{
    private readonly int m_MaxCount;
    private List<T> m_ActiveList = new List<T>();
    private List<T> m_HideList = new List<T>();

    private string m_Path;
    private Transform m_Parent;

    public MemoryPooling(int _maxCount, string _path, Transform _parent)
    {
        m_MaxCount = _maxCount;
        m_Path = _path;
        m_Parent = _parent;
    }

    public void Prewarm()
    {
        for (int i = 0; i < m_MaxCount; ++i)
        {
            T obj = ResUtil.Create<T>(m_Path, m_Parent, true);
            if (obj == null)
                break;

            obj.gameObject.SetActive(false);
            m_HideList.Add(obj);
        }
    }

    public T Pop()
    {
        T obj;

        if (m_HideList.Count > 0)
        {
            int lastIndex = m_HideList.Count - 1;
            obj = m_HideList[lastIndex];
            m_HideList.RemoveAt(lastIndex);
        }
        else
        {
            obj = ResUtil.Create<T>(m_Path, m_Parent, true);
        }

        if (obj == null)
        {
            Debug.LogError($"[MemoryPooling] Pop 실패 — 경로: {m_Path}");
            return null;
        }

        m_ActiveList.Add(obj);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public bool Push(T _obj)
    {
        bool isActive = m_ActiveList.Remove(_obj);
        if (isActive == true)
        {
            _obj.gameObject.SetActive(false);
            m_HideList.Add(_obj);
        }
        return isActive;
    }

    public void Clear()
    {
        foreach (T obj in m_ActiveList)
        {
            GameObject.Destroy(obj.gameObject);
        }
        foreach (T obj in m_HideList)
        {
            GameObject.Destroy(obj.gameObject);
        }

        m_ActiveList.Clear();
        m_HideList.Clear();
    }

    public virtual void UpdateLogic()
    {
    }
}

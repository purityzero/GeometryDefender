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
        // 중복 호출 시 풀 오브젝트가 배수로 늘어나는 것을 방지
        if (m_ActiveList.Count > 0 || m_HideList.Count > 0)
            return;

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
            Logger.Error($"[MemoryPooling] Pop 실패 — 경로: {m_Path}");
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
        // 씬 전환 등으로 풀 오브젝트(m_Parent의 자식)가 이미 파괴된 상태일 수 있음 — 그런 경우는 다시 Destroy할 필요 없이 건너뜀
        foreach (T obj in m_ActiveList)
        {
            if (obj == null)
                continue;

            GameObject.Destroy(obj.gameObject);
        }
        foreach (T obj in m_HideList)
        {
            if (obj == null)
                continue;

            GameObject.Destroy(obj.gameObject);
        }

        m_ActiveList.Clear();
        m_HideList.Clear();
    }

    public virtual void UpdateLogic()
    {
    }
}

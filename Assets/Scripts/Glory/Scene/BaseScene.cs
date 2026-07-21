using System.Collections.Generic;
using UnityEngine;

public abstract class BaseScene : SceneSingleton<BaseScene>
{
    private List<IUpdatable> m_UpdatableList = new List<IUpdatable>();

    private void Start()
    {
        OnSetup();
    }

    protected virtual void OnSetup() { }

    public void Register(IUpdatable _updatable)
    {
        if (m_UpdatableList.Contains(_updatable) == true)
            return;

        m_UpdatableList.Add(_updatable);
    }

    public void Unregister(IUpdatable _updatable)
    {
        m_UpdatableList.Remove(_updatable);
    }

    private void Update()
    {
        for (int i = 0; i < m_UpdatableList.Count; ++i)
        {
            m_UpdatableList[i].UpdateLogic();
        }
    }
}

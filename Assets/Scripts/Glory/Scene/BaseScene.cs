using System.Collections.Generic;
using UnityEngine;

public abstract class BaseScene : MonoBehaviour
{
    public static BaseScene Current { get; private set; }

    private List<IUpdatable> m_UpdatableList = new List<IUpdatable>();

    private void Awake()
    {
        Current = this;
    }

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

    private void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }
}

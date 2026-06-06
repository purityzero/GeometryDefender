using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Command_Delegate : ICommand
{
    UnityEngine.Events.UnityAction m_delegate;

    public Command_Delegate(UnityEngine.Events.UnityAction _delegate)
    {
        m_delegate = _delegate;
    }

    public void Execute()
    {
        if (null != m_delegate)
            m_delegate();
    }

    public void Update() { }
    public void Cancel() { }
    public bool IsFinished() { return true; }
}

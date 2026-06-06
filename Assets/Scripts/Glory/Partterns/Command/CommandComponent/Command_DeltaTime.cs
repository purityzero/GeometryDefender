using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Command_DeltaTime : ICommand
{
    UnityEngine.Events.UnityAction m_delegate;
    float m_maxTime;
    float m_time;
    bool m_isComplete;

    public Command_DeltaTime(float _maxTime, UnityEngine.Events.UnityAction _delegate)
    {
        m_delegate = _delegate;
        m_maxTime = _maxTime;
    }

    public void Cancel() { }
    public void Execute()
    {
        m_time = 0f;
        m_isComplete = false;
    }
    public void Update()
    {
        if (true == m_isComplete)
            return;

        m_time += Time.deltaTime;
        if( m_time >= m_maxTime )
        {
            if (null != m_delegate)
                m_delegate();
            m_isComplete = true;
        }
    }

    public bool IsFinished() { return m_isComplete; }
}

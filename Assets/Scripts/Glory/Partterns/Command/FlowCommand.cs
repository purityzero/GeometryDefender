using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowCommand : ICommand
{
    protected ICommand m_command;
    protected Queue<ICommand> m_commandList = new Queue<ICommand>();

    public void Execute()
    {

    }

    public bool IsFinished()
    {
        if (m_commandList.Count <= 0 && null == m_command)
            return true;

        return false;
    }

    public void Add(ICommand _command)
    {
        if (null == _command)
            return;
        m_commandList.Enqueue(_command);
    }

    public void Clear()
    {
        m_command = null;
        m_commandList.Clear();
    }

    public void Cancel()
    {
        var _var = m_commandList.GetEnumerator();
        while(_var.MoveNext())
        {
            _var.Current.Cancel();
        }
        m_commandList.Clear();

        if( null != m_command && m_command.IsFinished() == false )
        {
            m_command.Cancel();
        }

        m_command = null;
    }

    public void Update()
    {
        if (IsFinished() == true)
            return;
        if( m_command == null || m_command.IsFinished() )
        {
            if( m_commandList.Count > 0 )
            {
                m_command = m_commandList.Dequeue();
            }
            else
            {
                m_command = null;
            }

            if (null != m_command)
                m_command.Execute();
        }

        if (null != m_command)
            m_command.Update();
    }



}

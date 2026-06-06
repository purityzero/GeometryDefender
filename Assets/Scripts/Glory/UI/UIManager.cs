using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
	private Dictionary<string,  UIBase> m_UIDictinary = new Dictionary<string, UIBase>();
	private Dictionary<string,  UIBase> m_UIPopupDictinary = new Dictionary<string, UIBase>();

	private FlowCommand m_FlowCommand = new FlowCommand();

	public T Get<T>(string _name) where T : UIBase
	{
		UIBase obj = null;

		if(!m_UIDictinary.TryGetValue(_name, out obj))
		{
			obj = ResUtil.Load<T>(_name);
			m_UIDictinary.Add(_name, obj);
		}

		obj.transform.SetAsFirstSibling();
		obj.Show();
		return obj as T;
	}

	private void Update()
	{
		m_FlowCommand?.Update();
	}



}

public abstract class UIBase : MonoBehaviour
{
	public virtual void Show()
	{
		gameObject.SetActive(true);
	}

	public virtual void Close()
	{
		gameObject.SetActive(false);
	}


}

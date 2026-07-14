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
		UIBase cachedUI = null;
		m_UIDictinary.TryGetValue(_name, out cachedUI);

		// 씬 전환 등으로 인스턴스가 파괴된 경우도 재생성 대상
		if (cachedUI == null)
		{
			cachedUI = ResUtil.Create<T>(_name, transform);
			if (cachedUI == null)
				return null;

			m_UIDictinary[_name] = cachedUI;
		}

		cachedUI.transform.SetAsFirstSibling();
		cachedUI.Show();

		if (cachedUI is T == true)
		{
			var resultUI = cachedUI as T;
			return resultUI;
		}
		else
		{
			Debug.Log($"cachedUI is {typeof(T).Name} convert failed!");
			return null;
		}
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

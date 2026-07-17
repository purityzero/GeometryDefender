using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
	private const string UI_CANVAS_NAME = "UICanvas";
	private const string POPUP_CANVAS_NAME = "PopupCanvas";

	private const string TOAST_PREFAB_PATH = "Prefabs/UI/UIToastMessage";
	private const int TOAST_POOL_MAX_COUNT = 5;
	private const float TOAST_SLOT_HEIGHT = 90f;

	private Dictionary<string,  UIBase> m_UIDictinary = new Dictionary<string, UIBase>();
	private Dictionary<string,  UIBase> m_UIPopupDictinary = new Dictionary<string, UIBase>();

	private Transform m_UICanvas;
	private Transform m_PopupCanvas;

	private MemoryPooling<UIToastMessage> m_ToastPool;
	private List<UIToastMessage> m_ActiveToasts = new List<UIToastMessage>();

	private FlowCommand m_FlowCommand = new FlowCommand();

	public T Get<T>() where T : UIBase
	{
		UITable uiTable = TableManager.instance.GetTable<UITable>();
		if (uiTable == null)
			return null;

		UIRecord record = uiTable.GetRecordByName(typeof(T).Name);
		if (record == null)
		{
			Debug.Log($"[UIManager] Get Failed! UITable record not found - {typeof(T).Name}");
			return null;
		}

		bool isPopup = (record.UIType == eUIType.Popup) ? true : false;
		return Get<T>(record.PrefabPath, isPopup);
	}

	public T Get<T>(string _name) where T : UIBase
	{
		return Get<T>(_name, false);
	}

	private T Get<T>(string _name, bool _isPopup) where T : UIBase
	{
		Dictionary<string, UIBase> targetDictionary = (_isPopup == true) ? m_UIPopupDictinary : m_UIDictinary;

		UIBase cachedUI = null;
		targetDictionary.TryGetValue(_name, out cachedUI);

		// 씬 전환 등으로 인스턴스가 파괴된 경우도 재생성 대상
		if (cachedUI == null)
		{
			cachedUI = ResUtil.Create<T>(_name, GetCanvas(_isPopup));
			if (cachedUI == null)
				return null;

			SetFullStretch(cachedUI.transform);
			targetDictionary[_name] = cachedUI;
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

	private void SetFullStretch(Transform _target)
	{
		if (_target is RectTransform == true)
		{
			var rectTransform = _target as RectTransform;
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.localScale = Vector3.one;
		}
		else
		{
			Debug.Log($"_target is RectTransform convert failed! - {_target.name}");
		}
	}

	private Transform GetCanvas(bool _isPopup)
	{
		if (_isPopup == true)
		{
			if (m_PopupCanvas == null)
				m_PopupCanvas = transform.Find(POPUP_CANVAS_NAME);
			return (m_PopupCanvas != null) ? m_PopupCanvas : transform;
		}

		if (m_UICanvas == null)
			m_UICanvas = transform.Find(UI_CANVAS_NAME);
		return (m_UICanvas != null) ? m_UICanvas : transform;
	}

	public void ShowToast(string _message)
	{
		if (m_ToastPool == null)
		{
			m_ToastPool = new MemoryPooling<UIToastMessage>(TOAST_POOL_MAX_COUNT, TOAST_PREFAB_PATH, GetCanvas(true));
			m_ToastPool.Prewarm();
		}

		if (m_ActiveToasts.Count >= TOAST_POOL_MAX_COUNT)
		{
			UIToastMessage oldestToast = m_ActiveToasts[m_ActiveToasts.Count - 1];
			CloseToast(oldestToast);
		}

		UIToastMessage toast = m_ToastPool.Pop();
		if (toast == null)
			return;

		toast.Open();
		m_ActiveToasts.Insert(0, toast);
		toast.Show(_message, CloseToast);

		RepositionToastStack();
	}

	private void CloseToast(UIToastMessage _toast)
	{
		bool isRemoved = m_ActiveToasts.Remove(_toast);
		if (isRemoved == false)
			return;

		_toast.Close();
		m_ToastPool.Push(_toast);

		RepositionToastStack();
	}

	private void RepositionToastStack()
	{
		for (int i = 0; i < m_ActiveToasts.Count; ++i)
		{
			Vector2 targetPosition = new Vector2(0f, i * TOAST_SLOT_HEIGHT);
			m_ActiveToasts[i].MoveTo(targetPosition);
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

	// 마지막(리스트 끝)이 가장 최근에 열린 = 화면상 가장 위의 팝업. 뒤로가기는 이 팝업에만 전달된다
	private List<UIPopup> m_PopupStack = new List<UIPopup>();

	private FlowCommand m_FlowCommand = new FlowCommand();

	public T Get<T>() where T : UIBase
	{
		UITable uiTable = TableManager.instance.GetTable<UITable>();
		if (uiTable == null)
			return null;

		UIRecord record = uiTable.GetRecordByName(typeof(T).Name);
		if (record == null)
		{
			Logger.Log($"[UIManager] Get Failed! UITable record not found - {typeof(T).Name}");
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

		cachedUI.transform.SetAsLastSibling();
		cachedUI.Show();

		if (cachedUI is T == true)
		{
			var resultUI = cachedUI as T;
			return resultUI;
		}
		else
		{
			Logger.Log($"cachedUI is {typeof(T).Name} convert failed!");
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
			Logger.Log($"_target is RectTransform convert failed! - {_target.name}");
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

	public void RegisterPopup(UIPopup _popup)
	{
		if (m_PopupStack.Contains(_popup) == true)
			return;

		m_PopupStack.Add(_popup);
	}

	public void UnregisterPopup(UIPopup _popup)
	{
		m_PopupStack.Remove(_popup);
	}

	// 씬 전환 등 컨텍스트 자체가 사라지는 시점에 호출 — 뒤로가기로 안 닫히게 오버라이드된 팝업도 예외 없이 전부 닫는다
	public void CloseAllPopups()
	{
		List<UIPopup> popupsToClose = new List<UIPopup>(m_PopupStack);
		for (int i = 0; i < popupsToClose.Count; ++i)
		{
			popupsToClose[i].Close();
		}

		List<UIToastMessage> toastsToClose = new List<UIToastMessage>(m_ActiveToasts);
		for (int i = 0; i < toastsToClose.Count; ++i)
		{
			CloseToast(toastsToClose[i]);
		}
	}

	private void OnPressBackButton()
	{
		if (m_PopupStack.Count <= 0)
			return;

		UIPopup topPopup = m_PopupStack[m_PopupStack.Count - 1];
		topPopup.OnPressBackBtn();
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

		// Get<T>()의 SetAsLastSibling과 동일한 이유 — Pop 시점에 다시 최상단으로 올리지 않으면
		// 이 토스트를 처음 생성했을 때의 sibling index에 그대로 남아, 이후에 새로 연 팝업(Get<T>()가 매번
		// SetAsLastSibling으로 앞으로 옴)보다 뒤에 깔려 가려질 수 있다(2026-07-29, 사용자 리포트: RunOver 위에
		// MetaTree를 열면 토스트가 뒤에서 가려짐).
		toast.transform.SetAsLastSibling();

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

		// 안드로이드 하드웨어 뒤로가기가 Escape로 들어온다(새 Input System 기준, Keyboard 디바이스가 없는 환경 대비 null 체크)
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame == true)
			OnPressBackButton();
	}

}

public abstract class UIBase : MonoBehaviour, IUpdatable
{
	protected virtual void OnEnable()
	{
		BaseScene.Current.Register(this);
	}

	protected virtual void OnDisable()
	{
		BaseScene.Current?.Unregister(this);
	}

	public virtual void UpdateLogic() { }

	public virtual void Show()
	{
		gameObject.SetActive(true);
	}

	public virtual void Close()
	{
		gameObject.SetActive(false);
	}


}

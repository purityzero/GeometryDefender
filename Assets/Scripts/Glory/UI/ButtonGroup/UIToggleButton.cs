using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO
// Lock 기능 세부 설정에 관한 필요

public class UIToggleButton : MonoBehaviour
{
    public bool isOn => m_isOn;
    public TextMeshProUGUI textOn => m_TextOn;
    public TextMeshProUGUI textOff => m_TextOff;

    [SerializeField] Button m_SelectButton;
    [Header("# Toggle On/Off")]
    [SerializeField] GameObject m_GoOn;
    [SerializeField] GameObject m_GoOff;
    [Header("# Toggle Icon")]
    [SerializeField] Image m_ImageOn;
    [SerializeField] Image m_ImageOff;
    [SerializeField] TextMeshProUGUI m_TextOn;
    [SerializeField] TextMeshProUGUI m_TextOff;
    [SerializeField] GameObject m_LockObject;

    private bool m_isOn = false;
    private UnityEngine.Events.UnityAction<UIToggleButton> m_SelectCB;

    /// <summary>
    /// 아이콘/텍스트까지 직접 지정해서 초기화하는 SetData 함수
    /// </summary>
    /// <param name="_onSprite">On 상태 아이콘 (null이면 변경하지 않음)</param>
    /// <param name="_offSprite">Off 상태 아이콘 (null이면 변경하지 않음)</param>
    /// <param name="_onText">On 상태 텍스트</param>
    /// <param name="_offText">Off 상태 텍스트</param>
    /// <param name="_action">버튼 클릭 시 호출될 콜백</param>
    public void SetData(Sprite _onSprite, Sprite _offSprite, string _onText, string _offText, UnityEngine.Events.UnityAction<UIToggleButton> _action)
    {
        if (m_ImageOn != null && _onSprite != null)
            m_ImageOn.sprite = _onSprite;

        if (m_ImageOff != null && _offSprite != null)
            m_ImageOff.sprite = _offSprite;

        if (m_TextOn != null)
            m_TextOn.SetText(_onText);

        if (m_TextOff != null)
            m_TextOff.SetText(_offText);

        SetData(false, _action);
    }

    public void SetData(bool _isOn, UnityEngine.Events.UnityAction<UIToggleButton> _action)
    {
        m_SelectCB = _action;
        SetToggle(_isOn);

        if (m_SelectButton == null)
        {
            Logger.Error($"[UIToggleButton] SetData Failed! m_SelectButton == null - {gameObject.name}");
            return;
        }

        m_SelectButton.onClick.RemoveAllListeners();
        m_SelectButton.onClick.AddListener(OnClickToggle);
    }

    public void SetLock(bool _isLocked)
    {
        if (m_LockObject != null)
            m_LockObject.SetActive(_isLocked);

        if (m_SelectButton == null)
            return;

        m_SelectButton.interactable = _isLocked == false;
    }

    public void SetToggle(bool _isOn)
    {
        m_isOn = _isOn;
        m_GoOn.SetActive(m_isOn);
        m_GoOff.SetActive(m_isOn == false);
    }

    protected virtual void OnClickToggle()
    {
        SetToggle(m_isOn == false);
        m_SelectCB?.Invoke(this);
    }
}

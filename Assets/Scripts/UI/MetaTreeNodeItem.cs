using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaTreeNodeItem : UIToggleButton
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;
    [SerializeField] private GameObject m_LockIconObject;
    [SerializeField] private GameObject m_CostGroupObject;
    [SerializeField] private TextMeshProUGUI m_CompletedText;

    public void SetData(string _name, int _cost, Color _iconColor)
    {
        m_NameText.SetText(_name);
        m_CostText.SetText(_cost.ToString());
        m_IconImage.color = _iconColor;
    }

    // m_LockIconObject/m_CostGroupObject는 베이스(UIToggleButton)의 m_GoOn/m_GoOff와 같은 오브젝트를 가리키는 별도 참조 —
    // 완료 상태에서는 그 둘을 전부 강제로 숨기고 완료 문구만 보여줘야 해서, private인 베이스 필드 대신 이 클래스가 직접 참조를 들고 제어한다.
    public void SetCompleted(bool _isCompleted, string _completedLabel)
    {
        m_CompletedText.gameObject.SetActive(_isCompleted);

        if (_isCompleted == true)
        {
            m_CompletedText.SetText(_completedLabel);
            m_LockIconObject.SetActive(false);
            m_CostGroupObject.SetActive(false);
        }
    }
}

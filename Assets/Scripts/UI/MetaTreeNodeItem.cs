using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetaTreeNodeItem : UIToggleButton
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private TextMeshProUGUI m_CostText;
    [SerializeField] private Image m_IconImage;

    public void SetData(string _name, int _cost, Color _iconColor)
    {
        m_NameText.SetText(_name);
        m_CostText.SetText(_cost.ToString());
        m_IconImage.color = _iconColor;
    }
}

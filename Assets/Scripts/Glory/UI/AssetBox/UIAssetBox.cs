using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssetBox : UIBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI m_AmountText;
    [SerializeField] private eCurrencyType m_CurrencyType = eCurrencyType.Max;

    public void SetData(eCurrencyType _currencyType, long _amount)
    {
        m_CurrencyType = _currencyType;
        m_AmountText.SetText(_amount.ToString());
    }

    public void SetData(eCurrencyType _currencyType)
    {
        m_CurrencyType = _currencyType;

        long amount = GetCurrencyAmount(_currencyType);
        m_AmountText.SetText(amount.ToString());
    }

    public void SetData()
    {
        if (m_CurrencyType == eCurrencyType.Max)
        {
            Debug.LogError($"[UIAssetBox] SetData Failed! currencyType is Max! - {gameObject.name}");
            return;
        }

        long amount = GetCurrencyAmount(m_CurrencyType);
        m_AmountText.SetText(amount.ToString());
    }

    public void SetData(long _amount)
    {
        if (m_CurrencyType == eCurrencyType.Max)
        {
            Debug.LogError($"[UIAssetBox] SetData Failed! currencyType is Max! - {gameObject.name}");
            return;
        }

        m_AmountText.SetText(_amount.ToString());
    }

    public void Refresh()
    {
        if (m_CurrencyType == eCurrencyType.Max)
        {
            gameObject.SetActive(false);
            return;
        }

        long amount = GetCurrencyAmount(m_CurrencyType);
        m_AmountText.SetText(amount.ToString());
        gameObject.SetActive(true);
    }

    private long GetCurrencyAmount(eCurrencyType _currencyType)
    {
        return PlayerManager.instance.GetCurrencyAmount(_currencyType);
    }
}

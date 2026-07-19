using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssetBox : UIBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI m_AmountText;
    [SerializeField] private eCurrencyType m_CurrencyType = eCurrencyType.Max;

    private ObservableVariable<int> m_RegisteredObservable;

    protected override void OnEnable()
    {
        base.OnEnable();
        RegisterCurrencyObserver();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        UnregisterCurrencyObserver();
    }

    public void SetData(eCurrencyType _currencyType, long _amount)
    {
        SetCurrencyType(_currencyType);
        m_AmountText.SetText(_amount.ToString());
    }

    public void SetData(eCurrencyType _currencyType)
    {
        SetCurrencyType(_currencyType);

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

    private void SetCurrencyType(eCurrencyType _currencyType)
    {
        if (m_CurrencyType == _currencyType)
            return;

        UnregisterCurrencyObserver();
        m_CurrencyType = _currencyType;

        if (isActiveAndEnabled == true)
            RegisterCurrencyObserver();
    }

    private void RegisterCurrencyObserver()
    {
        if (m_CurrencyType == eCurrencyType.Max)
            return;

        m_RegisteredObservable = PlayerManager.instance.GetCurrencyObservable(m_CurrencyType);
        if (m_RegisteredObservable == null)
            return;

        m_RegisteredObservable.RegisterObserver(OnCurrencyChanged);
    }

    private void UnregisterCurrencyObserver()
    {
        if (m_RegisteredObservable == null)
            return;

        m_RegisteredObservable.UnregisterObserver(OnCurrencyChanged);
        m_RegisteredObservable = null;
    }

    private void OnCurrencyChanged(int _oldAmount, int _newAmount)
    {
        m_AmountText.SetText(_newAmount.ToString());
    }

    private long GetCurrencyAmount(eCurrencyType _currencyType)
    {
        return PlayerManager.instance.GetCurrencyAmount(_currencyType);
    }
}

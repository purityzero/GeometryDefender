using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInGameHUD : UIBase
{
    [SerializeField] private TextMeshProUGUI m_HpText;
    [SerializeField] private TextMeshProUGUI m_TimeText;
    [SerializeField] private TextMeshProUGUI m_KillText;
    [SerializeField] private Image m_XpFillImage;
    [SerializeField] private Image m_HpFillImage;

    private ObservableVariable<int> m_HpObservable;
    private ObservableVariable<int> m_KillObservable;
    private ObservableVariable<int> m_XpObservable;

    private void OnDestroy()
    {
        if (m_HpObservable != null)
            m_HpObservable.UnregisterObserver(OnHpChanged);

        if (m_KillObservable != null)
            m_KillObservable.UnregisterObserver(OnKillChanged);

        if (m_XpObservable != null)
            m_XpObservable.UnregisterObserver(OnXpChanged);
    }

    public override void UpdateLogic()
    {
        UpdateTimeText();
        TryRegisterHpObservable();
        TryRegisterKillObservable();
        TryRegisterXpObservable();
    }

    private void UpdateTimeText()
    {
        if (InGameScene.Current == null || InGameScene.Current.timerManager == null)
            return;

        int totalSeconds = (int)InGameScene.Current.timerManager.elapsedTime;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        m_TimeText.text = $"{minutes:00}:{seconds:00}";
    }

    private void TryRegisterHpObservable()
    {
        if (m_HpObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        m_HpObservable = InGameScene.Current.towerController.currentHp;
        m_HpObservable.RegisterObserver(OnHpChanged);
    }

    private void TryRegisterKillObservable()
    {
        if (m_KillObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.monsterManager == null)
            return;

        m_KillObservable = InGameScene.Current.monsterManager.killCount;
        m_KillObservable.RegisterObserver(OnKillChanged);
    }

    private void OnHpChanged(int _oldValue, int _newValue)
    {
        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        int maxHp = InGameScene.Current.towerController.maxHp;

        m_HpText.text = $"{_newValue}/{maxHp}";

        if (maxHp > 0)
            m_HpFillImage.fillAmount = (float)_newValue / maxHp;
    }

    private void OnKillChanged(int _oldValue, int _newValue)
    {
        m_KillText.text = _newValue.ToString();
    }

    private void TryRegisterXpObservable()
    {
        if (m_XpObservable != null)
            return;

        if (InGameScene.Current == null || InGameScene.Current.xpManager == null)
            return;

        m_XpObservable = InGameScene.Current.xpManager.currentXp;
        m_XpObservable.RegisterObserver(OnXpChanged);
    }

    private void OnXpChanged(int _oldValue, int _newValue)
    {
        if (InGameScene.Current == null || InGameScene.Current.xpManager == null || InGameScene.Current.xpManager.requiredXp <= 0)
            return;

        m_XpFillImage.fillAmount = (float)_newValue / InGameScene.Current.xpManager.requiredXp;
    }

    public void OnClickPauseButton()
    {
        UIManager.instance.Get<UIPause>();
    }

    public void OnClickCheatButton()
    {
        UIManager.instance.Get<UICheatWindow>();
    }
}

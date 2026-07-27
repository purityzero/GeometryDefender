using System.Collections.Generic;
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

    [Header("# 무기 쿨다운")]
    [SerializeField] private Transform m_WeaponCooldownContainer;
    [SerializeField] private GameObject m_WeaponCooldownTemplate;

    private ObservableVariable<int> m_HpObservable;
    private ObservableVariable<int> m_KillObservable;
    private ObservableVariable<int> m_XpObservable;

    private List<Image> m_WeaponCooldownFills = new List<Image>();

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
        UpdateWeaponCooldowns();
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

    // 무기는 카드로 계속 늘어나기만 하므로(줄어들 일 없음), 새로 생긴 무기만큼만 행을 추가로 만들고 나머지는 매 프레임 fillAmount만 갱신 —
    // ActorPlayer.GetWeaponCooldownRatio()는 쿨다운마다 0(방금 발사)→1(재장전 완료)로 차오르는 값이라 XP 게이지와 동일한 방식으로 표현.
    private void UpdateWeaponCooldowns()
    {
        if (InGameScene.Current == null || InGameScene.Current.towerController == null)
            return;

        ActorPlayer towerController = InGameScene.Current.towerController;

        while (m_WeaponCooldownFills.Count < towerController.weaponCount)
        {
            int index = m_WeaponCooldownFills.Count;

            GameObject row = ResUtil.Create(m_WeaponCooldownTemplate, m_WeaponCooldownContainer);
            row.SetActive(true);

            StringTable stringTable = TableManager.instance.GetTable<StringTable>();
            string weaponName = (stringTable != null) ? stringTable.GetString(towerController.GetWeaponNameKey(index)) : towerController.GetWeaponNameKey(index);

            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            label.text = weaponName;

            Image fill = row.transform.Find("Image_GaugeBG/Image_GaugeFill").GetComponent<Image>();
            if (ColorUtility.TryParseHtmlString(towerController.GetWeaponColorHex(index), out Color weaponColor) == true)
                fill.color = weaponColor;

            m_WeaponCooldownFills.Add(fill);
        }

        for (int i = 0; i < m_WeaponCooldownFills.Count; ++i)
        {
            m_WeaponCooldownFills[i].fillAmount = towerController.GetWeaponCooldownRatio(i);
        }
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

using System;
using UnityEngine;

public class UISetting : UIPopup
{
    private static readonly eLanguage[] LANGUAGES = { eLanguage.Korean, eLanguage.English, eLanguage.Chinese, eLanguage.Japanese };
    private static readonly eFpsOption[] FPS_OPTIONS = { eFpsOption.Adaptive, eFpsOption.Fps30, eFpsOption.Fps60 };

    [SerializeField] private ToggleButtonList m_LanguageToggles;
    [SerializeField] private ToggleButtonList m_FpsToggles;

    [SerializeField] private UIToggleButton m_SoundToggle;
    [SerializeField] private UIToggleButton m_HapticToggle;
    [SerializeField] private UIToggleButton m_LeftHandToggle;
    [SerializeField] private UIToggleButton m_EnemyDamageTextToggle;
    [SerializeField] private UIToggleButton m_AllyDamageTextToggle;

    public override void Show()
    {
        base.Show();

        OptionData optionData = PlayerManager.instance.optionData;

        int languageIndex = Array.IndexOf(LANGUAGES, optionData.Language);
        m_LanguageToggles.SetData(m_LanguageToggles.toggleListId, OnClickLanguageToggle, languageIndex);

        int fpsIndex = Array.IndexOf(FPS_OPTIONS, optionData.FpsOption);
        m_FpsToggles.SetData(m_FpsToggles.toggleListId, OnClickFpsToggle, fpsIndex);
        ApplyFpsLabels();

        m_SoundToggle.SetData(optionData.isSoundOn, OnClickSoundToggle);
        m_HapticToggle.SetData(optionData.isHapticOn, OnClickHapticToggle);
        m_LeftHandToggle.SetData(optionData.isLeftHandMode, OnClickLeftHandToggle);
        m_EnemyDamageTextToggle.SetData(optionData.isEnemyDamageTextOn, OnClickEnemyDamageTextToggle);
        m_AllyDamageTextToggle.SetData(optionData.isAllyDamageTextOn, OnClickAllyDamageTextToggle);
    }

    // Fps 항목의 OnText/OffText는 StringTable Key라 ToggleButtonList가 생성 직후엔 Key 문자열 그대로 보임 — 실제 문구로 덮어씀
    private void ApplyFpsLabels()
    {
        ToggleMenuTable toggleMenuTable = TableManager.instance.GetTable<ToggleMenuTable>();
        System.Collections.Generic.List<ToggleMenuRecord> menuRecords = toggleMenuTable.FindAllByToggleListId(m_FpsToggles.toggleListId);
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        for (int i = 0; i < menuRecords.Count; ++i)
        {
            UIToggleButton toggle = m_FpsToggles.GetToggle<UIToggleButton>(i);
            if (toggle == null)
                continue;

            string label = stringTable.GetString(menuRecords[i].OnText);
            toggle.textOn.SetText(label);
            toggle.textOff.SetText(label);
        }
    }

    private void OnClickLanguageToggle(int _index)
    {
        PlayerManager.instance.SetLanguage(LANGUAGES[_index]);
    }

    private void OnClickFpsToggle(int _index)
    {
        PlayerManager.instance.SetFpsOption(FPS_OPTIONS[_index]);
    }

    private void OnClickSoundToggle(UIToggleButton _toggle)
    {
        PlayerManager.instance.SetSoundOn(_toggle.isOn);
    }

    private void OnClickHapticToggle(UIToggleButton _toggle)
    {
        PlayerManager.instance.SetHapticOn(_toggle.isOn);
    }

    private void OnClickLeftHandToggle(UIToggleButton _toggle)
    {
        PlayerManager.instance.SetLeftHandMode(_toggle.isOn);
    }

    private void OnClickEnemyDamageTextToggle(UIToggleButton _toggle)
    {
        PlayerManager.instance.SetEnemyDamageTextOn(_toggle.isOn);
    }

    private void OnClickAllyDamageTextToggle(UIToggleButton _toggle)
    {
        PlayerManager.instance.SetAllyDamageTextOn(_toggle.isOn);
    }
}

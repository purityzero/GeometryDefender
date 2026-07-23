using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class UIPause : UIPopup
{
    [SerializeField] private TextMeshProUGUI m_SoundText;
    [SerializeField] private TextMeshProUGUI m_EnemyDamageTextText;
    [SerializeField] private TextMeshProUGUI m_AllyDamageTextText;
    [SerializeField] private TextMeshProUGUI m_BuildText;
    [SerializeField] private TextMeshProUGUI m_VersionText;

    public override void Show()
    {
        base.Show();

        InGameScene.Current?.SetPaused(true);

        RefreshSoundText();
        RefreshEnemyDamageTextText();
        RefreshAllyDamageTextText();
        RefreshBuildText();

        m_VersionText.text = Application.version;
    }

    public override void Close()
    {
        // 게임오버 상태면 SetPaused(false)를 호출해도 정지 상태가 유지된다(UICardDraft와 동일 이유, 2026-07-24, [[InGameScene]] 참고).
        InGameScene.Current?.SetPaused(false);

        base.Close();
    }

    public void OnClickResumeButton()
    {
        Close();
    }

    public void OnClickRestartButton()
    {
        Time.timeScale = 1f;
        SceneManager.instance.NextScene(EScene.InGameScene.ToString());
    }

    public void OnClickMainMenuButton()
    {
        Time.timeScale = 1f;
        SceneManager.instance.NextScene(EScene.TitleScene.ToString());
    }

    public void OnClickSoundButton()
    {
        PlayerManager.instance.SetSoundOn(PlayerManager.instance.optionData.isSoundOn == false);
        RefreshSoundText();
    }

    public void OnClickEnemyDamageTextButton()
    {
        PlayerManager.instance.SetEnemyDamageTextOn(PlayerManager.instance.optionData.isEnemyDamageTextOn == false);
        RefreshEnemyDamageTextText();
    }

    public void OnClickAllyDamageTextButton()
    {
        PlayerManager.instance.SetAllyDamageTextOn(PlayerManager.instance.optionData.isAllyDamageTextOn == false);
        RefreshAllyDamageTextText();
    }

    private void RefreshSoundText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        bool isSoundOn = PlayerManager.instance.optionData.isSoundOn;
        string stateText = (isSoundOn == true) ? stringTable.GetString("SettingsOn") : stringTable.GetString("SettingsOff");

        m_SoundText.text = $"{stringTable.GetString("SettingsSoundLabel")}: {stateText}";
    }

    private void RefreshEnemyDamageTextText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        bool isEnemyDamageTextOn = PlayerManager.instance.optionData.isEnemyDamageTextOn;
        string stateText = (isEnemyDamageTextOn == true) ? stringTable.GetString("SettingsOn") : stringTable.GetString("SettingsOff");

        m_EnemyDamageTextText.text = $"{stringTable.GetString("SettingsEnemyDamageTextLabel")}: {stateText}";
    }

    private void RefreshAllyDamageTextText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();
        bool isAllyDamageTextOn = PlayerManager.instance.optionData.isAllyDamageTextOn;
        string stateText = (isAllyDamageTextOn == true) ? stringTable.GetString("SettingsOn") : stringTable.GetString("SettingsOff");

        m_AllyDamageTextText.text = $"{stringTable.GetString("SettingsAllyDamageTextLabel")}: {stateText}";
    }

    // 07_ui.html "CURRENT BUILD" — 타겟팅 우선순위 + 지금까지 획득한 카드 요약(중복은 xN으로 묶어서 표시)
    private void RefreshBuildText()
    {
        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        if (InGameScene.Current == null || InGameScene.Current.towerController == null || InGameScene.Current.cardManager == null)
        {
            m_BuildText.text = string.Empty;
            return;
        }

        string targetingName = GetTargetingDisplayName(stringTable, InGameScene.Current.towerController.currentTargetingType);
        string targetingLine = $"{stringTable.GetString("PauseBuildTargetingLabel")}: {targetingName}";

        string cardListLine = BuildCardListText(stringTable);

        m_BuildText.text = $"{targetingLine}\n\n{stringTable.GetString("PauseBuildCardListLabel")}\n{cardListLine}";
    }

    private string GetTargetingDisplayName(StringTable _stringTable, eTargetingType _targetingType)
    {
        switch (_targetingType)
        {
            case eTargetingType.Strongest:
                return _stringTable.GetString("TargetingStrongest");
            case eTargetingType.Weakest:
                return _stringTable.GetString("TargetingWeakest");
            case eTargetingType.Fastest:
                return _stringTable.GetString("TargetingFastest");
            case eTargetingType.Random:
                return _stringTable.GetString("TargetingRandom");
            default:
                return _stringTable.GetString("TargetingClosest");
        }
    }

    private string BuildCardListText(StringTable _stringTable)
    {
        IReadOnlyList<int> obtainedCardIds = InGameScene.Current.cardManager.obtainedCardIds;
        if (obtainedCardIds.Count <= 0)
            return _stringTable.GetString("PauseBuildNoCards");

        CardTable cardTable = TableManager.instance.GetTable<CardTable>();

        Dictionary<int, int> cardCountById = new Dictionary<int, int>();
        List<int> orderedCardIds = new List<int>();
        for (int i = 0; i < obtainedCardIds.Count; ++i)
        {
            int cardId = obtainedCardIds[i];
            if (cardCountById.ContainsKey(cardId) == false)
            {
                cardCountById[cardId] = 0;
                orderedCardIds.Add(cardId);
            }

            cardCountById[cardId]++;
        }

        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < orderedCardIds.Count; ++i)
        {
            int cardId = orderedCardIds[i];
            CardRecord cardRecord = cardTable.GetRecordById(cardId);
            if (cardRecord == null)
                continue;

            if (stringBuilder.Length > 0)
                stringBuilder.Append(", ");

            stringBuilder.Append(_stringTable.GetString(cardRecord.NameKey));

            int count = cardCountById[cardId];
            if (count > 1)
                stringBuilder.Append($" x{count}");
        }

        return stringBuilder.ToString();
    }
}

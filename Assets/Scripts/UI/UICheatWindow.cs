using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Assets/Editor/QA/TimeScaleWindow, CombatDebugWindow, MonsterSpawnTestWindow의 런타임(빌드 포함) 버전.
// 에디터 전용 창들은 UnityEditor 의존이라 빌드에 안 들어가서, 같은 기능을 uGUI 팝업으로 이식했다.
// (PlayerDataResetWindow의 세이브 초기화는 파괴적 동작이라 이식 대상에서 제외 — 사용자 확정)
public class UICheatWindow : UIPopup
{
    private static readonly int[] QUICK_SKIP_SECONDS = { 10, 30, 60 };
    private static readonly int[] SPAWN_COUNTS = { 10, 50, 150, 300 };
    private static readonly eEnemyVariant[] VARIANTS = { eEnemyVariant.Normal, eEnemyVariant.Elite, eEnemyVariant.Boss };

    [Header("# 시간 배속")]
    [SerializeField] private Button[] m_TimeScaleButtons;
    [SerializeField] private TextMeshProUGUI m_TimeScaleText;

    [Header("# 웨이브 스킵")]
    [SerializeField] private Button[] m_QuickSkipButtons;
    [SerializeField] private Transform m_WaveButtonContainer;
    [SerializeField] private Button m_WaveButtonTemplate;

    [Header("# 치명타")]
    [SerializeField] private Button m_CritChanceButton;
    [SerializeField] private Button m_CritMultiplierButton;

    [Header("# 몬스터 스폰")]
    [SerializeField] private Button[] m_VariantToggleButtons;
    [SerializeField] private Button[] m_SpawnCountButtons;

    [Header("# 카드 즉시 적용")]
    [SerializeField] private Transform m_CardListContainer;
    [SerializeField] private GameObject m_CardRowTemplate;

    [Header("# 메타트리")]
    [SerializeField] private Button m_AddShardButton;
    [SerializeField] private Button m_ResetMetaTreeButton;
    [SerializeField] private Transform m_MetaTreeListContainer;
    [SerializeField] private GameObject m_MetaTreeRowTemplate;

    [Header("# 닫기")]
    [SerializeField] private Button m_CloseButton;

    private static readonly Color COLOR_VARIANT_ON = ColorUtil.GetColorHtml("00E5FF");
    private static readonly Color COLOR_VARIANT_OFF = ColorUtil.GetColorHtml("606078");

    private readonly bool[] m_isVariantIncluded = { true, true, true };
    private bool m_isListBuilt;

    public override void Show()
    {
        base.Show();

        InGameScene.Current?.SetPaused(true);

        BuildDynamicLists();
        WireStaticButtons();
    }

    public override void Close()
    {
        InGameScene.Current?.SetPaused(false);

        base.Close();
    }

    public override void UpdateLogic()
    {
        m_TimeScaleText.text = $"현재 배속: {Time.timeScale:0.0}x";
    }

    private void WireStaticButtons()
    {
        m_CloseButton.onClick.RemoveAllListeners();
        m_CloseButton.onClick.AddListener(Close);

        for (int index = 0; index < m_TimeScaleButtons.Length; ++index)
        {
            float timeScale = index + 1;
            m_TimeScaleButtons[index].onClick.RemoveAllListeners();
            m_TimeScaleButtons[index].onClick.AddListener(() => Time.timeScale = timeScale);
        }

        for (int index = 0; index < m_QuickSkipButtons.Length; ++index)
        {
            float seconds = QUICK_SKIP_SECONDS[index];
            m_QuickSkipButtons[index].onClick.RemoveAllListeners();
            m_QuickSkipButtons[index].onClick.AddListener(() => SkipTime(seconds));
        }

        m_CritChanceButton.onClick.RemoveAllListeners();
        m_CritChanceButton.onClick.AddListener(() => InGameScene.Current.towerController.AddCardCritChance(100f));

        m_CritMultiplierButton.onClick.RemoveAllListeners();
        m_CritMultiplierButton.onClick.AddListener(() => InGameScene.Current.towerController.AddCardCritMultiplier(1f));

        for (int index = 0; index < m_VariantToggleButtons.Length; ++index)
        {
            int variantIndex = index;
            SetVariantToggleVisual(variantIndex);

            m_VariantToggleButtons[variantIndex].onClick.RemoveAllListeners();
            m_VariantToggleButtons[variantIndex].onClick.AddListener(() => OnClickVariantToggle(variantIndex));
        }

        for (int index = 0; index < m_SpawnCountButtons.Length; ++index)
        {
            int spawnCount = SPAWN_COUNTS[index];
            m_SpawnCountButtons[index].onClick.RemoveAllListeners();
            m_SpawnCountButtons[index].onClick.AddListener(() => SpawnMonsters(spawnCount));
        }

        m_AddShardButton.onClick.RemoveAllListeners();
        m_AddShardButton.onClick.AddListener(() => PlayerManager.instance.AddCurrency(eCurrencyType.Shard, 1000));

        m_ResetMetaTreeButton.onClick.RemoveAllListeners();
        m_ResetMetaTreeButton.onClick.AddListener(() => PlayerManager.instance.ResetMetaTreeAndShard());
    }

    private void SkipTime(float _seconds)
    {
        InGameScene.Current.timerManager.AddElapsedTime(_seconds);
        InGameScene.Current.spawnManager.AddElapsedTime(_seconds);
    }

    private void OnClickVariantToggle(int _variantIndex)
    {
        m_isVariantIncluded[_variantIndex] = m_isVariantIncluded[_variantIndex] == false;
        SetVariantToggleVisual(_variantIndex);
    }

    private void SetVariantToggleVisual(int _variantIndex)
    {
        bool isIncluded = m_isVariantIncluded[_variantIndex];
        Image buttonImage = m_VariantToggleButtons[_variantIndex].image;
        buttonImage.color = (isIncluded == true) ? COLOR_VARIANT_ON : COLOR_VARIANT_OFF;
    }

    private void SpawnMonsters(int _spawnCount)
    {
        EnemyTable enemyTable = TableManager.instance.GetTable<EnemyTable>();
        if (enemyTable == null)
        {
            Logger.Error($"[UICheatWindow] SpawnMonsters Failed! EnemyTable not loaded");
            return;
        }

        List<EnemyRecord> candidateList = new();
        for (int index = 0; index < VARIANTS.Length; ++index)
        {
            if (m_isVariantIncluded[index] == false)
                continue;

            if (enemyTable.dicVariant.ContainsKey(VARIANTS[index]) == false)
                continue;

            candidateList.AddRange(enemyTable.dicVariant[VARIANTS[index]]);
        }

        if (candidateList.Count <= 0)
        {
            Logger.Error($"[UICheatWindow] SpawnMonsters Failed! 포함된 Variant가 없습니다");
            return;
        }

        for (int spawnIndex = 0; spawnIndex < _spawnCount; ++spawnIndex)
        {
            EnemyRecord record = candidateList[Random.Range(0, candidateList.Count)];
            InGameScene.Current.monsterManager.Spawn(record);
        }
    }

    private void BuildDynamicLists()
    {
        if (m_isListBuilt == true)
            return;

        m_isListBuilt = true;

        BuildWaveButtonList();
        BuildCardButtonList();
        BuildMetaTreeButtonList();
    }

    private void BuildWaveButtonList()
    {
        WaveTable waveTable = TableManager.instance.GetTable<WaveTable>();
        if (waveTable == null)
        {
            Logger.Error($"[UICheatWindow] BuildWaveButtonList Failed! WaveTable not loaded");
            return;
        }

        foreach (WaveRecord record in waveTable.list)
        {
            Button waveButton = ResUtil.Create(m_WaveButtonTemplate, m_WaveButtonContainer);
            waveButton.gameObject.SetActive(true);

            TextMeshProUGUI waveButtonText = waveButton.GetComponentInChildren<TextMeshProUGUI>();
            waveButtonText.text = $"Wave {record.Id} ({record.StartTime}s)";

            waveButton.onClick.AddListener(() => SkipTime(record.StartTime - InGameScene.Current.timerManager.elapsedTime));
        }
    }

    private void BuildCardButtonList()
    {
        CardTable cardTable = TableManager.instance.GetTable<CardTable>();
        if (cardTable == null)
        {
            Logger.Error($"[UICheatWindow] BuildCardButtonList Failed! CardTable not loaded");
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        foreach (CardRecord record in cardTable.list)
        {
            GameObject cardRowObject = ResUtil.Create(m_CardRowTemplate, m_CardListContainer);
            cardRowObject.SetActive(true);

            string cardName = (stringTable != null) ? stringTable.GetString(record.NameKey) : record.NameKey;

            TextMeshProUGUI cardNameText = cardRowObject.GetComponentInChildren<TextMeshProUGUI>();
            cardNameText.text = $"[{record.Rarity}] {cardName}";

            Button applyButton = cardRowObject.GetComponentInChildren<Button>();
            applyButton.onClick.AddListener(() => InGameScene.Current.cardManager.ApplyCard(record));
        }
    }

    // 정상 해금 흐름(UIMetaTree.OnClickNode)의 선행조건/Shard 소모를 건너뛰고 즉시 해금 — 치트 전용.
    private void BuildMetaTreeButtonList()
    {
        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        if (metaTreeTable == null)
        {
            Logger.Error($"[UICheatWindow] BuildMetaTreeButtonList Failed! MetaTreeTable not loaded");
            return;
        }

        StringTable stringTable = TableManager.instance.GetTable<StringTable>();

        foreach (MetaTreeRecord record in metaTreeTable.list)
        {
            GameObject metaTreeRowObject = ResUtil.Create(m_MetaTreeRowTemplate, m_MetaTreeListContainer);
            metaTreeRowObject.SetActive(true);

            string nodeName = (stringTable != null) ? stringTable.GetString(record.DisplayName) : record.DisplayName;

            TextMeshProUGUI nodeNameText = metaTreeRowObject.GetComponentInChildren<TextMeshProUGUI>();
            nodeNameText.text = $"[{record.Id}] {nodeName} ({record.Cost} Shard)";

            Button unlockButton = metaTreeRowObject.GetComponentInChildren<Button>();
            unlockButton.onClick.AddListener(() => PlayerManager.instance.UnlockMetaNode(record.Id));
        }
    }
}

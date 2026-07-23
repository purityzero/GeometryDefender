using UnityEngine;

// Assets/Design/04_card.html "드래프트 흐름"/"레벨업 곡선" — 몬스터 처치 시 XP 획득, 레벨업 시 카드 드래프트 오픈 신호만 준다(일시정지/카드 로직은 UICardDraft 소유)
public class XpManager : UpdatableBehaviour
{
    public int currentLevel { get; private set; }
    public ObservableVariable<int> currentXp { get; } = new ObservableVariable<int>(0);
    public int requiredXp { get; private set; }

    // 한 몬스터로 여러 레벨을 동시에 넘길 때, UICardDraft가 전부 소비할 때까지 남은 드래프트 횟수
    public int pendingLevelUps { get; private set; }

    private float m_XpMultiplier = 1f;

    // 핫 리로드 시 이벤트 구독이 풀리는 걸 막기 위한 보존 제외 플래그(MonsterManager/SpawnManager와 동일 이유)
    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        currentLevel = 1;
        currentXp.Value = 0;
        requiredXp = CalculateRequiredXp(currentLevel);
        pendingLevelUps = 0;

        MetaTreeTable metaTreeTable = TableManager.instance.GetTable<MetaTreeTable>();
        int xpPercent = (metaTreeTable != null) ? metaTreeTable.GetTotalEffectValue(eMetaEffectType.XpPercent, PlayerManager.instance.playerData.UnlockedMetaNodes) : 0;
        m_XpMultiplier = 1f + (xpPercent / 100f);

        if (InGameScene.Current.monsterManager == null)
        {
            Logger.Error($"[XpManager] Init Failed! InGameScene.Current.monsterManager is null - MonsterManager.Init() 선행 필요");
            return;
        }

        InGameScene.Current.monsterManager.OnMonsterDie += OnMonsterKilled;

        m_isInitialized = true;
    }

    private void OnMonsterKilled(RewardData _reward)
    {
        AddXp(_reward.XpReward);
    }

    public void AddXp(int _amount)
    {
        int finalAmount = Mathf.RoundToInt(_amount * m_XpMultiplier);
        currentXp.Value += finalAmount;

        while (currentXp.Value >= requiredXp)
        {
            currentXp.Value -= requiredXp;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        requiredXp = CalculateRequiredXp(currentLevel);
        pendingLevelUps++;

        // 여러 레벨을 동시에 넘겨도 UICardDraft는 한 번만 연다 — 나머지는 ConsumePendingLevelUp()으로 순차 소비됨
        if (pendingLevelUps == 1)
            UIManager.instance.Get<UICardDraft>();
    }

    public void ConsumePendingLevelUp()
    {
        if (pendingLevelUps > 0)
            pendingLevelUps--;
    }

    private int CalculateRequiredXp(int _level)
    {
        float value = GameConfigTable.XP_REQUIRED_BASE
            + _level * GameConfigTable.XP_REQUIRED_LINEAR
            + _level * _level * GameConfigTable.XP_REQUIRED_QUADRATIC;

        return Mathf.RoundToInt(value);
    }

    private void OnDestroy()
    {
        if (m_isInitialized == false)
            return;

        if (InGameScene.Current != null && InGameScene.Current.monsterManager != null)
            InGameScene.Current.monsterManager.OnMonsterDie -= OnMonsterKilled;
    }
}

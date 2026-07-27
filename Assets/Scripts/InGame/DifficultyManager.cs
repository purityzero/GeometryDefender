using System;
using UnityEngine;

// Assets/Design/08_balance.html "난이도 진행 (Normal → Hard → Hell → Infinite)"
public enum eDifficultyLevel
{
    Normal,
    Hard,
    Hell,
    Infinite
}

public class DifficultyManager : UpdatableBehaviour
{
    // 난이도 선택 UI가 아직 없어 기본값 Normal 고정 — 선택 화면이 생기면 InGameScene 진입 전 이 값만 설정해주면 된다
    public static eDifficultyLevel SelectedDifficulty = eDifficultyLevel.Normal;

    // Infinite를 제외한 난이도는 마지막 웨이브 시각에 도달하면 런이 종료된다(사용자 요청: "인피니티 난이도 이전까지는
    // 난이도 클리어 Popup 만들어서 정산해주고") — InGameScene이 구독해 UIRunOver를 띄우고 런을 정지시킨다.
    public event Action OnCleared;

    private WaveTable m_WaveTable;
    private DifficultyTable m_DifficultyTable;
    private DifficultyRecord m_CurrentRecord;
    private bool m_isCleared;

    public eDifficultyLevel currentDifficulty { get; private set; }

    // 핫 리로드 시 WaveTable 참조는 default로 리셋되지만 이 bool은 값이 보존됨 — MonsterManager/SpawnManager와 동일 이유로 보존 대상에서 제외
    [System.NonSerialized] private bool m_isInitialized;

    public void Init()
    {
        m_WaveTable = TableManager.instance.GetTable<WaveTable>();
        m_DifficultyTable = TableManager.instance.GetTable<DifficultyTable>();
        if (m_WaveTable == null || m_DifficultyTable == null)
        {
            Logger.Error($"[DifficultyManager] Init Failed! WaveTable/DifficultyTable not loaded - TableManager.init() 선행 필요");
            return;
        }

        currentDifficulty = SelectedDifficulty;

        m_CurrentRecord = m_DifficultyTable.GetRecordByLevel(currentDifficulty);
        if (m_CurrentRecord == null)
        {
            Logger.Error($"[DifficultyManager] Init Failed! DifficultyRecord not found - {currentDifficulty}");
            return;
        }

        m_isCleared = false;

        m_isInitialized = true;
    }

    public override void UpdateLogic()
    {
        if (m_isInitialized == false)
            return;

        if (m_isCleared == true)
            return;

        if (InGameScene.Current.timerManager == null)
            return;

        if (InGameScene.Current.timerManager.elapsedTime < m_WaveTable.GetFinalPhaseStartTime())
            return;

        m_isCleared = true;
        UnlockNextDifficulty();

        // Infinite는 클리어 개념이 없음 — 팝업 없이 계속 진행(난이도 배율은 GetInfiniteStepCount()로 계속 상승)
        if (currentDifficulty == eDifficultyLevel.Infinite)
            return;

        OnCleared?.Invoke();
    }

    private void UnlockNextDifficulty()
    {
        eDifficultyLevel? nextLevel = m_DifficultyTable.GetNextLevel(currentDifficulty);
        if (nextLevel == null)
            return;

        PlayerManager.instance.UnlockDifficulty(nextLevel.Value);
    }

    // 스폰/적HP 배율 (DifficultyTable.csv "DifficultyMultiplier" 컬럼)
    public float GetDifficultyMultiplier()
    {
        if (m_isInitialized == false)
            return 1f;

        return m_CurrentRecord.DifficultyMultiplier + GetInfiniteStepCount() * m_CurrentRecord.InfiniteStepAmount;
    }

    // Shards 배율 (호출 시점의 경과 시간 기준 — 런 종료 시 UIRunOver가 사망 시점에 호출)
    public float GetShardMultiplier()
    {
        if (m_isInitialized == false)
            return 1f;

        return m_CurrentRecord.ShardMultiplier + GetInfiniteStepCount() * m_CurrentRecord.InfiniteStepAmount;
    }

    private float GetInfiniteStepCount()
    {
        if (m_CurrentRecord.InfiniteStepSeconds <= 0f)
            return 0f;

        float finalStartTime = m_WaveTable.GetFinalPhaseStartTime();
        float elapsed = (InGameScene.Current.timerManager != null) ? InGameScene.Current.timerManager.elapsedTime : finalStartTime;
        float overtime = Mathf.Max(0f, elapsed - finalStartTime);

        return Mathf.Floor(overtime / m_CurrentRecord.InfiniteStepSeconds);
    }
}

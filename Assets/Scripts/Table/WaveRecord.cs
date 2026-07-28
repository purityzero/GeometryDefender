using System.Collections.Generic;

public class WaveRecord : Record
{
    public int StartTime;
    public int Duration;
    public int NormalWeight;
    public int SwiftWeight;
    public int HeavyWeight;
    public int SplitterWeight;
    public int RangedWeight;
    public float EliteChance;
}

public class WaveTable : Table<WaveRecord>
{
    public WaveTable(List<WaveRecord> _listRecord) : base(_listRecord) { }

    public WaveRecord GetActivePhase(int _elapsedSeconds)
    {
        WaveRecord activeRecord = null;
        for (int i = 0; i < list.Count; ++i)
        {
            if (list[i].StartTime <= _elapsedSeconds)
                activeRecord = list[i];
        }
        return activeRecord;
    }

    // Infinite 난이도 배율 증가 기준 시각(DifficultyManager.GetInfiniteStepCount())에 사용 — 마지막으로 정의된 웨이브 시작 시각
    public int GetFinalPhaseStartTime()
    {
        return list[list.Count - 1].StartTime;
    }

    // 난이도 클리어 판정(DifficultyManager)에 사용 — 마지막 웨이브가 끝나는 시각(시작 시각 + 지속 시간)
    public int GetFinalPhaseEndTime()
    {
        WaveRecord finalPhase = list[list.Count - 1];
        return finalPhase.StartTime + finalPhase.Duration;
    }
}

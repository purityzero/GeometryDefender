using System.Collections.Generic;

public class WaveRecord : Record
{
    public int StartTime;
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
}

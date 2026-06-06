using System.Collections.Generic;

public class WaveRecord : Record
{
    public int NormalCount;
    public int SwiftCount;
    public int HeavyCount;
    public float SpawnInterval;
    public int ClearBonus;
}

public class WaveTable : Table<WaveRecord>
{
    public WaveTable(List<WaveRecord> listRecord) : base(listRecord) { }
}

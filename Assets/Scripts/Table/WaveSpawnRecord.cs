using System.Collections.Generic;

public class WaveSpawnRecord : Record
{
    public int WaveId;
    public int SpawnOrder;
    public int EnemyId;
}

public class WaveSpawnTable : Table<WaveSpawnRecord>
{
    public WaveSpawnTable(List<WaveSpawnRecord> listRecord) : base(listRecord) { }
}

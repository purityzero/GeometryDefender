using System.Collections.Generic;

public class TowerSlotRecord : Record
{
    public float X;
    public float Y;
    public float Z;
}

public class TowerSlotTable : Table<TowerSlotRecord>
{
    public TowerSlotTable(List<TowerSlotRecord> listRecord) : base(listRecord) { }
}

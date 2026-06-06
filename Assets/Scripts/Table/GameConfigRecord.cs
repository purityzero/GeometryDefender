using System.Collections.Generic;

public class GameConfigRecord : Record
{
    public string DisplayName;
    public float Value;
}

public class GameConfigTable : Table<GameConfigRecord>
{
    public GameConfigTable(List<GameConfigRecord> listRecord) : base(listRecord) { }
}

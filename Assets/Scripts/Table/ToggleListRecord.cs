using System.Collections.Generic;

public class ToggleListRecord : Record
{
    public string ToggleListId;
    public string PrefabPath;
}

public class ToggleListTable : Table<ToggleListRecord>
{
    public ToggleListTable(List<ToggleListRecord> _listRecord) : base(_listRecord) { }

    public ToggleListRecord GetRecordByToggleListId(string _toggleListId)
    {
        return list.Find(record => record.ToggleListId == _toggleListId);
    }
}

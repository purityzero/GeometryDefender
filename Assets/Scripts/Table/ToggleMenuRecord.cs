using System.Collections.Generic;

public class ToggleMenuRecord : Record
{
    public string ToggleListId;
    public string OnText;
    public string OffText;
    public string OnImagePath;
    public string OffImagePath;
    public string OnColor;
}

public class ToggleMenuTable : Table<ToggleMenuRecord>
{
    public ToggleMenuTable(List<ToggleMenuRecord> _listRecord) : base(_listRecord) { }

    public List<ToggleMenuRecord> FindAllByToggleListId(string _toggleListId)
    {
        return list.FindAll(record => record.ToggleListId == _toggleListId);
    }
}

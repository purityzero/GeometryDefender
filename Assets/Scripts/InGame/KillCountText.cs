public class KillCountText : ObservableIntText<MonsterManager>
{
    protected override MonsterManager GetSource()
    {
        return MonsterManager.Current;
    }

    protected override ObservableVariable<int> GetObservable(MonsterManager _source)
    {
        return _source.killCount;
    }

    protected override string Format(int _value)
    {
        return _value.ToString();
    }
}

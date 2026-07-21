public class TowerHealthText : ObservableIntText<TowerHealth>
{
    protected override TowerHealth GetSource()
    {
        return TowerHealth.Current;
    }

    protected override ObservableVariable<int> GetObservable(TowerHealth _source)
    {
        return _source.currentHp;
    }

    protected override string Format(int _value)
    {
        return $"{_value}/{TowerHealth.Current.maxHp}";
    }
}

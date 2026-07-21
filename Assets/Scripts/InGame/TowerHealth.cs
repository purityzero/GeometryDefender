using System;
using UnityEngine;

public class TowerHealth : SceneSingleton<TowerHealth>
{
    public event Action OnDie;

    private int m_MaxHp;

    public int maxHp { get { return m_MaxHp; } }
    public ObservableVariable<int> currentHp { get; } = new ObservableVariable<int>(0);

    public void Init(int _maxHp)
    {
        m_MaxHp = _maxHp;
        currentHp.Value = _maxHp;
    }

    public void TakeDamage(int _amount)
    {
        if (currentHp.Value <= 0)
            return;

        int newHp = currentHp.Value - _amount;

        if (newHp < 0)
            newHp = 0;

        currentHp.Value = newHp;

        Logger.Log($"[TowerHealth] TakeDamage - amount:{_amount}, currentHp:{currentHp.Value}/{m_MaxHp}");

        if (currentHp.Value <= 0)
            OnDie?.Invoke();
    }

    public void OnEnemyReachTower(RewardData _reward)
    {
        TakeDamage(_reward.DamageToBase);
    }
}

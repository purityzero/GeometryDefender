using UnityEngine;

public class TowerColorEffect : MonoBehaviour, IUpdatable
{
    private const float COLOR_TWEEN_DURATION = 0.5f;
    private const float MID_HP_RATIO = 0.7f;
    private const float LOW_HP_RATIO = 0.3f;

    [SerializeField] private SpriteRenderer m_SpriteRenderer;

    // 02_combat.html "타워 HP 시각 표현" 기준 (100~70% Cyan / 70~30% Dim Cyan / 30~0% Red)
    [SerializeField] private Color m_HighHpColor = new Color(0f, 0.8980392f, 1f);
    [SerializeField] private Color m_MidHpColor = new Color(0f, 0.7372549f, 0.83137256f);
    [SerializeField] private Color m_LowHpColor = new Color(1f, 0.2f, 0.33333334f);

    private ObservableVariable<int> m_RegisteredObservable;

    private void Start()
    {
        // SpriteRenderer.color(표준 틴트)가 프리팹에 시안으로 baked돼 있어 material._Color와 곱해져 렌더링됨
        // — 이후 색 전환은 material.color만 트윈하므로, 틴트를 흰색으로 고정해 곱 연산이 결과를 왜곡하지 않게 함
        m_SpriteRenderer.color = Color.white;

        BaseScene.Current.Register(this);
    }

    private void OnDestroy()
    {
        BaseScene.Current?.Unregister(this);

        if (m_RegisteredObservable != null)
            m_RegisteredObservable.UnregisterObserver(OnHpChanged);
    }

    public void UpdateLogic()
    {
        if (m_RegisteredObservable != null)
            return;

        if (TowerHealth.Current == null)
            return;

        m_RegisteredObservable = TowerHealth.Current.currentHp;
        m_RegisteredObservable.RegisterObserver(OnHpChanged);
    }

    private void OnHpChanged(int _oldValue, int _newValue)
    {
        if (TowerHealth.Current == null || TowerHealth.Current.maxHp <= 0)
            return;

        float hpRatio = (float)_newValue / TowerHealth.Current.maxHp;
        Color targetColor = GetColorForRatio(hpRatio);

        TweenUtil.Color(m_SpriteRenderer.material, targetColor, COLOR_TWEEN_DURATION);
    }

    private Color GetColorForRatio(float _hpRatio)
    {
        if (_hpRatio <= LOW_HP_RATIO)
            return m_LowHpColor;

        if (_hpRatio <= MID_HP_RATIO)
            return m_MidHpColor;

        return m_HighHpColor;
    }
}

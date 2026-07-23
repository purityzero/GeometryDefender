using DG.Tweening;
using UnityEngine;

public class TowerColorEffect : UpdatableBehaviour
{
    private enum eHpTier
    {
        High,
        Mid,
        Low
    }

    private const string GLOW_AMOUNT_PROPERTY = "_GlowAmount";

    [SerializeField] private SpriteRenderer m_SpriteRenderer;

    // 02_combat.html "타워 HP 시각 표현" 기준 (100~70% Cyan / 70~30% Dim Cyan / 30~0% Red)
    [SerializeField] private Color m_HighHpColor = new Color(0f, 0.8980392f, 1f);
    [SerializeField] private Color m_MidHpColor = new Color(0f, 0.7372549f, 0.83137256f);
    [SerializeField] private Color m_LowHpColor = new Color(1f, 0.2f, 0.33333334f);

    // 02_combat.html 글로우 강도 CSS 참고치(8+20 / 4+10 / 6+14) 비율 근사치 — Low는 정적값 대신 펄스 점멸
    [SerializeField] private float m_HighGlowAmount = 1f;
    [SerializeField] private float m_MidGlowAmount = 0.5f;
    [SerializeField] private float m_LowPulseMinGlowAmount = 0.15f;
    [SerializeField] private float m_LowPulseMaxGlowAmount = 0.7f;

    private ObservableVariable<int> m_RegisteredObservable;
    private eHpTier? m_CurrentTier;
    private Tween m_GlowTween;

    private void Start()
    {
        // SpriteRenderer.color(표준 틴트)가 프리팹에 시안으로 baked돼 있어 material._Color와 곱해져 렌더링됨
        // — 이후 색 전환은 material.color만 트윈하므로, 틴트를 흰색으로 고정해 곱 연산이 결과를 왜곡하지 않게 함
        m_SpriteRenderer.color = Color.white;
    }

    private void OnDestroy()
    {
        if (m_RegisteredObservable != null)
            m_RegisteredObservable.UnregisterObserver(OnHpChanged);

        m_GlowTween?.Kill();
    }

    public override void UpdateLogic()
    {
        if (m_RegisteredObservable != null)
            return;

        if (InGameScene.Current.towerController == null)
            return;

        m_RegisteredObservable = InGameScene.Current.towerController.currentHp;
        m_RegisteredObservable.RegisterObserver(OnHpChanged);
    }

    private void OnHpChanged(int _oldValue, int _newValue)
    {
        if (InGameScene.Current.towerController == null || InGameScene.Current.towerController.maxHp <= 0)
            return;

        float hpRatio = (float)_newValue / InGameScene.Current.towerController.maxHp;
        eHpTier targetTier = GetTierForRatio(hpRatio);

        if (m_CurrentTier != null && m_CurrentTier.Value == targetTier)
            return;

        m_CurrentTier = targetTier;

        Material material = m_SpriteRenderer.material;

        TweenUtil.Color(material, GetColorForTier(targetTier), GameConfigTable.TOWER_COLOR_TWEEN_DURATION);
        ApplyGlowForTier(material, targetTier);
    }

    private eHpTier GetTierForRatio(float _hpRatio)
    {
        if (_hpRatio <= GameConfigTable.TOWER_LOW_HP_RATIO)
            return eHpTier.Low;

        if (_hpRatio <= GameConfigTable.TOWER_MID_HP_RATIO)
            return eHpTier.Mid;

        return eHpTier.High;
    }

    private Color GetColorForTier(eHpTier _tier)
    {
        if (_tier == eHpTier.Low)
            return m_LowHpColor;

        if (_tier == eHpTier.Mid)
            return m_MidHpColor;

        return m_HighHpColor;
    }

    // 30% 이하(Low)는 정적 글로우 대신 min~max 사이를 무한 반복하는 펄스 점멸로 표현(02_combat.html "적색 펄스가 점멸")
    private void ApplyGlowForTier(Material _material, eHpTier _tier)
    {
        m_GlowTween?.Kill();

        if (_tier == eHpTier.Low)
        {
            _material.SetFloat(GLOW_AMOUNT_PROPERTY, m_LowPulseMinGlowAmount);
            m_GlowTween = TweenUtil.Float(_material, GLOW_AMOUNT_PROPERTY, m_LowPulseMaxGlowAmount, GameConfigTable.TOWER_LOW_PULSE_DURATION)
                .SetLoops(-1, LoopType.Yoyo);
            return;
        }

        float targetGlowAmount = (_tier == eHpTier.Mid) ? m_MidGlowAmount : m_HighGlowAmount;
        m_GlowTween = TweenUtil.Float(_material, GLOW_AMOUNT_PROPERTY, targetGlowAmount, GameConfigTable.TOWER_GLOW_TWEEN_DURATION);
    }
}

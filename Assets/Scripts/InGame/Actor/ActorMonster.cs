using UnityEngine;

public class ActorMonster : Actor
{
    // 2026-07-30 — 사용자 요청("냉기오브에서 슬로우가 좀 걸리는게 눈에 띄었으면 좋겠고") — Frost Orb Turret 슬로우 판정 중인
    // 몬스터를 원래 색과 섞은 프로스트 블루로 틴트해 시각적으로 구분되게 한다.
    private static readonly Color SLOW_TINT_COLOR = new Color(0.4f, 0.8f, 1f);
    private const float SLOW_TINT_BLEND = 0.5f;

    [SerializeField] private Renderer m_Renderer;
    [SerializeField] private Renderer m_GlowRenderer;
    [SerializeField] private CullingObject m_CullingObject;
    private EnemyRecord m_Record;
    private bool m_isSlowTinted;

    public void UpdateCullingLogic()
    {
        if (m_CullingObject == null)
            return;

        m_CullingObject.UpdateLogic();
    }

    public void SetColor(Color _color)
    {
        // ColorUtility.TryParseHtmlString()는 sRGB(디자이너가 보는 hex 그대로) 기준 색을 돌려주는데,
        // 이 프로젝트는 Linear 색공간이라 material.color(=_Color 셰이더 프로퍼티)에 그대로 넣으면
        // 감마 보정이 두 번(혹은 0번) 걸려 실제 화면 색이 hex보다 밝고 채도 낮게(물빠진 색으로) 나온다.
        // .linear로 변환해 넣어야 화면에 hex 그대로 보인다.
        m_Renderer.material.color = _color.linear;

        if (m_GlowRenderer != null)
            m_GlowRenderer.material.color = _color.linear;
    }

    public void SetColor(string _colorHex)
    {
        if (ColorUtility.TryParseHtmlString(_colorHex, out Color color) == true)
            SetColor(color);
    }

    public void Open(EnemyRecord _record)
    {
        m_Record = _record;
        m_isSlowTinted = false; // 풀링 재사용 대비 — 이전 사용자가 슬로우 틴트 상태로 반납됐을 수 있음
        SetColor(_record.ColorHex);
        Open();
    }

    // Frost Orb Turret 슬로우 판정 결과를 매 프레임 반영(MonsterManager.ApplySlowAura 참고) — 원래 종족 색(m_Record.ColorHex)
    // 기준으로 매번 다시 섞어 계산하므로, 반복 호출해도 색이 계속 짙어지는 등의 누적 오염이 없다.
    public void SetSlowTinted(bool _isSlowed)
    {
        if (m_isSlowTinted == _isSlowed || m_Record == null)
            return;

        m_isSlowTinted = _isSlowed;

        if (_isSlowed == false)
        {
            SetColor(m_Record.ColorHex);
            return;
        }

        if (ColorUtility.TryParseHtmlString(m_Record.ColorHex, out Color baseColor) == true)
            SetColor(Color.Lerp(baseColor, SLOW_TINT_COLOR, SLOW_TINT_BLEND));
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }
}

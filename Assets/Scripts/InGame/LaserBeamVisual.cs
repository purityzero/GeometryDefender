using UnityEngine;

// 02_combat.html "투사체 종류" 확장 — 회전 레이저(#6) 전용 시각 효과. LineRenderer 2개(하얀 코어 + 색상 있는 외곽 글로우)를
// 겹쳐 그려 "안은 하얗고 겉은 무기 고유색으로 번지는" 레이저 느낌을 낸다(사용자 요청: "안은 하얀색인데 겉이 레이져처럼
// 연두색 이런걸루 했으면 좋겠는데" → 이후 코어를 빼봤다가 "컬러는 너무 레이져가 아니야"라는 재지적으로 다시 복원, 2026-07-27-2).
// Custom/GlowSpriteAdditive 셰이더 + MaterialPropertyBlock 조합이 이 프로젝트에서 실제로 확인된 발광 방식(몬스터/투사체/
// 타워와 동일) — LineRenderer 정점 색상은 이 셰이더에서 알파만 곱해질 뿐 RGB는 material._Color만 쓰므로 반드시 이 경로로
// 세팅해야 한다.
// Splash/Chain 이펙트와 달리 한 번 재생 후 소멸하는 풀링 오브젝트가 아니라, 무기 하나당 인스턴스 하나를 ActorPlayer 자식으로 계속 재사용한다.
public class LaserBeamVisual : MonoBehaviour
{
    private static readonly int COLOR_PROPERTY_ID = Shader.PropertyToID("_Color");

    [SerializeField] private LineRenderer m_CoreLineRenderer;
    [SerializeField] private LineRenderer m_GlowLineRenderer;

    private MaterialPropertyBlock m_PropertyBlock;

    // 코어(하얀 중심선)는 항상 고정 흰색 — 무기별로 색이 바뀌는 건 외곽 글로우 쪽만(무기 쿨다운 게이지와 같은 색, TowerRecord.ColorHex).
    public void SetColor(Color _color)
    {
        if (m_PropertyBlock == null)
            m_PropertyBlock = new MaterialPropertyBlock();

        m_GlowLineRenderer.GetPropertyBlock(m_PropertyBlock);
        m_PropertyBlock.SetColor(COLOR_PROPERTY_ID, _color);
        m_GlowLineRenderer.SetPropertyBlock(m_PropertyBlock);
    }

    public void UpdateBeam(Vector3 _origin, float _angleDegrees, float _range)
    {
        Vector3 direction = Quaternion.Euler(0f, 0f, _angleDegrees) * Vector3.right;
        Vector3 endPosition = _origin + direction * _range;

        m_CoreLineRenderer.SetPosition(0, _origin);
        m_CoreLineRenderer.SetPosition(1, endPosition);
        m_GlowLineRenderer.SetPosition(0, _origin);
        m_GlowLineRenderer.SetPosition(1, endPosition);
    }

    public void SetBeamActive(bool _isActive)
    {
        gameObject.SetActive(_isActive);
    }
}

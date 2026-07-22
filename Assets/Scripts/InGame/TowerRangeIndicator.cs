using DG.Tweening;
using UnityEngine;

// 02_combat.html "사거리 시각화" — 평시엔 숨겨져 있다가, 카드 선택 등 변경 시점에 Show()를 호출해 1.5초간 점선 원을 페이드 인/아웃.
// 카드 시스템이 아직 없어 자동 트리거는 없음 — Show()/SetRange()는 향후 카드 시스템이 호출할 확장 지점.
public class TowerRangeIndicator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_RangeRenderer;
    [SerializeField] private float m_NativeRadius = 1f;
    [SerializeField] private float m_FadeDuration = 0.3f;

    private Sequence m_Sequence;

    private void Awake()
    {
        Color color = m_RangeRenderer.color;
        color.a = 0f;
        m_RangeRenderer.color = color;
    }

    public void SetRange(float _range)
    {
        float scale = (_range / m_NativeRadius) * 2f;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    public void Show(float _duration)
    {
        m_Sequence?.Kill();

        float holdDuration = Mathf.Max(0f, _duration - (m_FadeDuration * 2f));

        m_Sequence = TweenSequenceBuilder.Create()
            .Append(TweenUtil.Fade(m_RangeRenderer, 1f, m_FadeDuration))
            .Delay(holdDuration)
            .Append(TweenUtil.Fade(m_RangeRenderer, 0f, m_FadeDuration))
            .Play();
    }

    private void OnDestroy()
    {
        m_Sequence?.Kill();
    }
}

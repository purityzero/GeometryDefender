using DG.Tweening;
using UnityEngine;

public enum eTweenStepType
{
    Append,     // 이전 스텝이 끝난 뒤 실행
    Join        // 직전 스텝과 동시에 실행
}

/// <summary>
/// TweenEffectPlayer가 시퀀스로 조립하는 이펙트 컴포넌트의 공통 베이스.
/// 파생 클래스는 CreateTween()에서 TweenUtil 헬퍼로 트윈만 만들어 반환한다.
/// (Ease/Delay는 베이스가 공통 적용)
/// </summary>
public abstract class TweenEffectBase : MonoBehaviour
{
    [SerializeField] private eTweenStepType m_StepType = eTweenStepType.Append;
    [SerializeField] private float m_Duration = 0.2f;
    [SerializeField] private float m_Delay = 0f;
    [SerializeField] private Ease m_Ease = Ease.OutQuad;

    public eTweenStepType stepType => m_StepType;

    protected float duration => m_Duration;

    public Tween BuildTween()
    {
        Tween tween = CreateTween();
        if (tween == null)
            return null;

        tween.SetEase(m_Ease);

        if (m_Delay > 0f)
            tween.SetDelay(m_Delay);

        return tween;
    }

    protected abstract Tween CreateTween();
}

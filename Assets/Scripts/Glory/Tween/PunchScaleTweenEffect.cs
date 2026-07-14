using DG.Tweening;
using UnityEngine;

public class PunchScaleTweenEffect : TweenEffectBase
{
    [SerializeField] private float m_Strength = 0.2f;

    protected override Tween CreateTween()
    {
        return TweenUtil.PunchScale(transform, m_Strength, duration);
    }
}

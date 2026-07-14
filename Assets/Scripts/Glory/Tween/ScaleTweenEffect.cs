using DG.Tweening;
using UnityEngine;

public class ScaleTweenEffect : TweenEffectBase
{
    [SerializeField] private Vector3 m_TargetScale = Vector3.one;

    protected override Tween CreateTween()
    {
        return TweenUtil.Scale(transform, m_TargetScale, duration);
    }
}

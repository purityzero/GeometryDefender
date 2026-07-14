using DG.Tweening;
using UnityEngine;

public class RotateTweenEffect : TweenEffectBase
{
    [SerializeField] private Vector3 m_RotationValue = Vector3.zero;
    [SerializeField] private RotateMode m_RotateMode = RotateMode.Fast;

    protected override Tween CreateTween()
    {
        return TweenUtil.RotateLocal(transform, m_RotationValue, duration, m_RotateMode);
    }
}

using DG.Tweening;
using UnityEngine;

/// <summary>RectTransform이면 anchoredPosition, 아니면 월드 좌표로 이동.</summary>
public class MoveTweenEffect : TweenEffectBase
{
    [SerializeField] private Vector3 m_TargetPosition = Vector3.zero;

    protected override Tween CreateTween()
    {
        if (transform is RectTransform == true)
        {
            var rectTransform = transform as RectTransform;
            return TweenUtil.MoveAnchored(rectTransform, m_TargetPosition, duration);
        }
        else
        {
            return TweenUtil.Move(transform, m_TargetPosition, duration);
        }
    }
}

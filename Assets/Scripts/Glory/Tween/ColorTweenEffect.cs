using DG.Tweening;
using UnityEngine;

/// <summary>같은 오브젝트의 Image → SpriteRenderer 순으로 컬러 대상을 자동 탐색.</summary>
public class ColorTweenEffect : TweenEffectBase
{
    [SerializeField] private Color m_TargetColor = Color.white;

    protected override Tween CreateTween()
    {
        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            return TweenUtil.Color(image, m_TargetColor, duration);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            return TweenUtil.Color(spriteRenderer, m_TargetColor, duration);

        Logger.Error($"[ColorTweenEffect] 컬러 대상 컴포넌트가 없습니다 - {gameObject.name}");
        return null;
    }
}

using DG.Tweening;
using UnityEngine;

/// <summary>같은 오브젝트의 CanvasGroup → Image → SpriteRenderer → TMP 순으로 페이드 대상을 자동 탐색.</summary>
public class FadeTweenEffect : TweenEffectBase
{
    [SerializeField] private float m_TargetAlpha = 1f;

    protected override Tween CreateTween()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            return TweenUtil.Fade(canvasGroup, m_TargetAlpha, duration);

        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            return TweenUtil.Fade(image, m_TargetAlpha, duration);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            return TweenUtil.Fade(spriteRenderer, m_TargetAlpha, duration);

        TMPro.TextMeshProUGUI text = GetComponent<TMPro.TextMeshProUGUI>();
        if (text != null)
            return TweenUtil.Fade(text, m_TargetAlpha, duration);

        Logger.Error($"[FadeTweenEffect] 페이드 가능한 컴포넌트가 없습니다 - {gameObject.name}");
        return null;
    }
}

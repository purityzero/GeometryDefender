using DG.Tweening;
using UnityEngine;

/// <summary>같은 오브젝트의 Image → SpriteRenderer 순으로 머테리얼(_GlowAmount)을 자동 탐색.</summary>
public class GlowAmountTweenEffect : TweenEffectBase
{
    private const string GLOW_AMOUNT_PROPERTY = "_GlowAmount";

    [SerializeField] private float m_TargetGlowAmount = 1f;

    protected override Tween CreateTween()
    {
        UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            return TweenUtil.Float(image.material, GLOW_AMOUNT_PROPERTY, m_TargetGlowAmount, duration);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            return TweenUtil.Float(spriteRenderer.material, GLOW_AMOUNT_PROPERTY, m_TargetGlowAmount, duration);

        Logger.Error($"[GlowAmountTweenEffect] 머테리얼 대상 컴포넌트가 없습니다 - {gameObject.name}");
        return null;
    }
}

using UnityEngine;

// 02_combat.html "투사체 종류" — Splash 카드로 명중 시 터지는 범위 폭발 이펙트. CritExplosion과 동일한 스케일 팝+페이드 구조이나
// 크리티컬 처치 전용이 아니라 명중마다(더 자주) 재생되므로 별도 클래스로 분리(더 작고 빠르게 튜닝, GameConfigTable 참고).
public class SplashExplosion : FactoryObject
{
    [SerializeField] private SpriteRenderer m_SpriteRenderer;

    public void Play(System.Action<SplashExplosion> _onComplete)
    {
        Color color = m_SpriteRenderer.color;
        color.a = 1f;
        m_SpriteRenderer.color = color;
        transform.localScale = Vector3.zero;

        Vector3 targetScale = Vector3.one * GameConfigTable.SPLASH_EXPLOSION_TARGET_SCALE;

        TweenSequenceBuilder.Create()
            .Append(TweenUtil.Scale(transform, targetScale, GameConfigTable.SPLASH_EXPLOSION_SCALE_POP_DURATION))
            .Join(TweenUtil.Fade(m_SpriteRenderer, 0f, GameConfigTable.SPLASH_EXPLOSION_FADE_DURATION))
            .OnComplete(() => _onComplete?.Invoke(this))
            .Play();
    }
}

using UnityEngine;

// 02_combat.html "치명타 시스템" — 치명타로 적을 처치했을 때 터지는 폭발 이펙트. 파티클 시스템 없이 스프라이트 확대+페이드로 표현.
public class CritExplosion : FactoryObject
{
    [SerializeField] private SpriteRenderer m_SpriteRenderer;

    public void Play(System.Action<CritExplosion> _onComplete)
    {
        Color color = m_SpriteRenderer.color;
        color.a = 1f;
        m_SpriteRenderer.color = color;
        transform.localScale = Vector3.zero;

        // 프리팹 네이티브 스프라이트 지름(2.22) 기준 — 몬스터/타워 실제 표시 크기(대략 0.5~1유닛)에 맞춰 최종 지름이 그와 비슷하게 나오도록 역산한 값
        Vector3 targetScale = Vector3.one * GameConfigTable.CRIT_EXPLOSION_TARGET_SCALE;

        TweenSequenceBuilder.Create()
            .Append(TweenUtil.Scale(transform, targetScale, GameConfigTable.CRIT_EXPLOSION_SCALE_POP_DURATION))
            .Join(TweenUtil.Fade(m_SpriteRenderer, 0f, GameConfigTable.CRIT_EXPLOSION_FADE_DURATION))
            .OnComplete(() => _onComplete?.Invoke(this))
            .Play();
    }
}

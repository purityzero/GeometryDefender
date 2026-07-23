using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// 02_combat.html "투사체 종류" — Chain 카드로 튄 경로를 LineRenderer로 그려 보여주는 이펙트(2026-07-24, 사용자 요청).
// HDR 파이프라인 + Bloom(URP)에 걸리도록 밝은 색을 사용해 별도 글로우 셰이더 없이도 발광 느낌을 낸다.
public class ChainLightning : FactoryObject
{
    [SerializeField] private LineRenderer m_LineRenderer;

    public void Play(List<Vector3> _points, System.Action<ChainLightning> _onComplete)
    {
        m_LineRenderer.useWorldSpace = true;
        m_LineRenderer.positionCount = _points.Count;
        for (int i = 0; i < _points.Count; ++i)
        {
            m_LineRenderer.SetPosition(i, _points[i]);
        }

        Color startColor = m_LineRenderer.startColor;
        Color endColor = m_LineRenderer.endColor;
        startColor.a = 1f;
        endColor.a = 1f;
        m_LineRenderer.startColor = startColor;
        m_LineRenderer.endColor = endColor;

        TweenUtil.Fade(m_LineRenderer, 0f, GameConfigTable.CHAIN_LIGHTNING_FADE_DURATION)
            .OnComplete(() => _onComplete?.Invoke(this))
            .Play();
    }
}

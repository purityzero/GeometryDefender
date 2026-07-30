using DG.Tweening;
using UnityEngine;

public class ActorProjectile : Actor
{
    [SerializeField] private Renderer m_Renderer;

    // 인스턴스 머티리얼(Renderer.material 최초 접근 시 자동 clone됨) 참조 — 외부에서 TweenUtil로 글로우/색상을
    // 직접 트윈하고 싶은 특수 케이스(Frost Orb Turret 등)를 위한 최소 노출. 대부분의 소비처는 SetColor()로 충분하다.
    public Material material => m_Renderer.material;

    public void SetColor(Color _color)
    {
        // ColorUtility.TryParseHtmlString()는 sRGB 기준 색을 돌려주는데 이 프로젝트는 Linear 색공간이라
        // 그대로 넣으면 화면 색이 물빠진 것처럼 보인다(ActorMonster.SetColor와 동일 이유) — .linear로 변환 필요.
        m_Renderer.material.color = _color.linear;
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        // 2026-07-30 — Orbital Ring 등 이 컴포넌트에 무한 루프 Tween(글로우 펄스/색상)을 걸어두는 소비처가 생기면서,
        // 반납 없이 그대로 두면 다음 재사용자(전혀 다른 투사체)에게 트윈이 계속 적용되는 풀링 오염이 생길 수 있음 —
        // 반납 시 트윈을 정리하고 글로우도 머티리얼 기본값(1)으로 되돌린다.
        DOTween.Kill(m_Renderer.material);
        m_Renderer.material.SetFloat("_GlowAmount", 1f);

        base.Close();
    }
}

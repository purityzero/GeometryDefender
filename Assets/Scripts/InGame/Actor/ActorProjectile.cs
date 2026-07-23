using UnityEngine;

public class ActorProjectile : Actor
{
    [SerializeField] private Renderer m_Renderer;

    // 02_combat.html "투사체 종류" — 카드로 동시에 여러 효과(관통/스플래시/체인/호밍)가 붙을 수 있어
    // 투사체 하나의 색을 바꾸는 대신, 활성 효과별 작은 아이콘을 겹쳐서 다중 표시(사용자 선택: "다중 표시(색 조합/아이콘 오버레이)")
    [SerializeField] private GameObject m_IconPierce;
    [SerializeField] private GameObject m_IconSplash;
    [SerializeField] private GameObject m_IconChain;
    [SerializeField] private GameObject m_IconHoming;

    public void SetColor(Color _color)
    {
        // ColorUtility.TryParseHtmlString()는 sRGB 기준 색을 돌려주는데 이 프로젝트는 Linear 색공간이라
        // 그대로 넣으면 화면 색이 물빠진 것처럼 보인다(ActorMonster.SetColor와 동일 이유) — .linear로 변환 필요.
        m_Renderer.material.color = _color.linear;
    }

    public void SetEffectIcons(bool _hasPierce, bool _hasSplash, bool _hasChain, bool _hasHoming)
    {
        m_IconPierce.SetActive(_hasPierce);
        m_IconSplash.SetActive(_hasSplash);
        m_IconChain.SetActive(_hasChain);
        m_IconHoming.SetActive(_hasHoming);
    }

    public override void Open()
    {
        base.Open();
    }

    public override void Close()
    {
        base.Close();
    }
}

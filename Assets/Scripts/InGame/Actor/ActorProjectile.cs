using UnityEngine;

public class ActorProjectile : Actor
{
    [SerializeField] private Renderer m_Renderer;

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
        base.Close();
    }
}

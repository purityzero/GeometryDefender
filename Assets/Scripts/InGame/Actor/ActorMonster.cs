using UnityEngine;

public class ActorMonster : Actor
{
    [SerializeField] private Renderer m_Renderer;

    public void SetColor(Color _color)
    {
        m_Renderer.material.color = _color;
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

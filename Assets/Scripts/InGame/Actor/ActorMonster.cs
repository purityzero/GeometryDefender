using UnityEngine;

public class ActorMonster : Actor
{
    [SerializeField] private Renderer m_Renderer;
    private EnemyRecord m_Record;

    public void SetColor(Color _color)
    {
        m_Renderer.material.color = _color;
    }

    public void SetColor(string _colorHex)
    {
        if (ColorUtility.TryParseHtmlString(_colorHex, out Color color) == true)
            SetColor(color);
    }

    public void Open(EnemyRecord _record)
    {
        m_Record = _record;
        SetColor(_record.ColorHex);
        Open();
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

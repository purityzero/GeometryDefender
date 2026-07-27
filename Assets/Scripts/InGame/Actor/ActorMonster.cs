using UnityEngine;

public class ActorMonster : Actor
{
    [SerializeField] private Renderer m_Renderer;
    [SerializeField] private Renderer m_GlowRenderer;
    [SerializeField] private CullingObject m_CullingObject;
    private EnemyRecord m_Record;

    public void UpdateCullingLogic()
    {
        if (m_CullingObject == null)
            return;

        m_CullingObject.UpdateLogic();
    }

    public void SetColor(Color _color)
    {
        // ColorUtility.TryParseHtmlString()는 sRGB(디자이너가 보는 hex 그대로) 기준 색을 돌려주는데,
        // 이 프로젝트는 Linear 색공간이라 material.color(=_Color 셰이더 프로퍼티)에 그대로 넣으면
        // 감마 보정이 두 번(혹은 0번) 걸려 실제 화면 색이 hex보다 밝고 채도 낮게(물빠진 색으로) 나온다.
        // .linear로 변환해 넣어야 화면에 hex 그대로 보인다.
        m_Renderer.material.color = _color.linear;

        if (m_GlowRenderer != null)
            m_GlowRenderer.material.color = _color.linear;
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

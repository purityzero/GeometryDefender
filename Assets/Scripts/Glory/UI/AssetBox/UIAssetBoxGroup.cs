using UnityEngine;
using UnityEngine.EventSystems;

public class UIAssetBoxGroup : UIBehaviour
{
    [SerializeField] private UIAssetBox[] m_AssetBoxes;

    protected override void OnEnable()
    {
       base.OnEnable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
    }

    public void SetData()
    {
        for (int i = 0; i < m_AssetBoxes.Length; ++i)
        {
            m_AssetBoxes[i].SetData();
        }  
    }
    public void Refresh()
    {
        for (int i = 0; i < m_AssetBoxes.Length; ++i)
        {
            m_AssetBoxes[i].Refresh();
        }
    }
}
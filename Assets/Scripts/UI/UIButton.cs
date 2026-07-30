using UnityEngine;
using UnityEngine.UI;

public class UIButton : Button
{
    [SerializeField] private string m_ClickSoundKey = "ButtonClick";

    protected override void Awake()
    {
        base.Awake();
        onClick.AddListener(OnClickPlaySound);
    }

    private void OnClickPlaySound()
    {
        BaseScene.Current?.PlaySfx(m_ClickSoundKey);
    }
}

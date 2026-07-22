public class TitleScene : BaseScene
{
    protected override void OnSetup()
    {
        //하지마라
    }

    public void OnClickPlayButton()
    {
        UIManager.instance.Get<UIDifficultySelect>();
    }

    public void OnClickMetatreeButton()
    {
        UIManager.instance.Get<UIMetaTree>();
    }

    public void OnClickSettingsButton()
    {
        
    }

    public void OnClickHowToPlayButton()
    {
        
    }
}

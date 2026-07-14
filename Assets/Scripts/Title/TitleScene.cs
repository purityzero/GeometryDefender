using UnityEngine;

public class TitleScene : MonoBehaviour
{
    private void Start()
    {
        //하지마라

    }

    public void OnClickPlayButton()
    {
        SceneManager.instance.NextScene(EScene.InGameScene.ToString());
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

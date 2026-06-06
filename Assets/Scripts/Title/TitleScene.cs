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
}

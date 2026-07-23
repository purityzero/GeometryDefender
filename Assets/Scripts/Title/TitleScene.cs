using UnityEngine;

// BaseScene.Awake()가 씬 내 다른 모든 스크립트의 Awake/OnEnable보다 먼저 실행되도록 강제 — 상세 이유는 InGameScene.cs 주석 참고
[DefaultExecutionOrder(-1000)]
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
        UIManager.instance.Get<UISetting>();
    }

    public void OnClickHowToPlayButton()
    {
        
    }
}

using UnityEngine;

// BaseScene.Awake()가 씬 내 다른 모든 스크립트의 Awake/OnEnable보다 먼저 실행되도록 강제 — 상세 이유는 InGameScene.cs 주석 참고치
[DefaultExecutionOrder(-1000)]
public class TitleScene : BaseScene
{
    protected override void OnSetup()
    {
        // 주의: 이 메서드에 TableManager.init() 같은 "공용 시스템 재초기화" 로직을 넣지 말 것(과거 중복 호출 버그 이력, [[TitleScene]] 참고).
        // BGM 재생은 그런 공용 시스템과 무관한 이 씬 자체의 1회성 진입 연출이라 안전.
        PlayBgm();
    }

    private void PlayBgm()
    {
        SoundTable soundTable = TableManager.instance.GetTable<SoundTable>();
        SoundRecord record = soundTable?.GetRecordByKey("TitleTheme");
        if (record == null)
        {
            Logger.Error($"[TitleScene] PlayBgm Failed! SoundRecord not found - TitleTheme");
            return;
        }

        AudioClip clip = ResUtil.Load<AudioClip>(record.ClipPath);
        if (clip == null)
            return;

        SoundManager.instance.PlayBgm(clip);
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

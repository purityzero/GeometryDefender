using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
        TableManager.instance.init();
        ErrorLogManager.instance.Init();

        // 사용자 피드백("전체적인 볼륨이 너무 커" 2회) — Glory SoundManager 기본값(1.0)은 그대로 두고 프로젝트 부트스트랩에서만 낮춤
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Master, 0.3f);

        // 옵션 화면의 BGM/SFX 슬라이더(UISetting)에 저장된 값을 부팅 시점에 반영. 이후 슬라이더 조작 시 반영은
        // PlayerManager.SetBgmVolume()/SetSfxVolume()에서 처리.
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Bgm, PlayerManager.instance.optionData.BgmVolume);
        SoundManager.instance.SetCategoryVolume(eSoundCategory.Sfx, PlayerManager.instance.optionData.SfxVolume);
    }
}

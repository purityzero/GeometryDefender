using System.Collections.Generic;
using UnityEngine;

// 공용 사운드 매니저 — BGM/Ambience(루프, 카테고리별 크로스페이드)와 Sfx(단발, 동시 재생 수 제한)를 담당.
// 프로젝트는 SoundManager를 씬에 배치하고 m_SoundTemplate(비활성 AudioSource 자식)만 연결하면 바로 사용 가능 —
// 볼륨 옵션(마스터/BGM/효과음 등)을 프로젝트가 직접 관리한다면 SetCategoryVolume()으로 연결만 해주면 된다.
public class SoundManager : MonoSingleton<SoundManager>, IUpdatable
{
    [SerializeField] private SoundComponent m_SoundTemplate;
    [SerializeField] private Transform m_PoolParent;
    [SerializeField] private SoundFadeData m_BgmFadeData;
    [SerializeField] private SoundFadeData m_AmbienceFadeData;

    private List<SoundComponent> m_InactivePool = new List<SoundComponent>();

    private SoundComponent m_ActiveBgm;
    private List<SoundComponent> m_ActiveAmbience = new List<SoundComponent>();
    private List<SoundComponent> m_ActiveSfx = new List<SoundComponent>();

    private List<SoundComponent> m_FadeInList = new List<SoundComponent>();
    private List<SoundComponent> m_FadeOutList = new List<SoundComponent>();

    private Dictionary<eSoundCategory, float> m_CategoryVolumes = new Dictionary<eSoundCategory, float>();
    private bool m_isMuted;
    private bool m_isPaused;
    private BaseScene m_RegisteredScene;

    protected override void Awake()
    {
        base.Awake();

        foreach (eSoundCategory category in System.Enum.GetValues(typeof(eSoundCategory)))
        {
            m_CategoryVolumes[category] = 1f;
        }

        // 씬에 SoundManager를 직접 배치하지 않고 MonoSingleton 자동 생성에 맡긴 경우 m_SoundTemplate가 비어있을 수 있다 —
        // 프로젝트 쪽 씬/프리팹 셋업 없이도 바로 동작하도록 코드로 최소 템플릿을 만들어 채워준다.
        if (m_SoundTemplate == null)
        {
            GameObject templateObject = new GameObject("SoundTemplate");
            templateObject.transform.SetParent(transform);
            templateObject.AddComponent<AudioSource>();
            m_SoundTemplate = templateObject.AddComponent<SoundComponent>();
            templateObject.SetActive(false);
        }
    }

    // MonoSingleton(DontDestroyOnLoad)이라 씬이 바뀌어도 자신은 파괴되지 않지만, IUpdatable을 배급하는
    // BaseScene(SceneSingleton)은 씬마다 새로 생기고 이전 것은 파괴된다 — 그래서 자기 자신의 Update()에서
    // 매 프레임 BaseScene.Current가 바뀌었는지 확인해 새 씬의 BaseScene에 재등록한다.
    // 일시정지 감지도 여기서 한다: 등록 여부와 무관하게 매 프레임 돌아야 전환되는 프레임을 놓치지 않기 때문
    // (UpdateLogic()은 BaseScene.Update()가 isPaused==true면 아예 호출을 건너뛰므로 이 감지에는 못 쓴다).
    private void Update()
    {
        if (BaseScene.Current != m_RegisteredScene)
        {
            m_RegisteredScene?.Unregister(this);
            m_RegisteredScene = BaseScene.Current;
            m_RegisteredScene?.Register(this);
        }

        bool isPaused = (BaseScene.Current != null) && BaseScene.Current.isPaused;
        if (isPaused != m_isPaused)
        {
            m_isPaused = isPaused;
            SetAllSoundsPaused(isPaused);
        }
    }

    // BaseScene.Update()가 isPaused==false일 때만 호출해준다 — 정지 중엔 정리/페이드가 자동으로 건너뛰어진다
    // (건너뛰지 않으면 Pause로 재생이 멈춘 사운드가 "재생 안 함"으로 오판돼 풀로 반납되어 버림).
    public void UpdateLogic()
    {
        for (int i = m_ActiveSfx.Count - 1; i >= 0; --i)
        {
            SoundComponent sound = m_ActiveSfx[i];
            if (sound == null || sound.isPlaying == false)
            {
                if (sound != null)
                    ReleaseSound(sound);

                m_ActiveSfx.RemoveAt(i);
            }
        }

        UpdateFade(m_FadeInList, true);
        UpdateFade(m_FadeOutList, false);
    }

    // Bgm은 일시정지 대상에서 제외 — Popup(UIPause 등)이 떠서 게임이 멈춰도 음악은 계속 재생돼야 한다는 요구사항.
    private void SetAllSoundsPaused(bool _isPause)
    {
        for (int i = 0; i < m_ActiveAmbience.Count; ++i)
        {
            m_ActiveAmbience[i].Pause(_isPause);
        }

        for (int i = 0; i < m_ActiveSfx.Count; ++i)
        {
            m_ActiveSfx[i].Pause(_isPause);
        }
    }

    #region BGM
    public SoundComponent PlayBgm(AudioClip _clip)
    {
        if (_clip == null)
            return null;

        if (m_ActiveBgm != null && m_ActiveBgm.audioSource.clip == _clip)
            return m_ActiveBgm;

        if (m_ActiveBgm != null)
            FadeOutSound(m_ActiveBgm, m_BgmFadeData);

        SoundComponent sound = CreateSound();
        if (sound == null)
            return null;

        sound.Play(_clip, eSoundCategory.Bgm, true, 0f);
        FadeInSound(sound, m_BgmFadeData);

        m_ActiveBgm = sound;
        return sound;
    }

    public void StopBgm()
    {
        if (m_ActiveBgm == null)
            return;

        FadeOutSound(m_ActiveBgm, m_BgmFadeData);
        m_ActiveBgm = null;
    }
    #endregion

    #region Ambience
    public List<SoundComponent> PlayAmbience(List<AudioClip> _clips)
    {
        List<SoundComponent> result = new List<SoundComponent>();

        if (_clips == null)
            return result;

        for (int i = 0; i < _clips.Count; ++i)
        {
            SoundComponent sound = PlayAmbience(_clips[i]);
            if (sound != null)
                result.Add(sound);
        }

        return result;
    }

    public SoundComponent PlayAmbience(AudioClip _clip)
    {
        if (_clip == null)
            return null;

        SoundComponent existing = m_ActiveAmbience.Find(sound => sound.audioSource.clip == _clip);
        if (existing != null)
            return existing;

        SoundComponent sound = CreateSound();
        if (sound == null)
            return null;

        sound.Play(_clip, eSoundCategory.Ambience, true, 0f);
        FadeInSound(sound, m_AmbienceFadeData);

        m_ActiveAmbience.Add(sound);
        return sound;
    }

    public void StopAmbience(AudioClip _clip = null)
    {
        if (_clip == null)
        {
            for (int i = 0; i < m_ActiveAmbience.Count; ++i)
            {
                FadeOutSound(m_ActiveAmbience[i], m_AmbienceFadeData);
            }

            m_ActiveAmbience.Clear();
            return;
        }

        SoundComponent sound = m_ActiveAmbience.Find(s => s.audioSource.clip == _clip);
        if (sound == null)
            return;

        FadeOutSound(sound, m_AmbienceFadeData);
        m_ActiveAmbience.Remove(sound);
    }
    #endregion

    #region Sfx
    // _maxConcurrent — 같은 클립이 동시에 이 수만큼 재생 중이면 가장 오래된 것을 정지시키고 새로 재생(0 이하면 제한 없음)
    public SoundComponent PlaySfx(AudioClip _clip, Vector3? _position = null, int _maxConcurrent = 0)
    {
        if (_clip == null)
            return null;

        if (_maxConcurrent > 0)
        {
            List<SoundComponent> sameClipSounds = m_ActiveSfx.FindAll(sound => sound.audioSource.clip == _clip);
            if (sameClipSounds.Count >= _maxConcurrent)
            {
                SoundComponent oldest = sameClipSounds[0];
                ReleaseSound(oldest);
                m_ActiveSfx.Remove(oldest);
            }
        }

        SoundComponent newSound = CreateSound();
        if (newSound == null)
            return null;

        if (_position.HasValue == true)
            newSound.transform.position = _position.Value;

        newSound.Play(_clip, eSoundCategory.Sfx, false, GetCategoryVolume(eSoundCategory.Sfx));
        m_ActiveSfx.Add(newSound);
        return newSound;
    }

    public void StopAllSfx()
    {
        for (int i = m_ActiveSfx.Count - 1; i >= 0; --i)
        {
            ReleaseSound(m_ActiveSfx[i]);
        }

        m_ActiveSfx.Clear();
    }
    #endregion

    #region Volume
    public void SetCategoryVolume(eSoundCategory _category, float _volume)
    {
        m_CategoryVolumes[_category] = Mathf.Clamp01(_volume);
        RefreshAllVolumes();
    }

    public float GetCategoryVolume(eSoundCategory _category)
    {
        float masterVolume = m_CategoryVolumes.TryGetValue(eSoundCategory.Master, out float master) ? master : 1f;
        float muteMultiplier = (m_isMuted == true) ? 0f : 1f;

        if (_category == eSoundCategory.Master)
            return masterVolume * muteMultiplier;

        float categoryVolume = m_CategoryVolumes.TryGetValue(_category, out float value) ? value : 1f;
        return masterVolume * categoryVolume * muteMultiplier;
    }

    public void SetMute(bool _isMute)
    {
        m_isMuted = _isMute;
        RefreshAllVolumes();
    }

    private void RefreshAllVolumes()
    {
        if (m_ActiveBgm != null && m_FadeInList.Contains(m_ActiveBgm) == false && m_FadeOutList.Contains(m_ActiveBgm) == false)
            m_ActiveBgm.SetVolume(m_ActiveBgm.baseVolume * GetCategoryVolume(eSoundCategory.Bgm));

        for (int i = 0; i < m_ActiveAmbience.Count; ++i)
        {
            SoundComponent sound = m_ActiveAmbience[i];
            if (m_FadeInList.Contains(sound) == false && m_FadeOutList.Contains(sound) == false)
                sound.SetVolume(sound.baseVolume * GetCategoryVolume(eSoundCategory.Ambience));
        }

        for (int i = 0; i < m_ActiveSfx.Count; ++i)
        {
            m_ActiveSfx[i].SetVolume(m_ActiveSfx[i].baseVolume * GetCategoryVolume(eSoundCategory.Sfx));
        }
    }
    #endregion

    #region Fade
    private void FadeInSound(SoundComponent _sound, SoundFadeData _fadeData)
    {
        _sound.fadeData = _fadeData;
        _sound.fadeTimer = 0f;
        m_FadeOutList.Remove(_sound);
        m_FadeInList.Add(_sound);
    }

    private void FadeOutSound(SoundComponent _sound, SoundFadeData _fadeData)
    {
        _sound.fadeData = _fadeData;
        _sound.fadeTimer = 0f;
        m_FadeInList.Remove(_sound);
        m_FadeOutList.Add(_sound);
    }

    private void UpdateFade(List<SoundComponent> _list, bool _isFadeIn)
    {
        for (int i = _list.Count - 1; i >= 0; --i)
        {
            SoundComponent sound = _list[i];
            if (sound == null)
            {
                _list.RemoveAt(i);
                continue;
            }

            sound.fadeTimer += Time.unscaledDeltaTime;

            float fadeRatio;
            bool isDone;
            if (sound.fadeData == null)
            {
                fadeRatio = (_isFadeIn == true) ? 1f : 0f;
                isDone = true;
            }
            else
            {
                fadeRatio = (_isFadeIn == true) ? sound.fadeData.GetFadeInVolume(sound.fadeTimer) : sound.fadeData.GetFadeOutVolume(sound.fadeTimer);
                float duration = (_isFadeIn == true) ? sound.fadeData.FadeInDuration : sound.fadeData.FadeOutDuration;
                isDone = sound.fadeTimer >= duration;
            }

            sound.SetVolume(sound.baseVolume * GetCategoryVolume(sound.category) * fadeRatio);

            if (isDone == false)
                continue;

            _list.RemoveAt(i);

            if (_isFadeIn == false)
                ReleaseSound(sound);
        }
    }
    #endregion

    #region Pool
    private SoundComponent CreateSound()
    {
        if (m_SoundTemplate == null)
        {
            Logger.Error("[SoundManager] CreateSound Failed! m_SoundTemplate not assigned");
            return null;
        }

        SoundComponent sound;
        if (m_InactivePool.Count > 0)
        {
            int lastIndex = m_InactivePool.Count - 1;
            sound = m_InactivePool[lastIndex];
            m_InactivePool.RemoveAt(lastIndex);
        }
        else
        {
            Transform parent = (m_PoolParent != null) ? m_PoolParent : transform;
            sound = ResUtil.Create(m_SoundTemplate, parent);
        }

        if (sound == null)
            return null;

        sound.gameObject.SetActive(true);
        sound.Open();
        return sound;
    }

    private void ReleaseSound(SoundComponent _sound)
    {
        if (_sound == null)
            return;

        _sound.Close();
        _sound.gameObject.SetActive(false);
        m_InactivePool.Add(_sound);
    }
    #endregion
}

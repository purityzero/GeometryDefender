using UnityEngine;

// AudioSource 하나를 감싸는 재생 단위 — SoundManager가 풀링해서 재사용(FactoryObject Open/Close로 상태 초기화).
[RequireComponent(typeof(AudioSource))]
public class SoundComponent : FactoryObject
{
    [SerializeField] private AudioSource m_AudioSource;

    // SoundManager가 페이드/볼륨 계산에 쓰는 상태 — 전부 SoundManager 쪽에서만 갱신(외부에서 직접 건드리지 않음)
    public eSoundCategory category { get; set; }
    public float baseVolume { get; set; } = 1f;
    public SoundFadeData fadeData { get; set; }
    public float fadeTimer { get; set; }

    public AudioSource audioSource => m_AudioSource;
    public bool isPlaying => m_AudioSource != null && m_AudioSource.isPlaying;

    private void Awake()
    {
        if (m_AudioSource == null)
            m_AudioSource = GetComponent<AudioSource>();
    }

    public void Play(AudioClip _clip, eSoundCategory _category, bool _isLoop, float _volume)
    {
        if (m_AudioSource == null || _clip == null)
            return;

        category = _category;
        fadeData = null;
        fadeTimer = 0f;
        baseVolume = 1f;

        m_AudioSource.clip = _clip;
        m_AudioSource.loop = _isLoop;
        m_AudioSource.volume = _volume;
        m_AudioSource.Play();
    }

    public void SetVolume(float _volume)
    {
        if (m_AudioSource != null)
            m_AudioSource.volume = _volume;
    }

    public void Stop()
    {
        if (m_AudioSource != null)
            m_AudioSource.Stop();
    }

    public void Pause(bool _isPause)
    {
        if (m_AudioSource == null)
            return;

        if (_isPause == true)
            m_AudioSource.Pause();
        else
            m_AudioSource.UnPause();
    }

    public override void Open()
    {
        base.Open();
        fadeData = null;
        fadeTimer = 0f;
    }

    public override void Close()
    {
        base.Close();
        Stop();

        if (m_AudioSource != null)
            m_AudioSource.clip = null;

        fadeData = null;
        fadeTimer = 0f;
    }
}

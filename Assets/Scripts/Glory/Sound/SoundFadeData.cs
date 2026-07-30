using UnityEngine;

// BGM/Ambience 크로스페이드용 페이드 곡선 에셋 — SoundManager의 m_BgmFadeData/m_AmbienceFadeData에 연결해서 사용
[CreateAssetMenu(fileName = "SoundFadeData", menuName = "Glory/Sound/SoundFadeData")]
public class SoundFadeData : ScriptableObject
{
    public float FadeInDuration = 1f;
    public float FadeOutDuration = 1f;
    public AnimationCurve FadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    public AnimationCurve FadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    public float GetFadeInVolume(float _elapsedTime)
    {
        float ratio = (FadeInDuration <= 0f) ? 1f : Mathf.Clamp01(_elapsedTime / FadeInDuration);
        return FadeInCurve.Evaluate(ratio);
    }

    public float GetFadeOutVolume(float _elapsedTime)
    {
        float ratio = (FadeOutDuration <= 0f) ? 1f : Mathf.Clamp01(_elapsedTime / FadeOutDuration);
        return FadeOutCurve.Evaluate(ratio);
    }
}

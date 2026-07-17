using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TweenEffectBase 컴포넌트들을 등록 순서대로 TweenSequenceBuilder에 조립해 재생/제어하는 컨트롤러.
/// 각 이펙트의 StepType(Append/Join)에 따라 순차 또는 동시 실행된다.
/// </summary>
public class TweenEffectPlayer : MonoBehaviour
{
    [SerializeField] private TweenEffectBase[] m_Effects;
    [SerializeField] private bool m_isPlayOnEnable = true;
    [SerializeField] private int m_LoopCount = 1;       // -1 = 무한 반복
    [SerializeField] private LoopType m_LoopType = LoopType.Restart;

    private Sequence m_Sequence;

    public bool isPlaying => m_Sequence != null && m_Sequence.IsActive() == true && m_Sequence.IsPlaying() == true;

    private void OnEnable()
    {
        if (m_isPlayOnEnable == true)
            Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play(UnityAction _onComplete = null)
    {
        Stop();

        if (m_Effects == null || m_Effects.Length == 0)
        {
            Debug.LogError($"[TweenEffectPlayer] 등록된 이펙트가 없습니다 - {gameObject.name}");
            return;
        }

        TweenSequenceBuilder builder = TweenSequenceBuilder.Create();

        for (int i = 0; i < m_Effects.Length; ++i)
        {
            TweenEffectBase effect = m_Effects[i];
            if (effect == null)
                continue;

            Tween tween = effect.BuildTween();
            if (tween == null)
                continue;

            if (effect.stepType == eTweenStepType.Join)
                builder.Join(tween);
            else
                builder.Append(tween);
        }

        if (m_LoopCount != 1)
            builder.Loops(m_LoopCount, m_LoopType);

        if (_onComplete != null)
            builder.OnComplete(_onComplete);

        m_Sequence = builder.Play();
    }

    public void Pause()
    {
        if (m_Sequence != null && m_Sequence.IsActive() == true)
            m_Sequence.Pause();
    }

    public void Resume()
    {
        if (m_Sequence != null && m_Sequence.IsActive() == true)
            m_Sequence.Play();
    }

    public void Stop()
    {
        if (m_Sequence != null)
        {
            m_Sequence.Kill();
            m_Sequence = null;
        }
    }
}

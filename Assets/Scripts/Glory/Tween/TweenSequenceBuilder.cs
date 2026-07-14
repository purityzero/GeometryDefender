using DG.Tweening;
using UnityEngine.Events;

/// <summary>
/// DOTween Sequence를 체이닝으로 조립하는 빌더.
/// TweenUtil의 헬퍼들을 Append/Join으로 붙이고, Play() 또는 ToCommand()(FlowCommand 연동)로 마무리한다.
/// 예) TweenSequenceBuilder.Create()
///         .Append(TweenUtil.ScalePop(transform, 0.2f))
///         .Join(TweenUtil.Fade(canvasGroup, 1f, 0.2f))
///         .Delay(0.5f)
///         .OnComplete(() => Close())
///         .Play();
/// </summary>
public class TweenSequenceBuilder
{
    private Sequence m_Sequence;

    private TweenSequenceBuilder()
    {
        m_Sequence = DOTween.Sequence();
        m_Sequence.Pause();
    }

    public static TweenSequenceBuilder Create()
    {
        return new TweenSequenceBuilder();
    }

    public TweenSequenceBuilder Append(Tween _tween)
    {
        m_Sequence.Append(_tween);
        return this;
    }

    public TweenSequenceBuilder Join(Tween _tween)
    {
        m_Sequence.Join(_tween);
        return this;
    }

    public TweenSequenceBuilder Delay(float _seconds)
    {
        m_Sequence.AppendInterval(_seconds);
        return this;
    }

    public TweenSequenceBuilder Callback(UnityAction _callback)
    {
        m_Sequence.AppendCallback(() => _callback?.Invoke());
        return this;
    }

    public TweenSequenceBuilder Loops(int _count, LoopType _loopType = LoopType.Restart)
    {
        m_Sequence.SetLoops(_count, _loopType);
        return this;
    }

    public TweenSequenceBuilder OnComplete(UnityAction _callback)
    {
        m_Sequence.OnComplete(() => _callback?.Invoke());
        return this;
    }

    public Sequence Play()
    {
        m_Sequence.Play();
        return m_Sequence;
    }

    /// <summary>FlowCommand에 붙일 수 있는 커맨드로 변환. FlowCommand가 Execute할 때 재생된다.</summary>
    public Command_Tween ToCommand()
    {
        return new Command_Tween(m_Sequence);
    }
}

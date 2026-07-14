using DG.Tweening;

/// <summary>
/// 임의의 Tween/Sequence를 FlowCommand 큐에 태우는 커맨드.
/// 완료 감지는 폴링 방식이라 트윈의 OnComplete 콜백을 덮어쓰지 않는다.
/// </summary>
public class Command_Tween : ICommand
{
    private Tween m_Tween;
    private bool m_isFinished;

    public Command_Tween(Tween _tween)
    {
        m_Tween = _tween;

        if (m_Tween != null && m_Tween.IsActive() == true)
            m_Tween.Pause();
    }

    public void Execute()
    {
        if (m_Tween == null || m_Tween.IsActive() == false)
        {
            m_isFinished = true;
            return;
        }

        m_Tween.Play();
    }

    public void Update()
    {
        if (m_isFinished == true)
            return;

        if (m_Tween == null || m_Tween.IsActive() == false || m_Tween.IsComplete() == true)
            m_isFinished = true;
    }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive() == true)
            m_Tween.Kill();

        m_isFinished = true;
    }

    public bool IsFinished() => m_isFinished;
}

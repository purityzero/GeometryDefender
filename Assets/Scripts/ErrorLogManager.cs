using UnityEngine;

// Application.logMessageReceived로 에러/예외 로그를 잡아 UIErrorWindow에 표시하는 전역 캐처.
public class ErrorLogManager : MonoSingleton<ErrorLogManager>
{
    private bool m_isHandlingLog;
    private string m_LastMessage;
    private string m_LastStackTrace;

    public void Init() { }

    protected override void Awake()
    {
        base.Awake();
        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string _condition, string _stackTrace, LogType _logType)
    {
        if (_logType != LogType.Error && _logType != LogType.Exception && _logType != LogType.Assert)
            return;

        if (m_isHandlingLog == true)
            return;

        // 같은 에러가 매 프레임 반복 출력되는 경우(Update 안 NRE 등) 스크롤뷰가 무한히 불어나는 것을 막는다
        if (_condition == m_LastMessage && _stackTrace == m_LastStackTrace)
            return;

        m_isHandlingLog = true;
        m_LastMessage = _condition;
        m_LastStackTrace = _stackTrace;

        try
        {
            UIErrorWindow errorWindow = UIManager.instance.Get<UIErrorWindow>();
            if (errorWindow == null)
                return;

            errorWindow.AddErrorEntry(_condition, _stackTrace);
        }
        finally
        {
            m_isHandlingLog = false;
        }
    }
}

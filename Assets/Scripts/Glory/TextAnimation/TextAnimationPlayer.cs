using TMPro;
using UnityEngine;
using UnityEngine.Events;

public enum eTextPlayMode
{
    SetText,
    Typewriter
}

// Text Animator(com.febucci.text-animator-unity) 패키지가 설치된 프로젝트에서만 컴파일된다
// TextAnimatorUtil을 인스펙터에서 붙여 쓰는 컴포넌트 래퍼
public class TextAnimationPlayer : MonoBehaviour
{
    [SerializeField] private TMP_Text m_TargetText;
    [SerializeField] private eTextPlayMode m_PlayMode = eTextPlayMode.Typewriter;
    [SerializeField, TextArea(3, 10)] private string m_Content;
    [SerializeField] private bool m_isPlayOnStart = true;
    [SerializeField] private float m_TypewriterSpeed = 1f;

    public UnityEvent OnComplete;

    private void Awake()
    {
        if (m_TargetText == null)
            m_TargetText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        if (m_isPlayOnStart == true)
            Play();
    }

    public void Play()
    {
        Play(m_Content);
    }

    public void Play(string _content)
    {
        if (m_TargetText == null)
        {
            Logger.Error($"[TextAnimationPlayer] Play Failed! m_TargetText is null");
            return;
        }

        if (m_PlayMode == eTextPlayMode.SetText)
        {
            TextAnimatorUtil.SetText(m_TargetText, _content);
            OnComplete.Invoke();
            return;
        }

        TextAnimatorUtil.SetTypewriterSpeed(m_TargetText, m_TypewriterSpeed);
        TextAnimatorUtil.PlayTypewriter(m_TargetText, _content, () => OnComplete.Invoke());
    }

    public void Skip()
    {
        TextAnimatorUtil.SkipTypewriter(m_TargetText);
    }

    public void Hide()
    {
        TextAnimatorUtil.HideTypewriter(m_TargetText);
    }
}

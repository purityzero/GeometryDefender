using Febucci.TextAnimatorCore.Typing;
using Febucci.TextAnimatorForUnity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Text Animator(com.febucci.text-animator-unity) 패키지가 설치된 프로젝트에서만 컴파일된다
public static class TextAnimatorUtil
{
    public static TextAnimator_TMP GetAnimator(TMP_Text _text)
    {
        if (_text == null)
        {
            Debug.LogError($"[TextAnimatorUtil] GetAnimator Failed! text is null");
            return null;
        }

        TextAnimator_TMP textAnimator = _text.GetComponent<TextAnimator_TMP>();
        if (textAnimator == null)
            textAnimator = _text.gameObject.AddComponent<TextAnimator_TMP>();

        return textAnimator;
    }

    public static TypewriterComponent GetTypewriter(TMP_Text _text)
    {
        TextAnimator_TMP textAnimator = GetAnimator(_text);
        if (textAnimator == null)
            return null;

        TypewriterComponent typewriter = _text.GetComponent<TypewriterComponent>();
        if (typewriter == null)
        {
            typewriter = _text.gameObject.AddComponent<TypewriterComponent>();
            typewriter.localSettings.useTypeWriter = true;
            typewriter.localSettings.startTypewriterMode = StartTypewriterMode.FromScriptOnly;
            typewriter.TimingSettings = ScriptableObject.CreateInstance<TypingDelaysByCharacter>();
        }

        return typewriter;
    }

    public static void SetText(TMP_Text _text, string _content)
    {
        TextAnimator_TMP textAnimator = GetAnimator(_text);
        if (textAnimator == null)
            return;

        textAnimator.SetText(_content);
    }

    public static TypewriterComponent PlayTypewriter(TMP_Text _text, string _content, UnityAction _onComplete = null)
    {
        TypewriterComponent typewriter = GetTypewriter(_text);
        if (typewriter == null)
            return null;

        if (_onComplete != null)
            AddListenerOnce(typewriter.onTextShowed, _onComplete);

        typewriter.ShowText(_content);
        typewriter.StartShowingText(true);
        return typewriter;
    }

    public static void SkipTypewriter(TMP_Text _text)
    {
        if (_text == null)
            return;

        TypewriterComponent typewriter = _text.GetComponent<TypewriterComponent>();
        if (typewriter == null)
            return;

        typewriter.SkipTypewriter();
    }

    public static void HideTypewriter(TMP_Text _text, UnityAction _onComplete = null)
    {
        if (_text == null)
            return;

        TypewriterComponent typewriter = _text.GetComponent<TypewriterComponent>();
        if (typewriter == null)
            return;

        if (_onComplete != null)
            AddListenerOnce(typewriter.onTextDisappeared, _onComplete);

        typewriter.StartDisappearingText();
    }

    public static void SetTypewriterSpeed(TMP_Text _text, float _speed)
    {
        TypewriterComponent typewriter = GetTypewriter(_text);
        if (typewriter == null)
            return;

        typewriter.SetTypewriterSpeed(_speed);
    }

    private static void AddListenerOnce(UnityEvent _event, UnityAction _onComplete)
    {
        UnityAction onCompleteOnce = null;
        onCompleteOnce = () =>
        {
            _event.RemoveListener(onCompleteOnce);
            _onComplete.Invoke();
        };
        _event.AddListener(onCompleteOnce);
    }
}

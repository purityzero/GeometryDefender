using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIToastMessage : FactoryObject
{
    [SerializeField] private RectTransform m_RectTransform;
    [SerializeField] private Image m_Image;
    [SerializeField] private TextMeshProUGUI m_MessageText;
    [SerializeField] private TweenEffectPlayer m_TweenPlayer;
    [SerializeField] private TextAnimationPlayer m_TextPlayer;
    [SerializeField] private float m_MoveDuration = 0.2f;

    public RectTransform rectTransform => m_RectTransform;

    private UnityEngine.Events.UnityAction<UIToastMessage> m_OnClosed;

    public override void Open()
    {
        base.Open();
        SetAlpha(0f);
    }

    public override void Close()
    {
        base.Close();
        m_TweenPlayer.Stop();
    }

    public void Show(string _message, UnityEngine.Events.UnityAction<UIToastMessage> _onClosed)
    {
        m_OnClosed = _onClosed;

        m_TextPlayer.Play(_message);

        m_TweenPlayer.Play(OnShowComplete);
    }

    public void MoveTo(Vector2 _anchoredPosition)
    {
        m_RectTransform.DOKill();
        TweenUtil.MoveAnchored(m_RectTransform, _anchoredPosition, m_MoveDuration);
    }

    private void SetAlpha(float _alpha)
    {
        Color imageColor = m_Image.color;
        imageColor.a = _alpha;
        m_Image.color = imageColor;

        m_MessageText.alpha = _alpha;
    }

    private void OnShowComplete()
    {
        m_OnClosed?.Invoke(this);
    }
}

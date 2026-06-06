using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class Command_Fade : ICommand
{
    private enum FadeTargetType { CanvasGroup, SpriteRenderer, Image, Material }

    private FadeTargetType m_TargetType;
    private CanvasGroup m_CanvasGroup;
    private SpriteRenderer m_SpriteRenderer;
    private Material m_Material;
	private Image m_Image;
	private UnityAction m_CallBack;

    private float m_TargetAlpha;
    private float m_Duration;
    private Tween m_Tween;
    private bool m_IsFinished;


    public Command_Fade(CanvasGroup canvasGroup, float targetAlpha, float duration, UnityAction _callBack = null)
    {
        m_TargetType = FadeTargetType.CanvasGroup;
        m_CanvasGroup = canvasGroup;
        m_TargetAlpha = targetAlpha;
        m_Duration = duration;
		m_CallBack = _callBack;
    }

    public Command_Fade(SpriteRenderer spriteRenderer, float targetAlpha, float duration, UnityAction _callBack = null)
    {
        m_TargetType = FadeTargetType.SpriteRenderer;
        m_SpriteRenderer = spriteRenderer;
        m_TargetAlpha = targetAlpha;
        m_Duration = duration;
		m_CallBack = _callBack;
    }

	public Command_Fade(Image _image, float targetAlpha, float duration, UnityAction _callBack = null)
    {
        m_TargetType = FadeTargetType.Image;
        m_Image = _image;
        m_TargetAlpha = targetAlpha;
        m_Duration = duration;
		m_CallBack = _callBack;
    }

    public Command_Fade(Material material, float targetAlpha, float duration, UnityAction _callBack = null)
    {
        m_TargetType = FadeTargetType.Material;
        m_Material = material;
        m_TargetAlpha = targetAlpha;
        m_Duration = duration;
		m_CallBack = _callBack;
    }

    public void Execute()
    {
        m_IsFinished = false;

        switch (m_TargetType)
        {
            case FadeTargetType.CanvasGroup:
                m_Tween = m_CanvasGroup.DOFade(m_TargetAlpha, m_Duration)
                    .OnComplete(() => {
						m_CallBack?.Invoke();
						m_IsFinished = true;
					});
                break;

            case FadeTargetType.SpriteRenderer:
                m_Tween = m_SpriteRenderer.DOFade(m_TargetAlpha, m_Duration)
                    .OnComplete(() => {
						m_CallBack?.Invoke();
						m_IsFinished = true;
					});
                break;

            case FadeTargetType.Material:
                m_Tween = m_Material.DOFade(m_TargetAlpha, m_Duration)
                    .OnComplete(() => {
						m_CallBack?.Invoke();
						m_IsFinished = true;
					});
                break;
		case FadeTargetType.Image:
                m_Tween = m_Image.DOFade(m_TargetAlpha, m_Duration)
                    .OnComplete(() => {
						m_CallBack?.Invoke();
						m_IsFinished = true;
					});
			break;
        }
    }

    public void Update()
    {

    }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished()
    {
        return m_IsFinished;
    }
}

public class Move_Command : ICommand
{
    private Transform m_Target;
    private Vector3 m_EndPosition;
    private float m_Duration;
    private Ease m_Ease;
    private Tween m_Tween;
    private bool m_IsFinished;
	private bool m_IsWorld;
	private UnityAction m_isCallBack;

    public Move_Command(Transform _target, Vector3 _endPosition, float _duration, bool _isWorld = true, Ease _ease = Ease.Linear, UnityAction _callBack = null)
    {
        m_Target = _target;
        m_EndPosition = _endPosition;
        m_Duration = _duration;
		m_IsWorld = _isWorld;
        m_Ease = _ease;
		m_isCallBack = _callBack;
    }

    public void Execute()
    {
        m_IsFinished = false;
		if( true == m_IsWorld )
		{
			m_Tween = m_Target.DOMove(m_EndPosition, m_Duration).SetEase(m_Ease).OnComplete(() => {
				m_IsFinished = true;
				m_isCallBack?.Invoke();
			});
		}
		else
		{
			m_Tween = m_Target.DOLocalMove(m_EndPosition, m_Duration).SetEase(m_Ease).OnComplete(() => {
				m_IsFinished = true;
				m_isCallBack?.Invoke();
			});
		}

    }

    public void Update() { }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished() => m_IsFinished;
}

public class Scale_Command : ICommand
{
    private Transform m_Target;
    private Vector3 m_EndScale;
    private float m_Duration;
    private Ease m_Ease;
    private Tween m_Tween;
    private bool m_IsFinished;
	private UnityAction m_CallBack;

    public Scale_Command(Transform _target, Vector3 _endScale, float _duration, Ease _ease = Ease.Linear, UnityAction _callBack = null)
    {
        m_Target = _target;
        m_EndScale = _endScale;
        m_Duration = _duration;
        m_Ease = _ease;
		m_CallBack = _callBack;
    }

    public void Execute()
    {
        m_IsFinished = false;
        m_Tween = m_Target.DOScale(m_EndScale, m_Duration).SetEase(m_Ease).OnComplete(() => {
			m_IsFinished = true;
			m_CallBack?.Invoke();
		});
    }

    public void Update() { }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished() => m_IsFinished;
}

public class Rotate_Command : ICommand
{
    private Transform m_Target;
    private Vector3 m_EndRotation;
    private float m_Duration;
    private Ease m_Ease;
    private Tween m_Tween;
    private bool m_IsFinished;

    public Rotate_Command(Transform _target, Vector3 _endRotation, float _duration, Ease _ease = Ease.Linear)
    {
        m_Target = _target;
        m_EndRotation = _endRotation;
        m_Duration = _duration;
        m_Ease = _ease;
    }

    public void Execute()
    {
        m_IsFinished = false;
        m_Tween = m_Target.DORotate(m_EndRotation, m_Duration).SetEase(m_Ease).OnComplete(() => {
			m_IsFinished = true;
		});
    }

    public void Update() { }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished() => m_IsFinished;
}

public class Color_Command : ICommand
{
    private SpriteRenderer m_Target;
    private Color m_EndColor;
    private float m_Duration;
    private Ease m_Ease;
    private Tween m_Tween;
    private bool m_IsFinished;

    public Color_Command(SpriteRenderer _target, Color _endColor, float _duration, Ease _ease = Ease.Linear)
    {
        m_Target = _target;
        m_EndColor = _endColor;
        m_Duration = _duration;
        m_Ease = _ease;
    }

    public void Execute()
    {
        m_IsFinished = false;
        m_Tween = m_Target.DOColor(m_EndColor, m_Duration)
            .SetEase(m_Ease)
            .OnComplete(() => m_IsFinished = true);
    }

    public void Update() { }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished() => m_IsFinished;
}

public class Parabola_Command : ICommand
{
    private Transform m_Target;
    private Vector3 m_StartPos;
    private Vector3 m_EndPos;
    private float m_Height;
    private float m_Duration;
    private Ease m_Ease;
    private Tween m_Tween;
    private bool m_IsFinished;
	private bool m_IsWorld;

    public Parabola_Command(Transform _target, Vector3 _endPos, float _height, float _duration, bool _isWorld = true, Ease _ease = Ease.Linear)
    {
        m_Target = _target;
        m_StartPos = _target.position;
        m_EndPos = _endPos;
        m_Height = _height;
        m_Duration = _duration;
        m_Ease = _ease;
		m_IsWorld = _isWorld;
    }

    public void Execute()
    {
        m_IsFinished = false;

        Vector3[] path = new Vector3[3];
        path[0] = m_StartPos;
        path[1] = new Vector3((m_StartPos.x + m_EndPos.x) / 2, m_StartPos.y + m_Height, (m_StartPos.z + m_EndPos.z) / 2);
        path[2] = m_EndPos;

		if( true == m_IsWorld )
		{
			m_Tween = m_Target.DOPath(path, m_Duration, PathType.CatmullRom)
				.SetEase(m_Ease)
				.OnComplete(() => m_IsFinished = true);
		}
		else
		{
			m_Tween = m_Target.DOLocalPath(path, m_Duration, PathType.CatmullRom)
				.SetEase(m_Ease)
				.OnComplete(() => m_IsFinished = true);
		}
    }

    public void Update() { }

    public void Cancel()
    {
        if (m_Tween != null && m_Tween.IsActive())
        {
            m_Tween.Kill();
            m_IsFinished = true;
        }
    }

    public bool IsFinished() => m_IsFinished;
}
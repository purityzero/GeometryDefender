using DG.Tweening;
using UnityEngine;

public static class TweenUtil
{
    // ---- Fade ----
    public static Tween Fade(CanvasGroup _target, float _targetAlpha, float _duration)
    {
        return _target.DOFade(_targetAlpha, _duration);
    }

    public static Tween Fade(UnityEngine.UI.Image _target, float _targetAlpha, float _duration)
    {
        return _target.DOFade(_targetAlpha, _duration);
    }

    public static Tween Fade(SpriteRenderer _target, float _targetAlpha, float _duration)
    {
        return _target.DOFade(_targetAlpha, _duration);
    }

    // TMP는 무료 DOTween에 확장 모듈이 없어 제네릭 To 사용
    public static Tween Fade(TMPro.TextMeshProUGUI _target, float _targetAlpha, float _duration)
    {
        return DOTween.To(() => _target.alpha, (alpha) => _target.alpha = alpha, _targetAlpha, _duration);
    }

    // ---- Scale ----
    public static Tween Scale(Transform _target, Vector3 _targetScale, float _duration)
    {
        return _target.DOScale(_targetScale, _duration);
    }

    public static Tween ScalePop(Transform _target, float _duration)
    {
        return _target.DOScale(1f, _duration).From(Vector3.zero).SetEase(Ease.OutBack);
    }

    public static Tween PunchScale(Transform _target, float _strength, float _duration)
    {
        return _target.DOPunchScale(Vector3.one * _strength, _duration);
    }

    public static Tween TapPress(Transform _target, float _scale, float _duration)
    {
        return _target.DOScale(_scale, _duration);
    }

    public static Tween TapRelease(Transform _target, float _duration)
    {
        return _target.DOScale(1f, _duration);
    }

    // ---- Rotate ----
    public static Tween RotateLocal(Transform _target, Vector3 _angles, float _duration, RotateMode _rotateMode = RotateMode.Fast)
    {
        return _target.DOLocalRotate(_angles, _duration, _rotateMode);
    }

    // ---- Move ----
    public static Tween Move(Transform _target, Vector3 _targetPosition, float _duration)
    {
        return _target.DOMove(_targetPosition, _duration);
    }

    public static Tween MoveAnchored(RectTransform _target, Vector2 _targetPosition, float _duration)
    {
        return _target.DOAnchorPos(_targetPosition, _duration);
    }

    // ---- Color ----
    public static Tween Color(SpriteRenderer _target, Color _targetColor, float _duration)
    {
        return _target.DOColor(_targetColor, _duration);
    }

    public static Tween Color(UnityEngine.UI.Image _target, Color _targetColor, float _duration)
    {
        return _target.DOColor(_targetColor, _duration);
    }

    // SpriteRenderer.color(표준 틴트)가 아니라 커스텀 셰이더의 _Color 프로퍼티를 직접 쓰는 머테리얼용(글로우 셰이더 등)
    public static Tween Color(Material _target, Color _targetColor, float _duration)
    {
        return _target.DOColor(_targetColor, _duration);
    }
}

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

    // 월드 스페이스(3D) TMP용 — UGUI가 아니라 씬에 직접 배치되는 TextMeshPro(데미지 텍스트 등)
    public static Tween Fade(TMPro.TextMeshPro _target, float _targetAlpha, float _duration)
    {
        return DOTween.To(() => _target.alpha, (alpha) => _target.alpha = alpha, _targetAlpha, _duration);
    }

    // LineRenderer는 alpha 전용 API가 없어 start/endColor를 함께 트윈(DOTween에 전용 모듈 없음, TMP와 동일 이유로 DOTween.To 사용)
    public static Tween Fade(LineRenderer _target, float _targetAlpha, float _duration)
    {
        Color startColor = _target.startColor;
        Color endColor = _target.endColor;

        return DOTween.To(() => startColor.a, (alpha) =>
        {
            startColor.a = alpha;
            endColor.a = alpha;
            _target.startColor = startColor;
            _target.endColor = endColor;
        }, _targetAlpha, _duration);
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

    // ---- Shake ----
    public static Tween ShakePosition(Transform _target, float _duration, float _strength, int _vibrato = 10)
    {
        return _target.DOShakePosition(_duration, _strength, _vibrato);
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

    // ---- Float (커스텀 셰이더 프로퍼티) ----
    public static Tween Float(Material _target, string _propertyName, float _targetValue, float _duration)
    {
        return _target.DOFloat(_targetValue, _propertyName, _duration);
    }

    // ---- Delay ----
    // 코루틴 대신 DOTween 기반 지연 콜백 — 트윈 대상(Transform 등)이 없는 순수 시간 지연에 사용
    public static Tween DelayedCall(float _delay, TweenCallback _callback)
    {
        return DOVirtual.DelayedCall(_delay, _callback);
    }
}

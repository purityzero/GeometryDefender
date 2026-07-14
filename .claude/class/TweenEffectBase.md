# TweenEffectBase (+ 파생 이펙트 6종)

연관 클래스: TweenEffectPlayer, TweenUtil, TweenSequenceBuilder

## 개요
TweenEffectPlayer가 시퀀스로 조립하는 이펙트 컴포넌트의 공통 베이스. 파생 클래스는 `CreateTween()`만 구현. 파생 6종이 단순해서 이 문서에서 함께 기록한다. (Assets/Scripts/Glory/Tween/)

## 현재 상태

**공통 직렬화 필드 (베이스)**: `m_StepType`(eTweenStepType Append/Join), `m_Duration`(0.2), `m_Delay`(0), `m_Ease`(OutQuad)
- `BuildTween()`이 CreateTween 결과에 Ease/Delay를 공통 적용.
- `eTweenStepType`: Append = 이전 스텝 완료 후, Join = 직전 스텝과 동시.

**파생 컴포넌트**
| 클래스 | 고유 필드 | 대상 |
|---|---|---|
| FadeTweenEffect | m_TargetAlpha(1) | CanvasGroup → Image → SpriteRenderer → TMP 자동 탐색 |
| ScaleTweenEffect | m_TargetScale(one) | transform |
| RotateTweenEffect | m_RotationValue(zero), m_RotateMode(Fast) | transform (상대 회전은 LocalAxisAdd) |
| MoveTweenEffect | m_TargetPosition(zero) | RectTransform이면 anchored, 아니면 월드 |
| ColorTweenEffect | m_TargetColor(white) | Image → SpriteRenderer 자동 탐색 |
| PunchScaleTweenEffect | m_Strength(0.2) | transform |

- Fade/Color는 대상 컴포넌트가 없으면 LogError + null 반환 (Player가 해당 스텝만 건너뜀).

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (범용 트윈 컴포넌트군 요청). TweenUtil에 범용 `Scale` 헬퍼 동시 추가.
- 미검증: 컴파일/플레이 확인 필요.

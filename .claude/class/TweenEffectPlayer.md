# TweenEffectPlayer

연관 클래스: TweenEffectBase, TweenSequenceBuilder

## 개요
TweenEffectBase 컴포넌트들을 등록 순서대로 시퀀스에 조립해 재생/제어하는 컨트롤러. (Assets/Scripts/Glory/Tween/TweenEffectPlayer.cs)

## 현재 상태
- 직렬화 필드: `m_Effects`(TweenEffectBase[], **인스펙터에서 순서대로 드래그 등록 필수**), `m_isPlayOnEnable`(true), `m_LoopCount`(1, -1=무한), `m_LoopType`(Restart)
- API: `Play(UnityAction _onComplete = null)`(재조립 후 재생, 기존 시퀀스 Kill, 완료 콜백 선택), `Pause()`, `Resume()`, `Stop()`, `isPlaying`
- 각 이펙트의 stepType에 따라 Append/Join으로 조립. null 이펙트/트윈 생성 실패 스텝은 건너뜀.
- OnEnable 자동 재생(옵션), OnDisable에서 Stop.
- **주의**: `m_isPlayOnEnable = true`인 상태로 오브젝트 풀에서 재사용(SetActive(true))하면, 콜백을 등록하기도 전에 OnEnable이 먼저 자동 재생을 시작해버릴 수 있다. 재생 시점을 코드에서 직접 제어해야 하는 풀링 대상(예: UIToastMessage)은 `m_isPlayOnEnable = false`로 두고 명시적으로 `Play(callback)`을 호출할 것.

## 사용법
1. 대상 오브젝트에 원하는 *TweenEffect 컴포넌트들 부착 (값 세팅)
2. TweenEffectPlayer 부착 → m_Effects 배열에 실행 순서대로 드래그
3. 자동 재생이면 그대로, 수동이면 코드에서 `Play()` 호출

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (트윈 컴포넌트 제어용 컨트롤러 요청).
- 미검증: 컴파일/플레이 확인 필요.

### 2026-07-18-0
- 개요: [UIToastMessage](./UIToastMessage.md)의 Show/Hide 시퀀스를 이 컴포넌트로 조립하면서, 완료 시점을 코드로 전달받을 방법이 없어(`isPlaying` 폴링밖에 없었음) `Play()`에 선택적 `UnityAction _onComplete` 파라미터 추가. 기존 호출부가 없어(사용처 전무, grep 확인) 하위 호환 문제 없이 시그니처 확장.
- 파일: Assets/Scripts/Glory/Tween/TweenEffectPlayer.cs
- 수정: `public void Play()` → `public void Play(UnityAction _onComplete = null)`, 빌더 조립 마지막에 `if (_onComplete != null) builder.OnComplete(_onComplete);` 추가. `using UnityEngine.Events;` 추가.
- 미검증: 컴파일 확인 필요.

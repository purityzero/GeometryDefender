# TweenEffectPlayer

연관 클래스: TweenEffectBase, TweenSequenceBuilder

## 개요
TweenEffectBase 컴포넌트들을 등록 순서대로 시퀀스에 조립해 재생/제어하는 컨트롤러. (Assets/Scripts/Glory/Tween/TweenEffectPlayer.cs)

## 현재 상태
- 직렬화 필드: `m_Effects`(TweenEffectBase[], **인스펙터에서 순서대로 드래그 등록 필수**), `m_isPlayOnEnable`(true), `m_LoopCount`(1, -1=무한), `m_LoopType`(Restart)
- API: `Play()`(재조립 후 재생, 기존 시퀀스 Kill), `Pause()`, `Resume()`, `Stop()`, `isPlaying`
- 각 이펙트의 stepType에 따라 Append/Join으로 조립. null 이펙트/트윈 생성 실패 스텝은 건너뜀.
- OnEnable 자동 재생(옵션), OnDisable에서 Stop.

## 사용법
1. 대상 오브젝트에 원하는 *TweenEffect 컴포넌트들 부착 (값 세팅)
2. TweenEffectPlayer 부착 → m_Effects 배열에 실행 순서대로 드래그
3. 자동 재생이면 그대로, 수동이면 코드에서 `Play()` 호출

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (트윈 컴포넌트 제어용 컨트롤러 요청).
- 미검증: 컴파일/플레이 확인 필요.

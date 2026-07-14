# RotateLoopEffect

연관 클래스: TweenUtil, TweenSequenceBuilder

## 개요
"회전 → 쉬고 → 반대 회전 → 쉬고" 무한 반복 연출 컴포넌트. (Assets/Scripts/Glory/Tween/RotateLoopEffect.cs)
guid: 3f8c2d94ab5e4f7a9c1d6e8b2a4c7f01 (meta를 직접 생성해 씬 YAML 연결에 사용)

## 현재 상태
- 직렬화 필드: `m_RotateDuration`, `m_RestDuration`, `m_Ease`(기본 Linear), `m_RotationValue`(기본 zero — **인스펙터에서 회전량을 지정해야 동작**, 예: (0,360,0))
- OnEnable에서 TweenSequenceBuilder로 [+회전 → Delay → -회전 → Delay] 시퀀스를 Loops(-1)로 재생, OnDisable에서 Kill.
- `RotateMode.LocalAxisAdd`(상대 회전)라 오브젝트의 기존 회전값을 보존한다.
- 사용처: 현재 없음 — 2026-07-14 TitleScene의 아이콘이 RotateTweenEffect ×2 + TweenEffectPlayer 조합으로 전환되면서 씬에서 제거됨. 라이브러리 유틸로 파일은 유지.

## 작업 내역

### 2026-07-14-0
- 개요: 신규 생성 (Btn_MetaTree 아이콘 Y축 왕복 회전 연출 요청). 초기 구현은 회전량(0,360,0) 고정이었으나, 사용자가 m_Ease/m_RotationValue를 직렬화 필드로 노출하도록 수정 → 그에 맞춰 미사용이 된 지역 변수(fullTurn) 제거.
- 미검증: 플레이 확인 필요.

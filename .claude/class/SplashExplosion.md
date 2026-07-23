# SplashExplosion

## 연관 클래스
- FactoryObject(부모)
- DamageTextManager — 풀링/스폰 주체
- TweenUtil, TweenSequenceBuilder
- [[CritExplosion]] — 동일한 스케일 팝+페이드 구조를 그대로 복제한 원본

## 개요
02_combat.html "투사체 종류" — Splash 카드로 명중했을 때 터지는 범위 폭발 이펙트. 사용자 요청("연쇄 데미지 이펙트나 이런것도 좀 넣어줘.. 뭐 연쇄는지 폭발하는지 알수가 없어")으로 신규 생성. 크리티컬 처치 전용인 [[CritExplosion]]과 달리 **명중마다(관통/스플래시 카드 보유 시 매 히트)** 재생될 수 있어 더 작고 빠르게 튜닝된 별도 클래스로 분리.

## 현재 상태
- 경로: Assets/Scripts/InGame/SplashExplosion.cs
- 프리팹: Assets/Resources/Prefabs/Effect/SplashExplosion.prefab
- `Play(Action<SplashExplosion> _onComplete)`: 알파 1로 리셋 + 스케일 0에서 시작 → `TweenSequenceBuilder`로 `Scale`(0→`GameConfigTable.SPLASH_EXPLOSION_TARGET_SCALE`, `SPLASH_EXPLOSION_SCALE_POP_DURATION`) + `Join Fade`(alpha→0, `SPLASH_EXPLOSION_FADE_DURATION`) 동시 재생 → 완료 시 콜백(풀 반납용). 값 전부 GameConfigTable 소유(2026-07-24 "Const는 ConfigTable로" 원칙 적용, 로컬 const 없음).
- 시각: `shape_circle.png`(CritExplosion과 동일 원형 스프라이트) + 전용 머테리얼 `GlowMat_SplashExplosion.mat`(`Shader Graphs/Glow`, `_Color`=주황(1, 0.4, 0.05), `_GlowAmount`=1.4) — 사용자가 "우리게임에 우리가 사용한 Glow같은 느낌을 많이 주고싶어"라고 요청해, 기본 Sprites-Default 대신 프로젝트의 Glow 셰이더 계열을 사용(HDR 파이프라인 + Bloom로 실제 발광 확인).

## 작업 내역

### 2026-07-24-0
- 개요: 신규 생성. `ProjectileCollisionSystem`의 Splash 분기에서 명중 지점에 트리거.
- 검증: 컴파일 에러 0건. Play Mode 실측(Unity MCP) — `DamageTextManager.ShowSplashExplosion()` 직접 호출로 풀에서 정상 스폰/활성화 확인, 스크린샷으로 오렌지색 글로우 버스트 렌더링 확인(부드러운 Bloom 헤일로 포함). 실제 전투 중(`TowerController.SetSplash(3f)` 적용 후 자동 전투 수 초 진행) 콘솔 에러 0건 — `ProjectileCollisionSystem` 연동 경로도 예외 없이 정상 동작.

### 관련 클래스
- [[DamageTextManager]] 2026-07-24-1 — `ShowSplashExplosion()` 호출부
- [[ProjectileCollisionSystem]] 2026-07-24-0 — 트리거 지점
- [[GameConfigRecord]] 2026-07-24-1 — 튜닝값 저장소

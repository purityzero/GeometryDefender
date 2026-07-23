# CritExplosion

## 연관 클래스
- FactoryObject(부모)
- DamageTextManager — 풀링/스폰 주체
- TweenUtil, TweenSequenceBuilder

## 개요
02_combat.html "치명타 시스템" — 치명타로 적을 처치했을 때 터지는 폭발 이펙트. 파티클 시스템이 프로젝트에 전혀 없어서(신규 도입 안 함, 단순함 우선) 스프라이트 확대+페이드로 대체 표현.

## 현재 상태
- 경로: Assets/Scripts/InGame/CritExplosion.cs
- 프리팹: Assets/Resources/Prefabs/Effect/CritExplosion.prefab
- `Play(Action<CritExplosion> _onComplete)`: 알파 1로 리셋 + 스케일 0에서 시작 → `TweenSequenceBuilder`로 `Scale`(0→0.5, 0.15초) + `Join Fade`(SpriteRenderer alpha→0, 0.25초) 동시 재생 → 완료 시 콜백(풀 반납용).
- `TARGET_SCALE = 0.5`(2026-07-23 사용자 피드백 "크리 터지는거 너무 커.."로 2.5→0.5 축소) — 프리팹 네이티브 스프라이트 지름 2.22 기준, 최종 표시 지름이 몬스터/타워 실제 크기(대략 0.5~1유닛)와 비슷해지도록 역산.

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("사격시스템 구현해줘" → 치명타 폭발 VFX 항목) — 신규 생성.
- 검증: 컴파일 에러 0건. Play Mode 실측 — 치명타 확률 200%로 강제한 뒤 처치 시 폭발 이펙트가 정상 표시되는 것 스크린샷 확인. 최초 TARGET_SCALE=2.5는 사용자 피드백으로 0.5로 축소, 재검증 완료(스크린샷으로 몬스터/타워 대비 크기 적절함 확인).

### 관련 클래스
- [[DamageTextManager]] 2026-07-23-1 — `ShowCritExplosion()` 호출부
- [[HealthSystem]] 2026-07-23-1 — 치명타 처치 감지 후 트리거

### 2026-07-24-0 — const 전부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `SCALE_POP_DURATION`/`FADE_DURATION`/`TARGET_SCALE`(Vector3) 제거 → `GameConfigTable.CRIT_EXPLOSION_SCALE_POP_DURATION`/`CRIT_EXPLOSION_FADE_DURATION`/`CRIT_EXPLOSION_TARGET_SCALE`(float) 참조. `TARGET_SCALE`은 Vector3였으나 균등 스케일이라 float로 저장 후 `Play()` 안에서 `Vector3.one * GameConfigTable.CRIT_EXPLOSION_TARGET_SCALE`로 복원.
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.

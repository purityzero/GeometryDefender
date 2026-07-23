# DamageText

## 연관 클래스
- FactoryObject (베이스)
- DamageTextManager — 풀링/스폰/회수 담당
- TweenUtil, TweenSequenceBuilder

## 개요
07_ui.html "데미지 텍스트" 스펙 구현체. 월드 스페이스 `TMPro.TextMeshPro`(3D, UGUI 아님) 기반 숫자 팝업 — 피격 시 위로 살짝 이동하며 0.5초 페이드아웃. 치명타는 1.5배 크기 + 노란색.

## 현재 상태
- 경로: Assets/Scripts/InGame/DamageText.cs
- 프리팹: Assets/Resources/Prefabs/Effect/DamageText.prefab ([[DamageText (prefab)]] 참고)
- `FactoryObject` 상속(Actor 아님 — 전투 참여자가 아니라 순수 이펙트라 구분).
- `Play(int _amount, bool _isCrit, bool _isAllyDamage, Action<DamageText> _onComplete)`: 텍스트/색상/스케일 설정 → `TweenSequenceBuilder`로 `Move`(위로 0.8) + `Join Fade`(TMP alpha→0, 0.5초) 동시 재생 → 완료 시 콜백(풀 반납용).
- 색상: 일반 적군 피격 흰색, 아군(타워) 피격 빨강 계열, 치명타는 대상 무관 노란색(우선순위 최상위).

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다 나올 수 있고") — 신규 생성.
- 검증: 컴파일 에러 0건. Play Mode 실측 — 몬스터 피격 시 흰 숫자, 타워 피격 시 빨간 숫자가 각각 정상 위치에 표시되고 페이드아웃되는 것 스크린샷으로 확인. 치명타(노란색) 케이스는 확률 기반이라 이번 세션 스크린샷으로 직접 포착은 못함(로직상 정상 — [[TowerController]] `isCrit` 판정 → [[ProjectileStats]]/[[DamageRequest]] 전달 경로 코드 리뷰로 확인).

### 2026-07-24-0 — const 전부 GameConfigTable로 이관
[[GameConfigRecord]] 2026-07-24-0 참고. `CRIT_SCALE`/`MOVE_UP_DISTANCE`/`FADE_DURATION` 제거 → `GameConfigTable.DAMAGE_TEXT_CRIT_SCALE`/`DAMAGE_TEXT_MOVE_UP_DISTANCE`/`DAMAGE_TEXT_FADE_DURATION` 참조. 색상 3종(Color)은 GameConfigTable이 float만 지원해 이관 대상에서 제외, 그대로 유지.
검증: 컴파일 에러 0건. Play Mode 재검증 미완료.

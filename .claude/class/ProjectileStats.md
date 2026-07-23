# ProjectileStats

## 연관 클래스
- ProjectileManager — `Fire()`/`SpawnOrbitals()`에서 생성
- ProjectileCollisionSystem — 명중 판정 시 `Damage`/`IsCrit`을 `DamageRequest`로 그대로 전달

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/ProjectileStats.cs
- `IComponentData`. 필드: `Damage`(int), `Radius`(float), `Pierce`(int), `IsCrit`(bool, 2026-07-23 추가).

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘") — 데미지 텍스트가 치명타 스타일(1.5배 크기+노란색)을 표시하려면, 명중 시점까지 크리티컬 여부가 함께 전달돼야 함. `TowerController.Fire()`가 발사 시점에 이미 크리티컬을 판정해두므로, 그 결과를 투사체에 실어 보냄.
- 수정: `IsCrit`(bool) 필드 추가. `ProjectileManager.Fire()`가 새 `_isCrit` 매개변수(기본값 false)를 받아 설정. `SpawnOrbitals()`는 오비탈이 크리티컬 대상이 아니라 기본값(false) 그대로.
- 검증: 컴파일 에러 0건, Play Mode 실측(치명타 시 노란 텍스트 확인) — [[ProjectileManager]] 2026-07-23-0, [[DamageTextManager]] 참고.

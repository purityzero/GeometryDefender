# ProjectileCollisionSystem

## 연관 클래스
- ProjectileStats, ProjectileEffects, DamageRequest, MonsterTag, CombatRadius

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- `ISystem`(unmanaged), `SimulationSystemGroup`, `[UpdateAfter(typeof(ProjectileMoveSystem))]`.
- naive O(N×M) 원형 거리 판정(몬스터/투사체 수가 적은 현재 규모에선 충분, 규모 커지면 Spatial Hash Grid로 교체 예정).
- 명중 시 `DamageRequest` 추가 → Chain Lightning(#304)/Splash I(#303) 카드 효과 처리(체인/스플래시는 배타적) → Pierce 스택 있으면 계속 날아감, 없으면 `ProjectileExpiredTag` 부착.

## 작업 내역

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘") — 데미지 텍스트가 치명타 스타일을 표시하려면 `DamageRequest`에 크리티컬 여부가 실려야 함.
- 파일: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- 수정 (함수 단위)
  - **OnUpdate()**: 명중/체인/스플래시 3곳의 `DamageRequest` 생성 코드 전부에 `IsCrit = stats.ValueRO.IsCrit` 추가 — 같은 발사체의 크리티컬 여부를 체인/스플래시로 튄 피해에도 동일하게 적용.
- 검증: 컴파일 에러 0건, Play Mode 실측(치명타 시 노란 텍스트, 일반 시 흰 텍스트 확인) — [[DamageRequest]] 2026-07-23-0, [[ProjectileStats]] 참고.

### 2026-07-24-0
- 개요: 사용자 요청("연쇄 데미지 이펙트나 이런것도 좀 넣어줘.. 뭐 연쇄는지 폭발하는지 알수가 없어") — Splash/Chain 분기에 시각 이펙트 트리거 추가. 데미지 계산/타겟 판정 로직은 전혀 건드리지 않음(시각만 추가, [[ChainLightning]] "설계 근거" 참고 — 체인 판정 기준점을 "진짜 순차 호핑"으로 바꿀지 검토했으나 게임플레이 부작용 우려로 되돌리고 순수 시각화만 추가).
- 파일: Assets/Scripts/InGame/ECS/ProjectileCollisionSystem.cs
- 수정 (함수 단위)
  - **OnUpdate()**: 명중 직후 `DamageTextManager damageTextManager = (InGameScene.Current != null) ? InGameScene.Current.damageTextManager : null;` 지역 변수 추가([[HealthSystem]] 2026-07-23-1과 동일 이중 null 체크 패턴).
  - **Chain 분기**: 명중 지점부터 시작하는 `List<Vector3> chainPoints`를 루프 중 함께 누적, 실제로 1체 이상 튀었으면(`chainPoints.Count > 1`) `damageTextManager?.ShowChainLightning(chainPoints)` 호출.
  - **Splash 분기**: 데미지 적용 루프 뒤에 `damageTextManager?.ShowSplashExplosion(splashPosition)` 호출(다른 대상에 실제로 맞았는지와 무관하게, Splash 카드 보유 상태에서 명중할 때마다 항상 재생 — "터졌다"는 시각 피드백 자체가 목적).
- 검증: 컴파일 에러 0건. Play Mode 실측 — `TowerController.SetSplash()`/`SetChain()`으로 강제 적용 후 자동 전투 수 초 진행, 콘솔 에러 0건. 상세는 [[SplashExplosion]]/[[ChainLightning]] 참고.

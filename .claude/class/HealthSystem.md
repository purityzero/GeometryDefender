# HealthSystem

## 연관 클래스
- HealthComponent (HealthData), DamageRequest, MonsterTags
- MoveSystem — UpdateAfter 대상

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/HealthSystem.cs
- `ISystem` (unmanaged, `[BurstCompile]` 없음 — 매니지드 코드 호출 가능), `SimulationSystemGroup`, `[UpdateAfter(typeof(MoveSystem))]`.
- 처리 흐름: `MonsterTag` 있고 `DeadTag` 없는 엔티티의 DamageRequest 버퍼를 모두 합산해 CurrentHp 차감 → 각 요청마다 `DamageTextManager.Current?.ShowEnemyDamage()` 호출(2026-07-23) → 버퍼 Clear → HP 0 이하면 EndSimulationECB로 `DeadTag` 부착.
- HP는 0 미만으로 내려가지 않게 클램프.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

### 2026-07-23-0
- 개요: 사용자 요청("데미지 폰트도 넣어줘 적군 아군 둘다 나올 수 있고") — 07_ui.html "데미지 텍스트" 스펙 구현.
- 파일: Assets/Scripts/InGame/ECS/HealthSystem.cs
- 수정 (함수 단위)
  - **OnUpdate()**: 쿼리에 `RefRO<LocalTransform>` 추가(데미지 텍스트 스폰 위치 필요). 데미지 차감 루프 안에서 `DamageTextManager.Current?.ShowEnemyDamage(worldPosition, damageRequests[i].Amount, damageRequests[i].IsCrit)` 호출 추가. `float3`→`Vector3` 명시적 변환 필요(암시적 캐스팅 없음, `VisualSyncSystem` 등 기존 패턴과 동일).
- 검증: 컴파일 에러 0건. Play Mode 실측 — 타워 발사→몬스터 피격 시 흰색 숫자 텍스트가 몬스터 위치에 정상 표시되는 것 스크린샷으로 확인, 콘솔 에러 0건.
- 관련 클래스: [[DamageTextManager]], [[DamageRequest]] 2026-07-23-0

### 2026-07-23-1 — 치명타 처치 폭발/셰이크/진동 트리거 + InGameScene.Current 이중 null 체크
사용자 요청("사격시스템 구현해줘") — 상세는 [[DamageTextManager]] 2026-07-23-2 참고.
- **OnUpdate()**: `InGameScene.Current.damageTextManager?...`로 안전하지 않게(Current 자체 null 미체크) 쓰던 것을 `DamageTextManager damageTextManager = (InGameScene.Current != null) ? InGameScene.Current.damageTextManager : null;`로 지역변수화 + 가드([[InGameScene]] 2026-07-23-1에서 확립한 이중 체크 패턴 재사용).
- **wasCritThisBatch 추적**: 데미지 요청 루프에서 크리티컬 발생 즉시 `ShakeCamera()`/`VibrateOnCrit()` 호출, 배치 내 하나라도 크리티컬이면 플래그 세움.
- **사망 분기**: `wasCritThisBatch == true`일 때만 `ShowCritExplosion()` 호출(같은 프레임에 낀 여러 피해 중 하나라도 치명타였으면 인정하는 근사치 — "치명타로 처치"를 엄밀히 판정하려면 정확히 어느 피해가 마지막 한 방이었는지 추적해야 하나, 단순함 우선으로 근사치 채택).
- 검증: 컴파일 에러 0건. Play Mode 실측 — 치명타 확률 200% 강제 후 반복 처치, 폭발/셰이크/진동 경로 전부 크래시 없이 동작 확인.

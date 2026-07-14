# HealthSystem

## 연관 클래스
- HealthComponent (HealthData), DamageRequest, MonsterTags
- MoveSystem — UpdateAfter 대상

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/HealthSystem.cs
- `ISystem` (unmanaged), `SimulationSystemGroup`, `[UpdateAfter(typeof(MoveSystem))]`.
- 처리 흐름: `MonsterTag` 있고 `DeadTag` 없는 엔티티의 DamageRequest 버퍼를 모두 합산해 CurrentHp 차감 → 버퍼 Clear → HP 0 이하면 EndSimulationECB로 `DeadTag` 부착.
- HP는 0 미만으로 내려가지 않게 클램프.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

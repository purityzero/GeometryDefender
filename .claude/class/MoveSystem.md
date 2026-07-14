# MoveSystem

## 연관 클래스
- MoveComponent (MoveData), WaypointElement, MonsterTags

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/MoveSystem.cs
- `ISystem` (unmanaged), `SimulationSystemGroup`.
- 처리 흐름: `MonsterTag` 있고 `DeadTag`/`ReachedEndTag` 없는 엔티티를 현재 웨이포인트로 등속 이동 → 0.05 이내 도달 시 다음 인덱스 → 웨이포인트 소진 시 EndSimulationECB로 `ReachedEndTag` 부착.
- ECB는 직접 생성하지 않고 EndSimulationECBSystem 싱글톤에서 받아 sync point 최소화.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

# MoveSystem

## 연관 클래스
- MoveComponent (MoveData), WaypointElement, MonsterTags

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/MoveSystem.cs
- `ISystem` (unmanaged), `SimulationSystemGroup`.
- 처리 흐름: `MonsterTag` 있고 `DeadTag`/`ReachedEndTag` 없는 엔티티를 현재 웨이포인트로 등속 이동 → 0.05 이내 도달 시 다음 인덱스 → 웨이포인트 소진 시 EndSimulationECB로 `ReachedEndTag` 부착.
- ECB는 직접 생성하지 않고 EndSimulationECBSystem 싱글톤에서 받아 sync point 최소화.

## 작업 내역

### 2026-07-21-2

#### 개요
사용자 보고: "몬스터 충돌(목적지 도달) 체크가 제대로 안 되고, 한참 뒤에야 사라짐" 버그 수정.

#### 파일
- Assets/Scripts/InGame/ECS/MoveSystem.cs

#### 증상
목적지 근처에서 도달 판정(`distance < 0.05f`)이 즉시 안 걸리고 한참 뒤에야 `ReachedEndTag`가 붙어 몬스터가 사라짐.

#### 원인
매 프레임 이동량(`MoveSpeed * deltaTime`)이 남은 거리보다 크면 목적지를 그냥 지나쳐버림(오버슈트). 예: Swift Elite(MoveSpeed 3.6)는 60fps에서 프레임당 약 0.06 유닛 이동 — 도달 판정 임계값 0.05f보다 큼. 지나친 다음 프레임엔 방향이 다시 목적지 쪽으로 재계산되어 되돌아오다 또 지나치는 식으로 목적지 주변을 왕복 진동하며, 우연히 두 값 중 하나가 0.05f 미만이 될 때까지 판정이 미뤄짐(그래서 "한참 뒤에" 사라지는 것처럼 보임).

#### 수정 (함수 단위)
**MoveSystem.OnUpdate (foreach 루프 내부)**
- 전: `direction * moveData.ValueRO.MoveSpeed * deltaTime`을 그대로 이동량으로 사용 (남은 거리보다 커도 그대로 적용 → 오버슈트 가능)
- 후: `distanceToTarget = math.distance(currentPosition, targetPosition)`을 먼저 구하고, `moveDistance = math.min(MoveSpeed * deltaTime, distanceToTarget)`으로 이번 프레임 이동량을 남은 거리 이하로 클램프. 목적지에 도달하는 프레임엔 정확히 그 지점에서 멈춰 `distance < 0.05f`가 즉시 성립.

#### 미검증
컴파일/에디터 미실행 상태 편집. 실제 Play Mode로 Swift/Swift Elite 등 빠른 몬스터가 베이스 도달 시 지연 없이 바로 사라지는지 확인 필요.

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

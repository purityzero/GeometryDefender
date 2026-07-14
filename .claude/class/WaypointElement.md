# WaypointElement

## 연관 클래스
- MoveSystem — CurrentWaypointIndex로 순회하며 이동
- MonsterManager — Spawn 시 버퍼 추가 (현재 Vector2.zero 하나만 추가)

## 현재 상태
- 경로: Assets/Scripts/InGame/ECS/WaypointElement.cs
- `IBufferElementData`, 필드: `Position` (Vector2).
- 현재 Spawn에서 원점 하나만 넣으므로 모든 몬스터가 화면 밖 링 → 중앙(0,0)으로 직진하는 구조.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

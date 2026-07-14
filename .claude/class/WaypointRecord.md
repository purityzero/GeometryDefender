# WaypointRecord

## 연관 클래스
- (과거) Table, Record, TableManager (Glory)

## 현재 상태
- 경로: Assets/Scripts/Table/WaypointRecord.cs
- **현재 빈 파일** — 클래스 정의가 제거된 상태.
- Assets/Resources/Table/WaypointTable.csv는 남아 있음 (헤더: Id,PathId,Order,X,Y,Z).
- TableManager.init()에서도 로드하지 않음 — 몬스터 이동이 웨이포인트 경로 방식에서 "링 스폰 → 원점 직진" 방식(WayPoint 클래스)으로 바뀌면서 사용 중단된 것으로 보임.
- MonsterManager.md(구버전)에 있던 `Spawn(record, waypoints)` 시그니처가 이 흔적.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음). 빈 파일 상태 및 사용 중단 경위 기록.

# WaveSpawnRecord (WaveSpawnTable)

## 연관 클래스
- Table, Record, TableManager (Glory)
- WaveRecord, EnemyRecord

## 현재 상태
- 경로: Assets/Scripts/Table/WaveSpawnRecord.cs
- 필드: WaveId(int), SpawnOrder(int), EnemyId(int) — 웨이브별 몬스터 스폰 순서 정의.
- `WaveSpawnTable : Table<WaveSpawnRecord>`.
- 데이터: Assets/Resources/Table/WaveSpawnTable.csv (헤더: Id,WaveId,SpawnOrder,EnemyId)
- 아직 이 테이블을 소비하는 코드는 없음.

## 작업 내역

### 2026-07-12-0
- 개요: 프로젝트 전체 스캔으로 기본 정보 문서 초기 생성 (코드 수정 없음)

---

## 2026-07-15-0

### 개요
D:\Unity\Job에서 머지 — "웨이브별 스폰 순서" 방식에서 "시각 지정 보스 스폰 이벤트" 방식으로 변경.

### 수정
- 전: WaveId/SpawnOrder/EnemyId
- 후: SpawnTime/EnemyId, `WaveSpawnTable.GetBossEventAtTime(경과초)` 추가
- WaveSpawnTable.csv도 보스 이벤트 10행(300초 간격)으로 교체.
